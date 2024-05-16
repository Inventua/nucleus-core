using System;

namespace Nucleus.Abstractions.Models.Cache
{
	/// <summary>
	/// Represents a cached item, with an expiry date.
	/// </summary>
	/// <typeparam name="T">The type of the cached item.</typeparam>	
	/// <internal/>
	/// <hidden/>
	internal class CacheItem<T> 
	{
		/// <summary>
		/// Expiry date/time for the cache item.
		/// </summary>
		internal DateTime Expires { get; private set; }

		/// <summary>
		/// The cached item.
		/// </summary>
		internal T Item { get; private set; }

		/// <summary>
		/// Initialize a new instance of the CacheItem class.  CacheItem instances should be created by the CacheCollection object only. 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="expires"></param>
		internal CacheItem(T item, DateTime expires)
		{
			this.Item = item;
			this.Expires = expires;
		}

    /// <summary>
    /// Replace the cached item.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="expires"></param>
    internal void Replace(T item, DateTime expires)
    {
      this.Item = item;
			this.Expires = expires;
    }

    /// <summary>
    /// Override the expiry time for this cache item.
    /// </summary>
    /// <param name="time"></param>
    internal void Expire(TimeSpan time)
    {
      this.Expires = DateTime.UtcNow.Add(time);
    }
	}
}
