﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions.Models.Cache;

/// <summary>
/// Non-generic interface for the <see cref="CacheCollection{TKey, TModel}"/> class, used when retrieving items from the cache (when the
/// full generic type is not known).
/// </summary>
public interface ICacheCollection
{
  /// <summary>
  /// Remove expired items from the cache.
  /// </summary>
  public void Collect();

  /// <summary>
  /// Remove all items from the cache.
  /// </summary>
  public void Clear();

  /// <summary>
  /// Cache name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Return the number of items in the cache.
  /// </summary>
  public int Count { get; }

  /// <summary>
  ///  Cache options.
  /// </summary>
  public CacheOption Options { get; }
}

/// <summary>
/// Stores a collection of cached entities keys and values.  Provides operations to populate and retrieve, remove and clear items the cache.
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TModel">Item type</typeparam>
/// <remarks>
/// Items are wrapped in a CacheItem object in order to apply a cache item expiry date.  This class has no Add method, instead the Get
/// method has a Func parameter which contains an delegate which is invoked to get an instance of the item, if it is not already present 
/// in the cache.
/// </remarks>
public class CacheCollection<TKey, TModel> : ICacheCollection
{
  private ConcurrentDictionary<TKey, CacheItem<TModel>> Cache { get; } = new();

  /// <summary>
  /// Cache name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Stores the collection options (expiry date and capacity) 
  /// </summary>
  public CacheOption Options { get; }

  private ILogger<ICacheManager> Logger { get; }

  /// <summary>
  /// Initialize a new instance of the CacheCollection class using the options provided.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="logger"></param>
  /// <param name="options"></param>
  public CacheCollection(string name, ILogger<ICacheManager> logger, CacheOption options)
  {
    this.Name = name;
    this.Options = options;
    this.Logger = logger;
  }

  /// <summary>
  /// Return the number of items in the cache.
  /// </summary>
  public int Count => this.Cache.Count;

  /// <summary>
  /// Get the item from the cache with the specified key, or if it not present in the cache, read it (using itemReader), add it to 
  /// the cache and return it.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="itemReader"></param>
  /// <returns>The item that matches the specified key.</returns>
  public TModel Get(TKey key, Func<TKey, TModel> itemReader)
  {
    TModel result = this.Get(key);
    return IsNullOrEmpty(result) ? this.Add(key, itemReader.Invoke(key)) : result;
  }

  /// <summary>
  /// Get the item from the cache with the specified key, or if it not present in the cache, read it (using itemReader), add it to 
  /// the cache and return it.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="itemReader"></param>
  /// <returns>The item that matches the specified key.</returns>
  public async Task<TModel> GetAsync(TKey key, Func<TKey, Task<TModel>> itemReader)
  {
    TModel result = this.Get(key);
    return IsNullOrEmpty(result) ? this.Add(key, await itemReader.Invoke(key)) : result;
  }

  /// <summary>
  /// Get the item from the cache with the specified key.  
  /// </summary>
  /// <param name="key"></param>
  /// <returns>The item with the specified key, or null if it not in the cache, or the cache item has expired.</returns>
  /// <remarks>
  /// This function is private.  Callers should use Get(TKey id, Func&lt;TKey, TModel&gt; itemReader).  If the cache item expiry date has
  /// been reached, this function removes it from the cache and returns null.
  /// </remarks>
  private TModel Get(TKey key)
  {
    if (this.Cache.TryGetValue(key, out CacheItem<TModel> result))
    {
      if (result != null)
      {
        this.Logger?.LogTrace("Checking cache '{name}' for '{type}' with key {key}.", this.Name, typeof(TModel).Name, key);
        if (result.Expires >= DateTime.UtcNow)
        {
          this.Logger?.LogTrace("Found valid cache entry for '{type}' with key {key}.", typeof(TModel).Name, key);
          return result.Item;
        }
        else
        {          
          this.Logger?.LogDebug("Removed expired '{type}' with key '{key}' from cache '{name}'.", typeof(TModel).Name, key, this.Name);
          Remove(key);
        }
      }
    }

    return default;
  }

  private Boolean IsNullOrEmpty(TModel value)
  {
    if (value == null) return true;

    if (value.GetType() == typeof(Guid))
    {
      return value.Equals(Guid.Empty);
    }

    if (value.GetType() == typeof(String))
    {
      return String.IsNullOrEmpty(value as string);
    }

    return false;
  }

  /// <summary>
  /// Add the specified key/item to the cache.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="item"></param>
  /// <remarks>
  /// This function is private.  Items are added to the cache by the .Get function.  If the cache reaches capacity, the oldest cache item
  /// is removed.
  /// </remarks>		
  private TModel Add(TKey key, TModel item)
  {
    Collect();

    if (IsNullOrEmpty(item))
    {
      // this mostly happens when .Get/.GetAsync invokes itemReader and it returns a null result because the database record does not exist.
      this.Remove(key);
    }
    else
    {
      if (this.Cache.TryGetValue(key, out CacheItem<TModel> value))
      {
        // replace item
        value.Replace(item, DateTime.UtcNow.Add(this.Options.ExpiryTime));
      }
      else
      {
        // insert item
        this.Cache.TryAdd(key, new CacheItem<TModel>(item, DateTime.UtcNow.Add(this.Options.ExpiryTime)));

        if (this.Cache.Count > this.Options.Capacity)
        {
          // Remove oldest
          KeyValuePair<TKey, CacheItem<TModel>> oldestItem = this.Cache.OrderBy(item => item.Value.Expires).FirstOrDefault();
          this.Cache.TryRemove(oldestItem);
        }
      }
    }

    return item;
  }

  /// <summary>
  /// Override the expiry time for an item from the cache, specifed by key.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="time"></param>
  public void Expire(TKey key, TimeSpan time)
  {
    if (this.Cache.TryGetValue(key, out CacheItem<TModel> item))
    {
      item.Expire(time);
    }
  }

  /// <summary>
  /// Remove an item from the cache, specified by key.
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
