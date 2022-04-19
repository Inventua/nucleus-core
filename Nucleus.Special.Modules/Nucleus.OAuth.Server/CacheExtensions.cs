using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server
{
	public static class CacheExtensions
	{
		public static CacheCollection<Guid, ClientApp> ClientAppCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, ClientApp>();
		}

		public static CacheCollection<Guid, ClientAppToken> ClientAppTokenCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, ClientAppToken>();
		}
	}
}
