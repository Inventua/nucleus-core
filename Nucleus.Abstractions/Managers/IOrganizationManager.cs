using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Organization"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IOrganizationManager
	{
		/// <summary>
		/// Create a new <see cref="Organization"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Organization"/> to the database.  Call <see cref="Save(Site, Organization)"/> to save the role group.
		/// </remarks>
		public Task<Organization> CreateNew(Site site);

		/// <summary>
		/// Retrieve an existing <see cref="Organization"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Organization> Get(Guid id);

    /// <summary>
    /// Retrieve an existing <see cref="Organization"/> from the database.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public Task<Organization> GetByName(Site site, string name);

    /// <summary>
    /// Add the specified <see cref="User"/> to the specified <see cref="Organization"/>.  
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="user"></param>
    /// <remarks>
    /// Changes are saved to the database immediately.
    /// </remarks>
    public Task AddUser(Organization organization, OrganizationUser user);

    /// <summary>
    /// Removes the specified <see cref="Role"/> to the specified <see cref="Organization"/>.  
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="user"></param>
    /// <remarks>
    /// Changes are saved to the database immediately.
    /// </remarks>
    public Task RemoveUser(Organization organization, OrganizationUser user);

    /// <summary>
    /// Return whether the specified user is a member (or admin) of the specified organization.
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public Task<Boolean> IsOrganizationMember(Organization organization, ClaimsPrincipal user);
    
    /// <summary>
    /// Return whether the specified user is an administrator of the specified organization.
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public Task<Boolean> IsOrganizationAdministrator(Organization organization, ClaimsPrincipal user);

    /// <summary>
    /// List all <see cref="Organization"/>s which belong to the specified <see cref="Site"/>.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public Task<IList<Organization>> List(Site site);

		/// <summary>
		/// List a page of <see cref="Organization"/>s which belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<Organization>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Returns a list of <see cref="Organization"/>s which match the specified searchTerm.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<Organization>> Search(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="Organization"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="organization"></param>
		public Task Save(Site site, Organization organization);

		/// <summary>
		/// Delete the specified <see cref="Organization"/> from the database.
		/// </summary>
		/// <param name="organization"></param>
		public Task Delete(Organization organization);
    		
	}
}
