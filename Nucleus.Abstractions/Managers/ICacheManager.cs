using Nucleus.Abstractions.Models.Cache;

namespace Nucleus.Abstractions.Managers
{
  /// <summary>
  /// Data cache manager.
  /// </summary>
  /// <remarks>
  /// Get an instance of this class from dependency injection by including a parameter in your class constructor.
  /// </remarks>
  /// <example>
  /// public class MyClass
  /// {
  ///		private ICacheManager CacheManager { get; }
  ///		public MyClass(ICacheManager cacheManager, Context context)
  ///		{
  ///			this.CacheManager = cacheManager;
  ///		}
  ///	}
  /// </example>
  /// <seealso href="https://www.nucleus-cms.com/developers/caching-data/">Caching Data</seealso>
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
		public CacheCollection<TKey, TModel> Get<TKey, TModel>([System.Runtime.CompilerServices.CallerMemberName] string caller = "Default");

		/// <summary>
		/// Remove expired items from all caches.
		/// </summary>
		public void Collect();
	}
}
