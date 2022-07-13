using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Nucleus.Abstractions.Models.ApiKey"/>s.
	/// </summary>
	public interface IApiKeyManager
	{
		/// <summary>
		/// Create a new <see cref="Nucleus.Abstractions.Models.ApiKey"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Nucleus.Abstractions.Models.ApiKey"/> to the database.  Call <see cref="Save(ApiKey)"/> to save the ApiKey.
		/// </remarks>
		public Task<ApiKey> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="Nucleus.Abstractions.Models.ApiKey"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<ApiKey> Get(Guid id);

		/// <summary>
		/// List all <see cref="Nucleus.Abstractions.Models.List"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<ApiKey>> List();

		/// <summary>
		/// List paged <see cref="Nucleus.Abstractions.Models.ApiKey"/>s.
		/// </summary>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey>> List(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="Nucleus.Abstractions.Models.ApiKey"/>.
		/// </summary>
		/// <param name="key"></param>
		public Task Save(ApiKey key);

		/// <summary>
		/// Delete the specified <see cref="Nucleus.Abstractions.Models.ApiKey"/> from the database.
		/// </summary>
		/// <param name="key"></param>
		public Task Delete(ApiKey key);

	}
}
