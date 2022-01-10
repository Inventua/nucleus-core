using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish
{
	public static class CacheExtensions
	{
		public static CacheCollection<Guid, Article> ArticleCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Article>();
		}
	}
}
