using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Core;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using System.Security.Cryptography;
using Nucleus.Extensions;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Page"/>s.
	/// </summary>
	public class PageManager : IPageManager
	{

		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public PageManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
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
		public Task<Page> CreateNew(Site site)
		{
			Page result = new();

			// default route (url)
			result.Routes.Add(new PageRoute());

			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Page> Get(Guid id)
		{
			if (id == Guid.Empty) return default;

			return await this.CacheManager.PageCache().GetAsync(id, async id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return await provider.GetPage(id);
				}
			});
		}

		/// <summary>
		/// Retrieve an existing page from the database, specified by site and path.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<Page> Get(Site site, string path)
		{
			// ensure that path starts with '/' and does not end with '/', but if the path is equal to '/' leave as-is.  '/' is
			// normally the route to the site's home page.
			if (path != "/")
			{
				path = String.Concat(!path.StartsWith('/') ? "/" : "", path.EndsWith('/') ? path[0..^1] : path);
			}

			return await FindPage(site, path);
		}

		/// <summary>
		/// Return the page matching the specified path, utilizing the cache.  Return null if there is no match.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		private async Task<Page> FindPage(Site site, string path)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				Guid pageId = await provider.FindPage(site, path);

				if (pageId == Guid.Empty && String.IsNullOrEmpty(path))
				{
					// treat empty local path as "/"
					pageId = await provider.FindPage(site, "/");
				}

				if (pageId == Guid.Empty)
				{
					return null;
				}
				else
				{
					return await Get(pageId);
				}
			}			
		}

		/// <summary>
		/// Retrive an existing page from the database.  The returned page is the "owner" of the specified module.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task<Page> Get(PageModule pageModule)
		{
			return await Get(pageModule.PageId);
		}

		/// <summary>
		/// List all <see cref="PageModule"/>s that are part of the page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task<List<PageModule>> ListModules(Page page)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListPageModules(page.Id);
			}
		}

		/// <summary>
		/// Delete the specifed <see cref="Page"/> from the database.
		/// </summary>
		/// <param name="page"></param>
		public async Task Delete(Page page)
		{
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        // page objects from http requests/which have been created by MVC don't always have a valid site id, so we must get 
        // the site from the database.
        Page existing = await provider.GetPage(page.Id);

        List<Page> childPages = await provider.ListPages(existing.SiteId, page.Id);
        if (childPages.Any())
        {
          throw new InvalidOperationException($"This page has {childPages.Count} child page{(childPages.Count==1 ? "" : "s")}.  You must delete the child page{(childPages.Count == 1 ? "" : "s")} before you can delete this page.");
        }
      }

      using (IPermissionsDataProvider permissionsProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				await permissionsProvider.DeletePermissions(page.Permissions);
			}

			if (page.Modules != null)
			{
				foreach (PageModule module in page.Modules)
				{
					using (IPermissionsDataProvider permissionsProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
					{
						await permissionsProvider.DeletePermissions(module.Permissions);
					}
					using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
					{
						await provider.DeletePageModule(module);
					}
				}
			}

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeletePage(page);
			}

			this.CacheManager.PageCache().Remove(page.Id);
			this.CacheManager.PageMenuCache().Clear();
			this.CacheManager.PageRouteCache().Clear();
		}

		/// <summary>
		/// List all <see cref="Page"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<Page>> List(Site site)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListPages(site.Id);
			}
		}

		/// <summary>
		/// Return a list of all <see cref="Page"/>s for the site which match the specified search term.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Page>> Search(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.SearchPages(site.Id, searchTerm, pagingSettings);
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
		public async Task CreatePermissions(Site site, Page page, Role role)
		{
			Boolean isAnonymousOrAllUsers = role.Equals(site.AnonymousUsersRole) || role.Equals(site.AllUsersRole);

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				{
					List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Page.URN);
					List<Permission> permissions = new();

					foreach (PermissionType permissionType in permissionTypes)
					{
						Permission permission = new();
						permission.Role = role;

						if (isAnonymousOrAllUsers && !permissionType.IsPageViewPermission())
						{
							permission.AllowAccess = false;
							permission.PermissionType = new() { Scope = PermissionType.PermissionScopeNamespaces.Disabled };
						}
						else
						{
							permission.AllowAccess = permissionType.IsPageViewPermission();
							permission.PermissionType = permissionType;
						}

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
		public async Task<List<Permission>> ListPermissions(Page page)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Page.URN);
				List<Permission> permissions = await provider.ListPermissions(page.Id, Page.URN);
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

				return permissions;
			}
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public async Task<List<PermissionType>> ListPagePermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return (await provider.ListPermissionTypes(Page.URN)).OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}

		/// <summary>
		/// Create or update a <see cref="Page"/>, including its <see cref="Page.Permissions"/> and <see cref="Page.Routes"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		public async Task Save(Site site, Page page)
		{
			// check for reserved routes
			foreach (PageRoute route in page.Routes)
			{
				string path = route.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
				if (!String.IsNullOrEmpty(path))
				{
					if (RoutingConstants.RESERVED_ROUTES.Contains(path, StringComparer.OrdinalIgnoreCase))
					{
						throw new InvalidOperationException($"Page routes starting with '{path}' are reserved.");
					}
				}
			}

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SavePage(site, page);
			}

			this.CacheManager.PageCache().Remove(page.Id);
			this.CacheManager.PageMenuCache().Clear();
			this.CacheManager.PageRouteCache().Clear();
		}

		/// <summary>
		/// Ensure that pages have unique sort order.
		/// </summary>
		/// <param name="pages"></param>
		/// <remarks>
		/// Page sort orders can produce duplicates and gaps when pages parents are changed, or pages are deleted.
		/// </remarks>
		private async Task CheckNumbering(Site site, List<Page> pages)
		{
			int sortOrder = 10;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				foreach (Page page in pages)
				{
					if (page.SortOrder != sortOrder)
					{
						page.SortOrder = sortOrder;
						await provider.SavePage(site, page);

						this.CacheManager.PageCache().Remove(page.Id);
					}

					sortOrder += 10;
				}
			}
		}

		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="pageId"></param>
		public async Task MoveDown(Site site, Guid pageId)
		{
			Page previousPage = null;
			Page thisPage;
			List<Page> siblingPages;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisPage = await this.Get(pageId);
				siblingPages = await provider.ListPages(site.Id, thisPage.ParentId);
			}

			await CheckNumbering(site, siblingPages);

			siblingPages.Reverse();
			foreach (Page page in siblingPages)
			{
				if (page.Id == pageId)
				{
					if (previousPage != null)
					{
						int temp = page.SortOrder;
						page.SortOrder = previousPage.SortOrder;
						previousPage.SortOrder = temp;

						if (previousPage.SortOrder == page.SortOrder)
						{
							page.SortOrder += 10;
						}

						using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
						{
							await provider.SavePage(site, previousPage);
						}

						using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
						{
							await provider.SavePage(site, page);
						}

						this.CacheManager.PageCache().Remove(previousPage.Id);
						this.CacheManager.PageCache().Remove(page.Id);
						this.CacheManager.PageMenuCache().Clear();
						break;
					}
				}
				else
				{
					previousPage = page;
				}
			}
		}


		/// <summary>
		/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="Page.SortOrder"/>.
		/// </summary>
		/// <param name="pageId"></param>
		public async Task MoveUp(Site site, Guid pageId)
		{
			Page previousPage = null;
			Page thisPage;
			List<Page> siblingPages;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisPage = await provider.GetPage(pageId);
				siblingPages = await provider.ListPages(site.Id, thisPage.ParentId);
			}

			await CheckNumbering(site, siblingPages);

			foreach (Page page in siblingPages)
			{
				if (page.Id == pageId)
				{
					if (previousPage != null)
					{
						int temp = page.SortOrder;
						page.SortOrder = previousPage.SortOrder;
						previousPage.SortOrder = temp;

						if (previousPage.SortOrder == page.SortOrder)
						{
							previousPage.SortOrder += 10;
						}

						using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
						{
							await provider.SavePage(site, previousPage);
						}

						using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
						{
							await provider.SavePage(site, page);
						}

						this.CacheManager.PageCache().Remove(previousPage.Id);
						this.CacheManager.PageCache().Remove(page.Id);
						this.CacheManager.PageMenuCache().Clear();
						break;
					}
				}
				else
				{
					previousPage = page;
				}
			}
		}

		public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels, Boolean ignorePermissions, Boolean ignoreDisabled, Boolean ignoreShowInMenu)
		{
			// read from database
			PageMenuChildrenResult childrenResult = await GetPageMenuChildren(site, parentPage, user, levels, 0, ignorePermissions, ignoreDisabled, ignoreShowInMenu);
			return new PageMenu(null, childrenResult.Children, childrenResult.HasChildren);
		}

		public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels)
		{
			return await GetAdminMenu(site, parentPage, user, levels, true, true, true);
		}

		public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, Guid? selectedPageId)
		{
			if (selectedPageId.HasValue && selectedPageId != Guid.Empty)
			{
				// use "breadcrumb" logic to fill in the tree up to the selected page
				List<Page> breadcrumbs = new();

				Page breadcrumbPage = await Get(selectedPageId.Value);
				do
				{
					if (breadcrumbPage != null)
					{
						breadcrumbs.Add(breadcrumbPage);
					}

					if (breadcrumbPage.ParentId.HasValue)
					{
						breadcrumbPage = await this.Get(breadcrumbPage.ParentId.Value);
					}
					else
					{
						breadcrumbPage = null;
					}
				} while (breadcrumbPage != null);

				if (breadcrumbs.Any())
				{
					breadcrumbs.Reverse();

					// get the top level
					PageMenu menu = await GetAdminMenu(site, null, user, 1, true, true, true);
					PageMenu subMenu = menu;

					foreach (Page ancestor in breadcrumbs)
					{
						subMenu = subMenu.Children.Where(child => child.Page.Id == ancestor.Id).FirstOrDefault();
						
						PageMenuChildrenResult newChildren = await GetPageMenuChildren(site, subMenu.Page, user, 1, 0, true, true, true);
						subMenu.UpdateChildren(newChildren.Children, newChildren.HasChildren);						
					}

					return menu;
				}
			}
			
			return await GetAdminMenu(site, parentPage, user, 1); ;
		}


		public async Task<PageMenu> GetMenu(Site site, Page parentPage, ClaimsPrincipal user, Boolean ignoreSettings)
		{
			string key;
			if (user.IsSystemAdministrator())
			{
				key = $"system-administrator:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
			}
			else if (user.IsSiteAdmin(site))
			{
				key = $"site-administrator:{site.Id}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
			}
			else
			{
				string roles = String.Join(":", user.Claims.Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value));
				if (String.IsNullOrEmpty(roles))
				{
					roles = "Anonymous User";
				}
				key = String.Join("|", Encode(roles) + $":{site.Id}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}");
			}

			return await this.CacheManager.PageMenuCache().GetAsync(key, async key =>
			{
			// read from database
			PageMenuChildrenResult childrenResult = await GetPageMenuChildren(site, parentPage, user, 0, 0, false, ignoreSettings, ignoreSettings);
				return new PageMenu(null, childrenResult.Children, childrenResult.HasChildren);
			});
		}

		private string Encode(string value)
		{
			byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(value);

			using (MD5 md5 = MD5.Create())
			{
				return BitConverter.ToString(md5.ComputeHash(valueBytes)).Replace("-", "");
			}
		}

		private async Task<PageMenuChildrenResult> GetPageMenuChildren(Site site, Page parentPage, ClaimsPrincipal user, int levels, int thisLevel, Boolean ignorePermissions, Boolean ignoreDisabled, Boolean ignoreShowInMenu)
		{
			List<PageMenu> children = new();

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				// read from database
				foreach (Page child in await provider.ListPages(site.Id, parentPage?.Id))
				{
					if (levels != 0 && thisLevel >= levels) return new PageMenuChildrenResult(true);

					if ((ignoreDisabled || !child.Disabled) && (ignoreShowInMenu || child.ShowInMenu) && (ignorePermissions || user.HasViewPermission(site, child)))
					{
						PageMenuChildrenResult childrenResult = await GetPageMenuChildren(site, child, user, levels, thisLevel + 1, ignorePermissions, ignoreDisabled, ignoreShowInMenu);
						children.Add(new PageMenu(child, childrenResult.Children, childrenResult.HasChildren));
					}
				}
			}

			return new PageMenuChildrenResult(children);
		}

		private class PageMenuChildrenResult
		{
			public Boolean HasChildren { get; set; }
			public IEnumerable<PageMenu> Children { get; set; }

			public PageMenuChildrenResult(Boolean hasChildren)
			{
				this.HasChildren = hasChildren;
			}

			public PageMenuChildrenResult(IEnumerable<PageMenu> children)
			{
				this.Children = children;
				this.HasChildren = this.Children?.Count() != 0;
			}

		}
	}
}
