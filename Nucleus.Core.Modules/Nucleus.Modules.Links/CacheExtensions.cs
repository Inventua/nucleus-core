using System.Collections.Generic;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Links.Models;
using System;

namespace Nucleus.Modules.Links
{
	public static class CacheExtensions
	{
		public static CacheCollection<Guid, Link> LinksCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Link>();
		}

		public static CacheCollection<Guid, List<Link>> ModuleLinksCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, List<Link>>();
		}
	}
}
