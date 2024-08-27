using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Models.Configuration;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Nucleus.Core.Managers;

public class CacheManager : ICacheManager
{
  private Dictionary<string, ICacheCollection> Caches { get; } = [];
  private IConfiguration Configuration { get; }
  private ILogger<ICacheManager> Logger { get; }

  private Meter CacheMeter { get; }

  private static readonly object lockObject = new();

  public CacheManager(IMeterFactory meterFactory, IConfiguration configuration, ILogger<ICacheManager> logger)
  {
    this.Configuration = configuration;
    this.Logger = logger;

    this.CacheMeter = meterFactory.Create("nucleus.cache", typeof(CacheManager).Assembly.GetName().Version.ToString());
    this.CacheMeter.CreateObservableGauge("nucleus.cache", GetCacheCounts, description: "Nucleus cache counters.");
  }

  private IEnumerable<Measurement<int>> GetCacheCounts()
  {
    foreach (KeyValuePair<string, ICacheCollection> cache in this.Caches)
    {
      yield return new Measurement<int>(cache.Value.Count, new List<KeyValuePair<string, object>>()
      {
        new KeyValuePair<string, object>("cache_name", cache.Value.Name.ToLower()),
        new KeyValuePair<string, object>("capacity", cache.Value.Options.Capacity),
        new KeyValuePair<string, object>("expiry_time_seconds", cache.Value.Options.ExpiryTime.TotalSeconds)
      });
    }
  }

  private static string CacheKey<TKey, TModel>(string caller)
  {
    return $"{caller} {typeof(TKey).FullName} {typeof(TModel).FullName}";
  }

  private CacheCollection<TKey, TModel> Add<TKey, TModel>(string configItemName)
  {
    string cacheKey = CacheKey<TKey, TModel>(configItemName);
    ICacheCollection cache;

    if (!this.Caches.TryGetValue(cacheKey, out cache))
    {
      lock (lockObject)
      {
        if (!this.Caches.TryGetValue(cacheKey, out cache))
        {
          CacheOption options = new();
          this.Configuration.GetSection($"{CacheOption.Section}:{configItemName}").Bind(options, binderOptions => binderOptions.BindNonPublicProperties = true);

          this.Logger?.LogDebug("Creating cache:'{configItemName}' with capacity={capacity}, expiry time={expiry}.", configItemName, options.Capacity, options.ExpiryTime);

          CacheCollection<TKey, TModel> newCache = new(configItemName, this.Logger, options);
          this.Caches.Add(cacheKey, newCache);

          return newCache;
        }
      }
    }

    return cache as CacheCollection<TKey, TModel>;
  }

  public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "Default")
  {
    if (this.Caches.TryGetValue(CacheKey<TKey, TModel>(caller), out ICacheCollection cache))
    {
      CacheCollection<TKey, TModel> result = cache as CacheCollection<TKey, TModel>;

      if (result != null)
      {
        return result;
      }
    }

    return Add<TKey, TModel>(caller);
  }

  /// <summary>
  /// Return a report containing the current state of all caches.
  /// </summary>
  /// <returns></returns>
  public List<CacheReport> Report()
  {
    List<CacheReport> result = new();

    lock (lockObject)
    {
      foreach (ICacheCollection cache in this.Caches.Values)
      {
        result.Add(new() { Name = cache.Name, Count = cache.Count, Options = cache.Options });
      }
    }

    return result;
  }

  public void Collect()
  {
    lock (lockObject)
    {
      foreach (ICacheCollection cache in this.Caches.Values)
      {
        cache.Collect();
      }
    }
  }

  public void ClearAll()
  {
    lock (lockObject)
    {
      foreach (ICacheCollection cache in this.Caches.Values)
      {
        cache.Clear();
      }
    }
  }
    
  public void Clear(string cacheName)
  {
    ICacheCollection cache = this.Caches.Values.Where(cache => cache.Name == cacheName).FirstOrDefault();

    cache?.Clear();
  }
}
