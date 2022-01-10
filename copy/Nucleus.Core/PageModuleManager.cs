using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="PageModule"/>s.
	/// </summary>
	public class PageModuleManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }

		public PageModuleManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{			
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="PageModule"/> with default settings.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method does not save the new <see cref="PageModule"/> unless you call <see cref="Save(Page, PageModule)"/>.
		/// </remarks>
		public PageModule CreateNew(Site site)
		{
			PageModule result = new();

			// Add permissions (administrator-view/edit)				
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListModulePermissionTypes();
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.AllowAccess = true;
					permission.PermissionType = permissionType;
					permission.Role = site.AdministratorsRole;

					permissions.Add(permission);
				}

				result.Permissions.AddRange(permissions);
			}

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="PageModule"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public PageModule Get(Guid id)
		{
			return this.CacheManager.PageModuleCache.Get(id, id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return provider.GetPageModule(id);
				}
			});
		}

		/// <summary>
		/// List all installed <see cref="ModuleDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ModuleDefinition> ListModuleDefinitions()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListModuleDefinitions().OrderBy(definition => definition.FriendlyName);
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
		public void CreatePermissions(PageModule module, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListModulePermissionTypes();
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.AllowAccess = false;
					permission.PermissionType = permissionType;
					permission.Role = role;

					permissions.Add(permission);
				}

				module.Permissions.AddRange(permissions);
			}
		}

		/// <summary>
		/// Save permissions for the specified <see cref="PageModule"/>.
		/// </summary>
		/// <param name="module"></param>
		public void SavePermissions(PageModule module)
		{			
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<Permission> originalPermissions = provider.ListPageModulePermissions(module.Id);

				provider.SavePageModulePermissions(module.Id, module.Permissions, originalPermissions);
				this.CacheManager.PageModuleCache.Remove(module.Id);
				// Modules are cached as part of their parent page, so we have invalidate the cache for the page
				this.CacheManager.PageCache.Remove(this.GetPageId(module));
			}
		}

		/// <summary>
		/// List all permissions for the module specified by moduleId.
		/// </summary>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		public List<Permission> ListPermissions(PageModule module)
		{
			List<Permission> result = new();

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListModulePermissionTypes();
				List<Permission> permissions = provider.ListPageModulePermissions(module.Id);

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
		public void Save(Page page, PageModule module)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SavePageModule(page.Id, module);
				this.CacheManager.PageModuleCache.Remove(module.Id);
				// Modules are cached as part of their parent page, so we have invalidate the cache for the page
				this.CacheManager.PageCache.Remove(this.GetPageId(module));
			}
		}

		/// <summary>
		/// Save the settings for the specified <see cref="PageModule"/>.
		/// </summary>
		/// <param name="module"></param>
		public void SaveSettings(PageModule module)
		{
			using (DataProviders.ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SavePageModuleSettings(module.Id, module.ModuleSettings);
				this.CacheManager.PageModuleCache.Remove(module.Id);
								
				// Modules are cached as part of their parent page, so we have invalidate the cache for the page
				this.CacheManager.PageCache.Remove(this.GetPageId(module));
			}
		}

		/// <summary>
		/// Get the <see cref="Page"/> Id for the specified <see cref="PageModule"/>.
		/// </summary>
		/// <param name="pageModule"></param>
		/// <returns></returns>
		public Guid GetPageId(PageModule pageModule)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.GetPageModulePageId(pageModule);				
			}
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public List<PermissionType> ListModulePermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListModulePermissionTypes().OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}


		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public void MoveDown(Guid id)
		{
			PageModule previousModule = null;
			PageModule thisModule = null;
			Guid pageId;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisModule = this.Get(id);
				pageId = GetPageId(thisModule);

				List<PageModule> modules = provider.ListPageModules(pageId).Where(module => module.Pane == thisModule.Pane).ToList(); ;

				modules.Reverse();
				foreach (PageModule module in modules)
				{
					if (module.Id == id)
					{
						if (previousModule != null)
						{
							long temp = module.SortOrder;
							module.SortOrder = previousModule.SortOrder;
							previousModule.SortOrder = temp;

							provider.SavePageModule(pageId, previousModule);
							provider.SavePageModule(pageId, module);

							this.CacheManager.PageModuleCache.Remove(previousModule.Id);
							this.CacheManager.PageModuleCache.Remove(module.Id);
							// Modules are cached as part of their parent page, so we have invalidate the cache for the page
							this.CacheManager.PageCache.Remove(this.GetPageId(module));
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
		/// <param name="id"></param>
		public void MoveUp(Guid id)
		{
			PageModule previousModule = null;
			PageModule thisModule = null;
			Guid pageId;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisModule = provider.GetPageModule(id);
				pageId = GetPageId(thisModule);

				List<PageModule> modules = provider.ListPageModules(pageId).Where(module => module.Pane == thisModule.Pane).ToList(); ;

				foreach (PageModule module in modules)
				{
					if (module.Id == id)
					{
						if (previousModule != null)
						{
							long temp = module.SortOrder;
							module.SortOrder = previousModule.SortOrder;
							previousModule.SortOrder = temp;

							provider.SavePageModule(pageId, previousModule);
							provider.SavePageModule(pageId, module);
							
							this.CacheManager.PageModuleCache.Remove(previousModule.Id);
							this.CacheManager.PageModuleCache.Remove(module.Id);
							// Modules are cached as part of their parent page, so we have invalidate the cache for the page
							this.CacheManager.PageCache.Remove(this.GetPageId(module));
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
		public void Delete(Guid Id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				PageModule module = provider.GetPageModule(Id);

				if (module != null)
				{
					Guid pageId = this.GetPageId(new PageModule() { Id = Id });
					provider.DeletePageModule(module);
					this.CacheManager.PageModuleCache.Remove(Id);
					// Modules are cached as part of their parent page, so we invalidate the cache for the page
					this.CacheManager.PageCache.Remove(pageId);
				}			
			}
		}
	}
}
