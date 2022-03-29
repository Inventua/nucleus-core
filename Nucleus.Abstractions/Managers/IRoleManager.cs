using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Defines the interface for the role manager.
	/// </summary>
	public interface IRoleManager
	{
		/// <summary>
		/// Create a new <see cref="Role"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Role"/> to the database.  Call <see cref="Save(Site, Role)"/> to save the role.
		/// </remarks>
		public Task<Role> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Role> Get(Guid id);

		/// <summary>
		/// List all <see cref="Role"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IEnumerable<Role>> List(Site site);

		/// <summary>
		/// List <see cref="Role"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<Role>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="role"></param>
		public Task Save(Site site, Role role);

		/// <summary>
		/// Delete the specified <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="role"></param>
		public Task Delete(Role role);

	}
}
