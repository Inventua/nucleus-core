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
using System.Diagnostics.Metrics;
using Nucleus.Core.Services;

namespace Nucleus.Core.Managers
{
	public class CacheManager : ICacheManager
	{
		private Dictionary<string, ICacheCollection> Caches { get; } = new();
		private IConfiguration Configuration { get; }
		private ILogger<ICacheManager> Logger {get;}
    private Meter CacheMeter { get; }

		private static readonly object lockObject = new();

		public CacheManager(IConfiguration configuration, ILogger<ICacheManager> logger)
		{
			this.Configuration = configuration;
			this.Logger = logger;
     
      this.CacheMeter = new("nucleus.cache", typeof(CacheManager).Assembly.GetName().Version.ToString());
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

			if (!this.Caches.ContainsKey(cacheKey))
			{
				lock(lockObject)
				{
					if (!this.Caches.ContainsKey(cacheKey))
					{
						CacheOption options = new();
						this.Configuration.GetSection($"{CacheOption.Section}:{configItemName}").Bind(options, binderOptions => binderOptions.BindNonPublicProperties = true);

						this.Logger?.LogDebug("Creating cache:'{configItemName}' with capacity={capacity}, expiry time={expiry}.", configItemName, options.Capacity, options.ExpiryTime);

						CacheCollection<TKey, TModel> result = new(configItemName, this.Logger, options);
						this.Caches.Add(cacheKey, result);
						return result;
					}
				}				
			}

			return this.Caches[cacheKey] as CacheCollection<TKey, TModel>;
		}

		public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "Default") 
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
      lock (lockObject)
      {
        foreach (ICacheCollection cache in this.Caches.Values)
        {
          cache.Collect();
        }
      }
		}
	}
}
