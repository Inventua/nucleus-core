using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions.Models.Cache
{
	/// <summary>
	/// Marker-only interface used so that extensions can add their own cache collections.
	/// </summary>
	public interface ICacheCollection { }

	/// <summary>
	/// Stores a collection of cached entities keys and values.  Provides operations to populate and retrieve, remove and clear items the cache.
	/// </summary>
	/// <typeparam name="TKey">Key type</typeparam>
	/// <typeparam name="TModel">Item type</typeparam>
	/// <remarks>
	/// Items are wrapped in a CacheItem object in order to apply a cache item expiry date.  This class has no Add method, instead the Get
	/// method has a Func parameters which contains a lambda which is invoked to get an instance of the item, if it is not already present 
	/// in the cache.
	/// </remarks>
	public class CacheCollection<TKey, TModel> : ICacheCollection 
	{		
		private ConcurrentDictionary<TKey, CacheItem<TModel>> Cache { get; } = new();
		
		// stores the collection options (expiry date and capacity)
		private CacheOption Options { get; }

		/// <summary>
		/// Initialize a new instance of the CacheCollection class using the options provided.
		/// </summary>
		/// <param name="options"></param>
		public CacheCollection(CacheOption options)
		{
			this.Options=options;
		}

		/// <summary>
		/// Get the item from the cache with the specified key, or if it not present in the cache, read it (using itemReader), add it to 
		/// the cache and return it.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="itemReader"></param>
		/// <returns>The item that matches the specified key.</returns>
		public TModel Get(TKey id, Func<TKey, TModel> itemReader)
		{
			TModel result = this.Get(id);
			
			if (result == null)
			{
				result = itemReader.Invoke(id);
				this.Add(id, result);
			}

			return result;
		}

		/// <summary>
		/// Get the item from the cache with the specified key.  
		/// </summary>
		/// <param name="id"></param>
		/// <returns>The item with the specified key, or null if it not in the cache, or the cache item has expired.</returns>
		/// <remarks>
		/// This function is private.  Callers should use Get(TKey id, Func&lt;TKey, TModel&gt; itemReader).  If the cache item expiry date has
		/// been reached, this function removes it from the cache and returns null.
		/// </remarks>
		private TModel Get(TKey id)
		{
			this.Cache.TryGetValue(id, out CacheItem<TModel> result);

			if (result == null)
			{
				return default;
			}
			else
			{
				if (result.Expires >= DateTime.UtcNow)
				{
					return result.Item;
				}
				else
				{
					Remove(id);
					return default; 
				}
			}			
		}
				
		/// <summary>
		/// Add the specified key/item to the cache.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="item"></param>
		/// <remarks>
		/// This function is private.  Items are added to the cache by the .Get function.  If the cache reaches capacity, the oldest cache item
		/// is removed.
		/// </remarks>		
		private void Add(TKey id, TModel item)
		{
			Collect();

			if (item == null)
			{
				this.Remove(id);
				return;
			}

			if (this.Cache.ContainsKey(id))
			{
				// replace item
				this.Cache[id] = new CacheItem<TModel>(item, DateTime.UtcNow.Add(this.Options.ExpiryTime));
			}
			else
			{
				// insert item
				this.Cache.TryAdd(id, new CacheItem<TModel>(item, DateTime.UtcNow.Add(this.Options.ExpiryTime)));

				if (this.Cache.Count > this.Options.Capacity)
				{
					// Remove oldest
					KeyValuePair<TKey, CacheItem<TModel>> oldestItem = this.Cache.OrderBy(KeyValue => KeyValue.Value.Expires).FirstOrDefault();
					this.Cache.TryRemove(oldestItem);
				}
			}			
		}

		/// <summary>
		/// Remove an item from the cache, specifed by key.
		/// </summary>
		/// <param name="key"></param>
		public void Remove(TKey key)
		{
			this.Cache.TryRemove(key, out _);
		}

		/// <summary>
		/// Remove all items from the cache.
		/// </summary>
		public void Clear()
		{
			this.Cache.Clear();
		}

		/// <summary>
		/// Remove expired items from the cache.
		/// </summary>
		public void Collect()
		{
			IEnumerable<KeyValuePair<TKey, CacheItem<TModel>>> items = this.Cache.Where(KeyValue => KeyValue.Value.Expires < DateTime.UtcNow);

			if (items.Any())
			{
				foreach (KeyValuePair<TKey, CacheItem<TModel>> item in items)
				{
					this.Cache.TryRemove(item);
				}
			}
		}
	}
}
