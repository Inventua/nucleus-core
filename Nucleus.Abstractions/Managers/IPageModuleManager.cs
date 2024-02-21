using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="PageModule"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IPageModuleManager
	{
    /// <summary>
		/// Create a new <see cref="PageModule"/> with default settings.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method does not save the new <see cref="PageModule"/> unless you call <see cref="Save(Page, PageModule)"/>.
		/// </remarks>
    [Obsolete(message:"Use CreateNew(site, page) instead")]
		public Task<PageModule> CreateNew(Site site);

    /// <summary>
    /// Create a new <see cref="PageModule"/> with default settings.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    /// <remarks>
    /// This method does not save the new <see cref="PageModule"/> unless you call <see cref="Save(Page, PageModule)"/>.
    /// </remarks>
    public Task<PageModule> CreateNew(Site site, Page page);

		/// <summary>
		/// Retrieve an existing <see cref="PageModule"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<PageModule> Get(Guid id);

		/// <summary>
		/// List all installed <see cref="ModuleDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<ModuleDefinition>> ListModuleDefinitions();

		/// <summary>
		/// Create/add default permissions to the specified <see cref="PageModule"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="module"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new <see cref="Permission"/>s are not saved unless you call <see cref="SavePermissions(Page, PageModule)"/>.
		/// </remarks>
		public Task CreatePermissions(Site site, PageModule module, Role role);

    /// <summary>
		/// Save permissions for the specified <see cref="PageModule"/>.
		/// </summary>
    /// <param name="module"></param>
		[Obsolete(message:"Use SavePermissions(page, module) instead.")]
    public Task SavePermissions(PageModule module);

    /// <summary>
    /// Save permissions for the specified <see cref="PageModule"/>.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="module"></param>
    public Task SavePermissions(Page page, PageModule module);

		/// <summary>
		/// List all permissions for the module specified by moduleId.
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public Task<List<Permission>> ListPermissions(PageModule module);

		/// <summary>
		/// Save the specified <see cref="PageModule"/> including <see cref="PageModule.ModuleSettings"/> and <see cref="PageModule.Permissions"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="module"></param>
		public Task Save(Page page, PageModule module);

    /// <summary>
    /// Move the specified <see cref="PageModule"/> to the target pane.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="module"></param>
    /// <param name="pane"></param>
    /// <param name="beforeModule"></param>
    public Task MoveTo(Page page, PageModule module, string pane, PageModule beforeModule);

    /// <summary>
    /// Save the settings for the specified <see cref="PageModule"/>.
    /// </summary>
    /// <param name="module"></param>
    [Obsolete(message: "Use SaveSettings(page, module) instead.")]
    public Task SaveSettings(PageModule module);

    /// <summary>
    /// Save the settings for the specified <see cref="PageModule"/>.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="module"></param>
    public Task SaveSettings(Page page, PageModule module);
		
		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public Task<List<PermissionType>> ListModulePermissionTypes();

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="page"></param>
    /// <param name="moduleId"></param>
		public Task MoveDown(Page page, Guid moduleId);

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="PageModule.SortOrder"/>.
		/// </summary>
    /// <param name="page"></param>
		/// <param name="moduleId"></param>
		public Task MoveUp(Page page, Guid moduleId);

		/// <summary>
		/// Delete the <see cref="PageModule"/> specified by Id.
		/// </summary>
		/// <param name="Id"></param>
		public Task Delete(Guid Id);
	}
}
