using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Cache
{
	/// <summary>
	/// Represents a cached item, with an expiry date.
	/// </summary>
	/// <typeparam name="T">The type of the cached item.</typeparam>	
	public class CacheItem<T> where T : class
	{
		/// <summary>
		/// Expiry date/time for the cache item.
		/// </summary>
		public DateTime Expires { get; }

		/// <summary>
		/// The cached item.
		/// </summary>
		public WeakReference<T> Item { get; }

		/// <summary>
		/// Initialize a new instance of the CacheItem class.  CacheItem instances should be created by the CacheCollection object only. 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="expires"></param>
		internal CacheItem(T item, DateTime expires)
		{
			this.Item = new WeakReference<T>(item);
			this.Expires = expires;
		}
	}
}
