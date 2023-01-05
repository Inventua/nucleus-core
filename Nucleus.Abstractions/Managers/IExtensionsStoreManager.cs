using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Nucleus.Abstractions.Models.ExtensionsStoreSettings"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IExtensionsStoreManager
  {
		/// <summary>
		/// Create a new <see cref="Nucleus.Abstractions.Models.ExtensionsStoreSettings"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Nucleus.Abstractions.Models.ExtensionsStoreSettings"/> to the database.  Call <see cref="Save(ExtensionsStoreSettings)"/> to save the list.
		/// </remarks>
		public Task<ExtensionsStoreSettings> CreateNew(string storeUrl, ClaimsPrincipal user);

		/// <summary>
		/// Retrieve an existing <see cref="Nucleus.Abstractions.Models.ExtensionsStoreSettings"/> from the database.
		/// </summary>
		/// <param name="storeUrl"></param>
		/// <returns></returns>
		public Task<ExtensionsStoreSettings> Get(string storeUrl);

		/// <summary>
		/// Create or update the specified <see cref="Nucleus.Abstractions.Models.ExtensionsStoreSettings"/>.
		/// </summary>
		/// <param name="settings"></param>
		public Task Save(ExtensionsStoreSettings settings);


	}
}
