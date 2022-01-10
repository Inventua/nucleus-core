using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents the configurable options for a CacheCollection.
	/// </summary>
	/// <remarks>
	/// CacheOptions are configured in appSettings.json.  Each cache has an item in the Nucleus:CacheOptions section.
	/// </remarks>
	/// <example>
	/// "CacheOptions": {
	///   "PageCache": { "Capacity": 500, "ExpiryTimeMinutes": 5 }
	///  }
	///</example>
	public class CacheOption
	{
		private const int DEFAULT_CAPACITY = 1000;
		private const int DEFAULT_EXPIRYTIMEMINUTES = 5;

		/// <summary>
		/// The maximum number of items that can be help in the cache.  When the capacity is reached, every time a new
		/// item is added, the oldest item is removed from the cache.
		/// </summary>
		public int Capacity { get; private set; } = DEFAULT_CAPACITY;

		/// <summary>
		/// The cache item expiry time.  Items are automatically removed from the CacheCollection when the expiry time is reached.
		/// </summary>
		public TimeSpan ExpiryTime { get; private set; } = TimeSpan.FromMinutes(DEFAULT_EXPIRYTIMEMINUTES);
	}
}
