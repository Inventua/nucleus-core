using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Nucleus.Core;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Page"/>s.
	/// </summary>
	public class PageManager
	{
		
		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }
		
		public PageManager (DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{			
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="Page"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Page"/> is not saved to the database until you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public Page CreateNew(Site site)
		{
			Page result = new();

			// default route (url)
			result.Routes.Add(new PageRoute());

			// permissions (add administrator role - view/edit)				
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListPagePermissionTypes();
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
		/// Retrieve an existing <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Page Get(Guid id)
		{
			return this.CacheManager.PageCache.Get(id, id => 
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return provider.GetPage(id);					
				}
			});
		}

		/// <summary>
		/// Retrieve an existing page from the database, specified by site and path.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public Page Get(Site site, string path)
		{
			if (path.EndsWith('/') && path != "/")
			{
				path = path.Substring(0, path.Length - 1);
			}

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				Guid pageId = provider.FindPage(site, path);
				if (pageId == Guid.Empty)
				{
					return null;
				}
				else
				{
					return Get(pageId);
				}
			}
		}

		/// <summary>
		/// Retrive an existing page from the database.  The returned page is the "owner" of the specified module.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public Page Get(PageModule page)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				Guid pageId = provider.GetPageModulePageId(page);
				if (pageId == Guid.Empty)
				{
					return null;
				}
				else
				{
					return Get(pageId);
				}
			}
		}

		/// <summary>
		/// List all <see cref="PageModule"/>s that are part of the page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public List<PageModule> ListModules(Page page)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListPageModules(page.Id);
			}
		}

		/// <summary>
		/// Delete the specifed <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="page"></param>
		public void Delete(Page page)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeletePage(page);
				this.CacheManager.PageCache.Remove(page.Id);
				this.CacheManager.PageMenuCache.Clear();
			}
		}

		/// <summary>
		/// List all <see cref="Page"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Page> List(Site site)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListPages(site.Id);
			}
		}

		/// <summary>
		/// Return a list of all <see cref="Page"/>s for the site which match the specified search term.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		public IEnumerable<Page> Search(Site site, string searchTerm)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.SearchPages(site.Id, searchTerm);
			}
		}

		/// <summary>
		/// Add default permissions to the specifed <see cref="Page"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public void CreatePermissions(Page page, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				{
					List<PermissionType> permissionTypes = provider.ListPagePermissionTypes();
					List<Permission> permissions = new();

					foreach (PermissionType permissionType in permissionTypes)
					{
						Permission permission = new();
						permission.AllowAccess = false;
						permission.PermissionType = permissionType;
						permission.Role = role;

						permissions.Add(permission);
					}
					
					page.Permissions.AddRange(permissions);
				}
			}			
		}

		/// <summary>
		/// List all permissions for the specified <see cref="Page"/>, sorted by <see cref="Role"/> name and <see cref="PermissionType"/> <see cref="PermissionType.SortOrder"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public List<Permission> ListPermissions(Page page)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListPagePermissionTypes();
				List<Permission> permissions = provider.ListPagePermissions(page.Id);
				Dictionary<Role, IList<Permission>> results = new();

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

				//foreach (Role role in permissions.Select((permission) => permission.Role).ToList())
				//{
				//	List<Permission> rolePermissions = permissions.Where (permission=>permission.Role.Id == role.Id).OrderBy(permission=>permission.PermissionType.SortOrder).ToList();
				//	results.Add(role, rolePermissions);
				//}

				return permissions;

					//return permissions.OrderBy((permission) => permission.Role.Name).ThenBy((permission) => permission.PermissionType.SortOrder).ToList();
			}
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public List<PermissionType> ListPagePermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListPagePermissionTypes().OrderBy(permissionType => permissionType.SortOrder).ToList(); 
			}
		}

		/// <summary>
		/// Create or update a <see cref="Page"/>, including its <see cref="Page.Permissions"/> and <see cref="Page.Routes"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		public void Save(Site site, Page page)
		{			
			// If no default Url was selected and there's more than one Url, set the first Url as the default
			if (page.DefaultPageRouteId == Guid.Empty && page.Routes.Count > 0)
			{
				page.DefaultPageRouteId = page.Routes[0].Id;
			}
			
			// check for reserved routes
			foreach (PageRoute route in page.Routes)
			{
				if (Constants.RESERVED_ROUTES.Contains(route.Path.StartsWith('/') ? route.Path.Substring(1): route.Path, StringComparer.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException($"The page route '{route.Path}' is reserved.");
				}
			}

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SavePage(site, page);
				this.CacheManager.PageCache.Remove(page.Id);
				this.CacheManager.PageMenuCache.Clear();
			}			
		}


		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="pageId"></param>
		public void MoveDown(Site site, Guid pageId)
		{
			Page previousPage = null;
			Page thisPage = null;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisPage = this.Get(pageId);
				
				List<Page> siblingPages = provider.ListPages(site.Id, !thisPage.ParentId.HasValue ? Guid.Empty : thisPage.ParentId.Value);

				siblingPages.Reverse();
				foreach (Page page in siblingPages)
				{
					if (page.Id == pageId)
					{
						if (previousPage != null)
						{
							long temp = page.SortOrder;
							page.SortOrder = previousPage.SortOrder;
							previousPage.SortOrder = temp;

							if (previousPage.SortOrder == page.SortOrder)
							{
								page.SortOrder += 10;
							}

							provider.SavePage(site, previousPage);
							provider.SavePage(site, page);

							this.CacheManager.PageCache.Remove(previousPage.Id);
							this.CacheManager.PageCache.Remove(page.Id);
							break;
						}
					}
					else
					{
						previousPage = page;
					}
				}
			}
		}

		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="pageId"></param>
		public void MoveUp(Site site, Guid pageId)
		{
			Page previousPage = null;
			Page thisPage = null;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisPage = provider.GetPage(pageId);

				List<Page> siblingPages = provider.ListPages(site.Id, !thisPage.ParentId.HasValue ? Guid.Empty : thisPage.ParentId.Value);

				foreach (Page page in siblingPages)
				{
					if (page.Id == pageId)
					{
						if (previousPage != null)
						{
							long temp = page.SortOrder;
							page.SortOrder = previousPage.SortOrder;
							previousPage.SortOrder = temp;

							if (previousPage.SortOrder == page.SortOrder)
							{
								previousPage.SortOrder += 10;
							}

							provider.SavePage(site, previousPage);
							provider.SavePage(site, page);

							this.CacheManager.PageCache.Remove(previousPage.Id);
							this.CacheManager.PageCache.Remove(page.Id);
							break;
						}
					}
					else
					{
						previousPage = page;
					}
				}
			}
		}

		public PageMenu GetAdminMenu(Site site, ClaimsPrincipal user)
		{
			// read from database
			return new PageMenu(null, GetPageMenuChildren(site, null, user, true));			
		}


		public PageMenu GetMenu(Site site, Page parentPage, ClaimsPrincipal user, Boolean ignoreSettings)
		{
			string key;
			if (user.IsSystemAdministrator())
			{
				key = $"system-administrator:{ignoreSettings}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
			}
			else if (user.IsSiteAdmin(site))
			{
				key = $"site-administrator:{site.Id}:{ignoreSettings}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
			}
			else
			{
				key = String.Join("|", user.Claims.Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value)) + $":{site.Id}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
			}

			return this.CacheManager.PageMenuCache.Get(key, key => 
			{ 
				// read from database
				return new PageMenu(null, GetPageMenuChildren(site, parentPage, user, ignoreSettings));				
			});	
		}

		private IEnumerable<PageMenu> GetPageMenuChildren(Site site, Page parentPage, ClaimsPrincipal user, Boolean ignoreSettings)
		{
			List<PageMenu> children = new();

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				// read from database
				foreach (Page child in provider.ListPages(site.Id, parentPage == null ? Guid.Empty : parentPage.Id))
				{
					//if (ignoreSettings || (!child.Disabled && child.ShowInMenu && child.Permissions.Where(permission => permission.IsValid(site, user)).Any()))
					if (ignoreSettings || (!child.Disabled && child.ShowInMenu && user.HasViewPermission(site, child)))
					{
						children.Add(new PageMenu(child, GetPageMenuChildren(site, child, user, ignoreSettings)));
					}
				}
			}
			

			return children;
		}
	}
}
