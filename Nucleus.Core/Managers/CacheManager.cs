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

namespace Nucleus.Core.Managers
{
	public class CacheManager : ICacheManager
	{
		private Dictionary<string, ICacheCollection> Caches { get; } = new();
		private IConfiguration Configuration { get; }
		private static readonly object lockObject = new();

		public CacheManager(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		private static string CacheKey<TKey, TModel>()
		{
			return $"{typeof(TKey).FullName} {typeof(TModel).FullName}";
		}

		private CacheCollection<TKey, TModel> Add<TKey, TModel>(string configItemName) where TModel : class
		{

			if (!this.Caches.ContainsKey(CacheKey<TKey, TModel>()))
			{
				lock(lockObject)
				{
					if (!this.Caches.ContainsKey(CacheKey<TKey, TModel>()))
					{
						CacheOption options = new();
						//this.Configuration.GetSection($"{CacheOption.Section}:{typeof(TModel).Name}Cache").Bind(options, binderOptions => binderOptions.BindNonPublicProperties = true);
						this.Configuration.GetSection($"{CacheOption.Section}:{configItemName}").Bind(options, binderOptions => binderOptions.BindNonPublicProperties = true);

						CacheCollection<TKey, TModel> result = new(options);
						this.Caches.Add(CacheKey<TKey, TModel>(), result);
						return result;
					}
				}				
			}

			return this.Caches[CacheKey<TKey, TModel>()] as CacheCollection<TKey, TModel>;
		}

		public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "") where TModel : class
		{
			if (this.Caches.TryGetValue(CacheKey<TKey, TModel>(), out ICacheCollection result))
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
