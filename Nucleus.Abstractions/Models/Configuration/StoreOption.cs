using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to read configuration data for the extension store(s).
	/// </summary>
	public class Store
	{
		/// <summary>
		/// Store Url.  
		/// </summary>
		public string Url { get; private set; }

		/// <summary>
		/// A friendly name for the store.
		/// </summary>
		public string Name { get; private set; }

    /// <summary>
    /// Default Store entry, used by CoreServiceExtensions.ConfigureStoreOptions
    /// </summary>
    public static Store Default = new() { Name = "Nucleus Extensions Store", Url = "https://www.nucleus-cms.com/store" };
	}
}
