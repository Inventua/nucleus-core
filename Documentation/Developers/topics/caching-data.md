Nucleus core caches most data entities in memory in order to improve performance.  Modules and extensions can use the Nucleus Cache Manager to cache their own
data.  Caching is provided by the [CacheManager](/api-documentation/Nucleus.Abstractions.Managers.ICacheManager) 
and [CacheCollection](/api-documentation/Nucleus.Abstractions.Models.Cache.CacheCollectionT0T1) 
classes.


## Cache Extension
To implement caching, create a cache extension class.  Each Nucleus cache collection specfies a key type and model type.  

```
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Links.Models;
using System;

namespace Nucleus.Modules.Links;

public static class CacheExtensions
{
  public static CacheCollection<Guid, Link> LinksCache(this ICacheManager cacheManager)
  {
    return cacheManager.Get<Guid, Link>();
  }
}
```

> A CacheCollection object is automatically created by the [CacheManager](/api-documentation/Nucleus.Abstractions.Managers.ICacheManager)
if one doesn't already exist when you call [CacheManager.Get](/api-documentation/Nucleus.Abstractions.Managers.ICacheManager/#GetKeyTModel)

## Getting a Cache Manager instance
In your manager class (or anywhere else that you need it), add an ICacheManager parameter to your constructor so that 
dependency injection automatically provides an instance of the Cache Manager.

```
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Links;

public class LinksManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }
    
  public LinksManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
  {
    this.CacheManager = cacheManager;
    this.DataProviderFactory = dataProviderFactory;
  }
}
```

## Using the Cache
Wrap your database access ==Get== method implementation in a call to [GetAsync](/api-documentation/Nucleus.Abstractions.Models.Cache.CacheCollectionT0T1/#GetAsync(TKeyTKey))
in order to check for a cache item matching your key (id) before invoking the code to retrieve it from the database.
```
public async Task<Link> Get(Site site, Guid id)
{
  return await this.CacheManager.LinksCache().GetAsync(id, async id =>
  {
    using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
    {
      Link result = await provider.Get(id);
      if (result != null)
      {
        await GetLinkItem(site, result);
      }

      return result;
    }
  });
}
```

> The code to retrieve the item from the database is not executed unless the item specified by `id` is not present in the cache.

> In the examples above, the cache item key is a Guid, but you can use different types for cache item keys.

## Removing an item from the Cache
If the cached entity is changed, you must remove it from the cache.  In your ==Manager== class `.Save` method (or any other method which 
updates data), add a call to your cache extension's [Remove](/api-documentation/Nucleus.Abstractions.Models.Cache.CacheCollectionT0T1/#Remove(TKey))
method, specifying the entity id.

```
this.CacheManager.LinksCache().Remove(id);
```

If you need to clear the entire cache, you can call [Clear](/api-documentation/Nucleus.Abstractions.Models.Cache.CacheCollectionT0T1/#Clear).

```
this.CacheManager.LinksCache().Clear();
```

