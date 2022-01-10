using Nucleus.Abstractions.Models.Cache;
using Nucleus.Core;
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
		public static CacheCollection<Guid, Group> GroupsCache(this CacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Group>();
		}

		public static CacheCollection<Guid, Forum> ForumsCache(this CacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Forum>();
		}
	}
}
