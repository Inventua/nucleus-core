using System.Collections.Generic;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.PageLinks.Models;
using System;

namespace Nucleus.Modules.PageLinks
{
	public static class CacheExtensions
	{
    public static CacheCollection<Guid, PageLink> PageLinksCache(this ICacheManager cacheManager)
    {
      return cacheManager.Get<Guid, PageLink>();
    }

    public static CacheCollection<Guid, List<PageLink>> ModulePageLinksCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, List<PageLink>>();
		}
	}
}
