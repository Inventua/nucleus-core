using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using System.Security.Claims;
using Nucleus.Abstractions;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Page"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IPageManager
	{

		/// <summary>
		/// Create a new <see cref="Page"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Page"/> is not saved to the database until you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public Task<Page> CreateNew(Site site);

		/// <summary>
		/// Retrieve an existing <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Page> Get(Guid id);

		/// <summary>
		/// Retrieve an existing page from the database, specified by site and path.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public Task<Page> Get(Site site, string path);

		/// <summary>
		/// Retrive an existing page from the database.  The returned page is the "owner" of the specified module.
		/// </summary>
		/// <param name="pageModule"></param>
		/// <returns></returns>
		public Task<Page> Get(PageModule pageModule);

		/// <summary>
		/// List all <see cref="PageModule"/>s that are part of the page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public Task<List<PageModule>> ListModules(Page page);

		/// <summary>
		/// Delete the specifed <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="page"></param>
		public Task Delete(Page page);

		/// <summary>
		/// List all <see cref="Page"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IList<Page>> List(Site site);

    /// <summary>
    /// List all <see cref="Page"/>s within the specified site.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    /// <remarks>
    /// This function returns a simple/not fully populated list of pages and is intended for use when populating drop-down
    /// lists, etc.
    /// </remarks>
    public Task<IList<Page>> ListSitePages(Site site);

    /// <summary>
    /// Return a list of all <see cref="Page"/>s for the site which match the specified search term.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="searchTerm"></param>
    /// <param name="userRoles"></param>
    /// <param name="pagingSettings"></param>
    /// <returns></returns>
    public Task<Nucleus.Abstractions.Models.Paging.PagedResult<Page>> Search(Site site, string searchTerm, IEnumerable<Role> userRoles, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Add default permissions to the specifed <see cref="Page"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public Task CreatePermissions(Site site, Page page, Role role);

		/// <summary>
		/// List all permissions for the specified <see cref="Page"/>, sorted by <see cref="Role"/> name and <see cref="PermissionType"/> <see cref="PermissionType.SortOrder"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public Task<List<Permission>> ListPermissions(Page page);

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public Task<List<PermissionType>> ListPagePermissionTypes();

		/// <summary>
		/// Create or update a <see cref="Page"/>, including its <see cref="Page.Permissions"/> and <see cref="Page.Routes"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		public Task Save(Site site, Page page);


		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pageId"></param>
		public Task MoveDown(Site site, Guid pageId);

		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pageId"></param>
		public Task MoveUp(Site site, Guid pageId);

		/// <summary>
		/// Retrieve a "tree" representation of site pages, ignoring permissions and the disabled and show in menu
		/// flags.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="parentPage"></param>
		/// <param name="user"></param>
		/// <param name="levels"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is intended for page selection controls in admin/control panel pages, where all pages 
		/// must be available regardless of settings.
		/// </remarks>
		public Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels);

		/// <summary>
		/// Retrieve a "tree" representation of site pages, ignoring permissions and the disabled and show in menu
		/// flags.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="parentPage"></param>
		/// <param name="user"></param>
		/// <param name="selectedPageId"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is intended for page selection controls in admin/control panel pages, where all pages 
		/// must be available regardless of settings.
		/// This overload reads page levels "down to" the specifed selectedPageId.
		/// </remarks>
		public Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, Guid? selectedPageId);

		/// <summary>
		/// Retrieve a "tree" representation of site pages, with control over whether to ignore permissions, disabled flag and the show in menu flag
		/// flags.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="parentPage"></param>
		/// <param name="user"></param>
		/// <param name="levels"></param>
		/// <param name="ignoreDisabled"></param>
		/// <param name="ignorePermissions"></param>
		/// <param name="ignoreShowInMenu"></param>
		/// <returns></returns>
		public Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels, Boolean ignorePermissions, Boolean ignoreDisabled, Boolean ignoreShowInMenu);

		/// <summary>
		/// Retrieve a "tree" representation of site pages which the specified user has view permissions for.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="parentPage"></param>
		/// <param name="user"></param>
		/// <param name="ignoreSettings"></param>
		/// <returns></returns>
		public Task<PageMenu> GetMenu(Site site, Page parentPage, ClaimsPrincipal user, Boolean ignoreSettings);

		/// <summary>
		/// Copy permissions from the specified <paramref name="page"/> to its descendants.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
    /// <param name="user"></param>
		/// <param name="operation">
		/// If <paramref name="operation"/> is <see cref="IPageManager.CopyPermissionOperation.Replace"/> overwrite all permissions of descendant pages.  
		/// if <paramref name="operation"/> is <see cref="IPageManager.CopyPermissionOperation.Merge"/>, merge the descendant pages permissions with the specified 
		/// <paramref name="page"/> permissions.		
		/// </param>
		/// <returns></returns>
		public Task<Boolean> CopyPermissionsToDescendants(Site site, Page page, ClaimsPrincipal user, CopyPermissionOperation operation);

  }
}
