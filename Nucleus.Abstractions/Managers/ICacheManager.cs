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

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Data cache manager.
	/// </summary>
	/// <remarks>
	/// Refer to the <see href="https://www.nucleus-cms.com/developers/caching-data/">Caching Data</see> page for more information.
	/// </remarks>
	public interface ICacheManager
	{
		/// <summary>
		/// Get the Cache collection for the specified type and entity, or create and add a new one if it does not exist.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TModel"></typeparam>
		/// <returns></returns>
		/// <remarks>
		/// Cache options for the specified entity are automatically read from configuration.  The configuration file key is 
		/// Nucleus:CacheOptions:[caller-name]Cache.  The caller should be an extension method, see 
		/// <see href="https://www.nucleus-cms.com/developers/caching-data/">Caching Data</see> for details.
		/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
		/// </remarks>
		public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "Default") where TModel : class;

		/// <summary>
		/// Remove expired items from all caches.
		/// </summary>
		public void Collect();
	}
}
