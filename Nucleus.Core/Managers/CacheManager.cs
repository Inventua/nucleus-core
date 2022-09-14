using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.Cache;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nucleus.Core.Managers
{
	public class CacheManager : ICacheManager
	{
		private Dictionary<string, ICacheCollection> Caches { get; } = new();
		private IConfiguration Configuration { get; }
		private ILogger<ICacheManager> Logger {get;}

		private static readonly object lockObject = new();

		public CacheManager(IConfiguration configuration, ILogger<ICacheManager> logger)
		{
			this.Configuration = configuration;
			this.Logger = logger;
		}

		private static string CacheKey<TKey, TModel>(string caller)
		{
			return $"{caller} {typeof(TKey).FullName} {typeof(TModel).FullName}";
		}

		private CacheCollection<TKey, TModel> Add<TKey, TModel>(string configItemName) where TModel : class
		{
			string cacheKey = CacheKey<TKey, TModel>(configItemName);

			if (!this.Caches.ContainsKey(cacheKey))
			{
				lock(lockObject)
				{
					if (!this.Caches.ContainsKey(cacheKey))
					{
						CacheOption options = new();
						this.Configuration.GetSection($"{CacheOption.Section}:{configItemName}").Bind(options, binderOptions => binderOptions.BindNonPublicProperties = true);

						this.Logger?.LogDebug("Creating cache:'{configItemName}' with options: capacity={capacity}, expiry time={expiry}.", configItemName, options.Capacity, options.ExpiryTime);

						CacheCollection<TKey, TModel> result = new(this.Logger, options);
						this.Caches.Add(cacheKey, result);
						return result;
					}
				}				
			}

			return this.Caches[cacheKey] as CacheCollection<TKey, TModel>;
		}

		public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "Default") where TModel : class
		{
			if (this.Caches.TryGetValue(CacheKey<TKey, TModel>(caller), out ICacheCollection result))
			{
				if (result is CacheCollection<TKey, TModel>)
				{
					return result as CacheCollection<TKey, TModel>;
				}
			}

			return Add<TKey, TModel>(caller);
		}

		public void Collect()
		{
			foreach (ICacheCollection cache in this.Caches.Values)
			{
				cache.Collect();
			}
		}
	}
}
