using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="RoleGroup"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IRoleGroupManager
	{
		/// <summary>
		/// Create a new <see cref="RoleGroup"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="RoleGroup"/> to the database.  Call <see cref="Save(Site, RoleGroup)"/> to save the role group.
		/// </remarks>
		public Task<RoleGroup> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<RoleGroup> Get(Guid id);

    /// <summary>
		/// Retrieve an existing <see cref="RoleGroup"/> from the database.
		/// </summary>
    /// <param name="site"></param>
    /// <param name="name"></param>
		/// <returns></returns>
		public Task<RoleGroup> GetByName(Site site, string name);

    /// <summary>
    /// List all <see cref="RoleGroup"/>s for the specified site.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public Task<IEnumerable<RoleGroup>> List(Site site);

		/// <summary>
		/// List paged <see cref="RoleGroup"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<RoleGroup>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="RoleGroup"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="roleGroup"></param>
		public Task Save(Site site, RoleGroup roleGroup);

		/// <summary>
		/// Delete the specified <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="roleGroup"></param>
		public Task Delete(RoleGroup roleGroup);
	}
}
