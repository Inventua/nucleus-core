using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="PageModule"/>s.
	/// </summary>
	public class PageModuleManager : IPageModuleManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public PageModuleManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{			
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="PageModule"/> with default settings.
		/// </summary>
		/// <param name="site"></param>
    /// <param name="page"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method does not save the new <see cref="PageModule"/> unless you call <see cref="Save(Page, PageModule)"/>.
		/// </remarks>
		public Task<PageModule> CreateNew(Site site, Page page)
		{
			PageModule result = new() { PageId = page.Id };
      
			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="PageModule"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<PageModule> Get(Guid id)
		{
			return await this.CacheManager.PageModuleCache().GetAsync(id, async id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return await provider.GetPageModule(id);
				}
			});
		}

		/// <summary>
		/// List all installed <see cref="ModuleDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public async Task<IEnumerable<ModuleDefinition>> ListModuleDefinitions()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return (await provider.ListModuleDefinitions()).OrderBy(definition => definition.FriendlyName);
			}
		}

		/// <summary>
		/// Create/add default permissions to the specified <see cref="PageModule"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new <see cref="Permission"/>s are not saved unless you call <see cref="SavePermissions(PageModule)"/>.
		/// </remarks>
		public async Task CreatePermissions(Site site, PageModule module, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = await provider.ListPermissionTypes(PageModule.URN);
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();

					permission.Role = role;
					permission.AllowAccess = permissionType.IsModuleViewPermission();
					permission.PermissionType = permissionType;
					
					permissions.Add(permission);
				}

				module.Permissions.AddRange(permissions);
			}
		}

		/// <summary>
		/// Save permissions for the specified <see cref="PageModule"/>.
		/// </summary>
		/// <param name="module"></param>
		public async Task SavePermissions(Page page, PageModule module)
		{
      using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<Permission> originalPermissions = await provider.ListPermissions(module.Id, PageModule.URN);
        
				await provider.SavePermissions(module.Id, module.Permissions, originalPermissions);
			}

			InvalidateCache(page, module);
		}

		/// <summary>
		/// List all permissions for the module specified by moduleId.
		/// </summary>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		public async Task<List<Permission>> ListPermissions(PageModule module)
		{
			List<Permission> result = new();

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = await provider .ListPermissionTypes(PageModule.URN);
				List<Permission> permissions = await provider.ListPermissions(module.Id, PageModule.URN);

				// ensure that for each role with any permissions defined, there is a full set of permission types for the role
				foreach (Role role in permissions.Select((permission) => permission.Role).ToList())
				{
					foreach (PermissionType permissionType in permissionTypes)
					{
						if (permissions.Where((permission) => permission?.Role.Id == role.Id && permission?.PermissionType.Id == permissionType.Id).ToList().Count == 0)
						{
							Permission permission = new();
							permission.AllowAccess = false;
							permission.PermissionType = permissionType;
							permission.Role = role;
							permissions.Add(permission);
						}
					}
				}

				result = permissions.OrderBy((permission) => permission.Role.Name).ThenBy((permission) => permission.PermissionType.SortOrder).ToList();
			}

			return result;
		}

		/// <summary>
		/// Save the specified <see cref="PageModule"/> and its <see cref="PageModule.ModuleSettings"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="module"></param>
		public async Task Save(Page page, PageModule module)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SavePageModule(page.Id, module);
			}

      InvalidateCache(page, module);
		}

    /// <summary>
    /// Move the specified <paramref name="module"/> to the specified <paramref name="pane"/>.  If <paramref name="beforeModule"/>
    /// is not null, set the soft index to before it.
    /// </summary>
    /// <param name="module"></param>
    /// <param name="pane"></param>
    /// <param name="beforeModule"></param>
    /// <returns></returns>
    public async Task MoveTo(Page page, PageModule module, string pane, PageModule beforeModule)
    {
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        module.Pane = pane;

        if (beforeModule != null)
        {
          // move before the specified module.  this gets renumbered in steps of 10 later
          module.SortOrder = beforeModule.SortOrder - 1;
        }
        else
        {
          // move to end.  this gets renumbered in steps of 10 later
          module.SortOrder = int.MaxValue;
        }

        await provider.SavePageModule(page.Id, module);
      }

      // re-number the modules in the specified pane
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        List<PageModule> modules = (await provider.ListPageModules(page.Id))
          .Where(module=>module.Pane.Equals(pane, StringComparison.OrdinalIgnoreCase))
          .ToList();
        await CheckNumbering(page.Id, modules);
      }

      InvalidateCache(page, module);
    }

    /// <summary>
    /// Save the settings for the specified <see cref="PageModule"/>.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="module"></param>
    public async Task SaveSettings(Page page, PageModule module)
		{
			using (DataProviders.ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SavePageModuleSettings(module.Id, module.ModuleSettings);
      }

			InvalidateCache(page, module);
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public async Task<List<PermissionType>> ListModulePermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return (await provider.ListPermissionTypes(PageModule.URN)).OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}

		/// <summary>
		/// Ensure that page modules have unique sort order.
		/// </summary>
		/// <param name="pageModules"></param>
		/// <remarks>
		/// Module sort orders can produce duplicates and gaps when pages panes are changed, or modules are deleted.
		/// </remarks>
		private async Task CheckNumbering(Guid pageId, List<PageModule> pageModules)
		{
			int sortOrder = 10;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				foreach (PageModule pageModule in pageModules)
				{
					if (pageModule.SortOrder != sortOrder)
					{
						pageModule.SortOrder = sortOrder;
						
						await provider.SavePageModule(pageId, pageModule);

						this.CacheManager.PageModuleCache().Remove(pageModule.Id);
					}

					sortOrder += 10;
				}
			}
		}

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="PageModule.SortOrder"/>.
		/// </summary>
    /// <param name="page"></param>
		/// <param name="moduleId"></param>
		public async Task MoveDown(Page page, Guid moduleId)
		{
			PageModule previousModule = null;
			PageModule thisModule = null;
			
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisModule = await this.Get(moduleId);
				
				List<PageModule> modules = (await provider.ListPageModules(thisModule.PageId)).Where(module => module.Pane == thisModule.Pane).ToList(); ;
				await CheckNumbering(thisModule.PageId, modules);

				modules.Reverse();

				foreach (PageModule module in modules)
				{
					if (module.Id == moduleId)
					{
						if (previousModule != null)
						{
							int temp = module.SortOrder;
							module.SortOrder = previousModule.SortOrder;
							previousModule.SortOrder = temp;

							await provider.SavePageModule(thisModule.PageId, previousModule);
							await provider.SavePageModule(thisModule.PageId, module);

							this.CacheManager.PageModuleCache().Remove(previousModule.Id);
							InvalidateCache(page, module);
							break;
						}
					}
					else
					{
						previousModule = module;
					}
				}
			}
		}

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="PageModule.SortOrder"/>.
		/// </summary>
    /// <param name="page"></param>
		/// <param name="moduleId"></param>
		public async Task MoveUp(Page page, Guid moduleId)
		{
			PageModule previousModule = null;
			PageModule thisModule = null;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisModule = await provider.GetPageModule(moduleId);

				List<PageModule> modules = (await provider.ListPageModules(thisModule.PageId)).Where(module => module.Pane == thisModule.Pane).ToList(); ;
				await CheckNumbering(thisModule.PageId, modules);

				foreach (PageModule module in modules)
				{
					if (module.Id == moduleId)
					{
						if (previousModule != null)
						{
							int temp = module.SortOrder;
							module.SortOrder = previousModule.SortOrder;
							previousModule.SortOrder = temp;

							await provider.SavePageModule(thisModule.PageId, previousModule);
							await provider.SavePageModule(thisModule.PageId, module);
							
							this.CacheManager.PageModuleCache().Remove(previousModule.Id);
							InvalidateCache(page, module);
							break;
						}
					}
					else
					{
						previousModule = module;
					}
				}
			}
		}

		/// <summary>
		/// Delete the <see cref="PageModule"/> specified by Id.
		/// </summary>
		/// <param name="Id"></param>
		public async Task Delete(Guid Id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				PageModule module = await provider.GetPageModule(Id);

				if (module != null)
				{
					using (IPermissionsDataProvider permissionsProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
					{
						await permissionsProvider.DeletePermissions(module.Permissions);
					}

					await provider.DeletePageModule(module);

					InvalidateCache(new() { Id = module.PageId  }, module);
				}			
			}
		}

		private void InvalidateCache(Page page, PageModule module)
    {
			this.CacheManager.PageModuleCache().Remove(module.Id);
			// Modules are cached as part of their parent page, so we have invalidate the cache for the page
			this.CacheManager.PageCache().Remove(page.Id);
			this.CacheManager.PageRouteCache().Clear();
		}
	}
}
