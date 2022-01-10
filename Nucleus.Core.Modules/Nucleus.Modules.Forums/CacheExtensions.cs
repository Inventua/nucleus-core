using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums
{
	public static class CacheExtensions
	{
		public static CacheCollection<Guid, Group> GroupsCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Group>();
		}

		public static CacheCollection<Guid, Forum> ForumsCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Forum>();
		}
	}
}
