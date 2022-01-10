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
	/// Data cache manager
	/// </summary>
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
		/// Nucleus:CacheOptions:[EntityType.Name]Cache
		/// </remarks>
		public CacheCollection<TKey, TModel> Get<TKey, TModel>() where TModel : class;

	}
}
