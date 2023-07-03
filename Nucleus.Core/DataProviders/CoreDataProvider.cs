using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Data.Common;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Security.Claims;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Extensions;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Nucleus data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with entity framework.  
	/// </remarks>
	public class CoreDataProvider : Nucleus.Data.EntityFramework.DataProvider, ILayoutDataProvider, IUserDataProvider, IPermissionsDataProvider, ISessionDataProvider, IMailDataProvider, IScheduledTaskDataProvider, IFileSystemDataProvider, IListDataProvider, IContentDataProvider, IApiKeyDataProvider, IOrganizationDataProvider, IExtensionsStoreDataProvider
  {
		protected IEventDispatcher EventManager { get; }
		protected new CoreDataProviderDbContext Context { get; }

		public CoreDataProvider(CoreDataProviderDbContext context, IEventDispatcher eventManager, ILogger<CoreDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}


		#region "    Site methods    "

		public async Task<Site> GetSite(Guid siteId)
		{
			Site result = await this.Context.Sites.Where(site => site.Id == siteId)
				.Include(site => site.AdministratorsRole)
				.Include(site => site.AllUsersRole)
				.Include(site => site.AnonymousUsersRole)
				.Include(site => site.RegisteredUsersRole)
				.Include(site => site.DefaultContainerDefinition)
				.Include(site => site.DefaultLayoutDefinition)
				.Include(site => site.Aliases)
				.Include(site => site.SiteSettings)
				.Include(site => site.UserProfileProperties.OrderBy(prop => prop.SortOrder))
				.AsSplitQuery()
				.FirstOrDefaultAsync();

			// We have to handle default site alias manually, because it is .Ignored in Nucleus.Data.EntityFramework.CoreDbContextBuilderExtensions.  The
			// preceeding Linq query must allow tracking (don't apply .AsNoTracking), because we need to read the DefaultSiteAliasId shadow property, and
			// shadow property values are stored by the change tracker.
			if (result != null)
			{
				result.DefaultSiteAlias = await this.Context.SiteAlias
					.Where(alias => alias.Id == this.Context.Entry(result).Property<Guid?>("DefaultSiteAliasId").CurrentValue)
					.AsNoTracking()
					.FirstOrDefaultAsync();
			}
			return result;
		}

		private async Task SaveSiteSettings(Guid siteId, List<SiteSetting> siteSettings)
		{
			Site current = await this.Context.Sites
				.Where(site => site.Id == siteId)
				.Include(site => site.SiteSettings)
				.FirstOrDefaultAsync();

			if (current != null)
			{
				foreach (SiteSetting setting in siteSettings)
				{
					SiteSetting existing = current.SiteSettings.Where(existingSetting => existingSetting.SettingName == setting.SettingName).FirstOrDefault();
					if (existing != null)
					{
						// Update setting
						this.Context.Entry(existing).CurrentValues.SetValues(setting);
						this.Context.Entry(existing).State = EntityState.Modified;
					}
					else
					{
						// Add setting
						current.SiteSettings.Add(setting);
						this.Context.Entry(setting).Property("SiteId").CurrentValue = siteId;
						this.Context.Entry(setting).State = EntityState.Added;
					}
				}

				await this.Context.SaveChangesAsync<SiteSetting>();
			}
		}

		public async Task DeleteSite(Site site)
		{
			Site current = await this.Context.Sites
				.Where(current => current.Id == site.Id)
				.Include(site => site.Aliases)
				.Include(site => site.SiteSettings)
				.Include(site => site.UserProfileProperties)
				.AsSplitQuery()
				.FirstOrDefaultAsync();

			List<SiteAlias> aliases = await this.Context.SiteAlias.Where(alias => EF.Property<Guid>(alias, "SiteId") == site.Id).ToListAsync();
			List<Role> roles = await this.Context.Roles.Where(role => EF.Property<Guid>(role, "SiteId") == site.Id).ToListAsync();
			List<RoleGroup> roleGroups = await this.Context.RoleGroups.Where(roleGroup => EF.Property<Guid>(roleGroup, "SiteId") == site.Id).ToListAsync();
			List<MailTemplate> mailTemplates = await this.Context.MailTemplates.Where(mailTemplate => EF.Property<Guid>(mailTemplate, "SiteId") == site.Id).ToListAsync();
			List<List> lists = await this.Context.Lists
				.Where(list => EF.Property<Guid>(list, "SiteId") == site.Id)
				.Include(list => list.Items)
				.ToListAsync();

			if (current != null)
			{
				current.AdministratorsRole = null;
				current.AllUsersRole = null;
				current.AnonymousUsersRole = null;
				current.RegisteredUsersRole = null;
				current.DefaultSiteAlias = null;
				await this.Context.SaveChangesAsync<Site>();

				this.Context.RemoveRange(aliases);
				this.Context.RemoveRange(roleGroups);
				this.Context.RemoveRange(roles);
				this.Context.RemoveRange(mailTemplates);
				this.Context.RemoveRange(lists);

				this.Context.Remove(current);
				await this.Context.SaveChangesAsync();
				this.EventManager.RaiseEvent<Site, Delete>(site);
			}
		}

		public async Task<Guid> DetectSite(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase)
		{
			string siteAlias = requestUri.Value + pathBase;
			Site site;

			if (String.IsNullOrEmpty(siteAlias) && await CountSites() == 1)
			{
				site = await this.Context.Sites
					.AsNoTracking()
					.FirstOrDefaultAsync();
			}
			else
			{
				site = await this.Context.Sites
					.Where(site => site.Aliases.Where(alias => alias.Alias == siteAlias).Any())
					.AsNoTracking()
					.FirstOrDefaultAsync();
			}
			return site == null ? Guid.Empty : site.Id;
		}

		public async Task<long> CountSites()
		{
			return await this.Context.Sites.CountAsync();
		}

		/// <summary>
		/// List all sites.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function is used by the control panel user interface to list sites for editing.  The returned site entities are not fully populated.
		/// </remarks>
		public async Task<List<Site>> ListSites()
		{
			return await this.Context.Sites
				.AsNoTracking()
				.ToListAsync();
		}

		/// <summary>
		/// List the user profile properties for a site, specified by <paramref name="siteId"/>.
		/// </summary>
		/// <param name="siteId"></param>
		/// <returns></returns>
		public async Task<List<UserProfileProperty>> ListSiteUserProfileProperties(Guid siteId)
		{
			return await this.Context.UserProfileProperties
				.Where(prop => EF.Property<Guid>(prop, "SiteId") == siteId)
				.OrderBy(prop => prop.SortOrder)
				.AsNoTracking()
				.ToListAsync();
		}

		/// <summary>
		/// Create or update a site.
		/// </summary>
		/// <param name="site"></param>
		public async Task SaveSite(Site site)
		{
			Boolean isNew = !this.Context.Sites.Where(existing => existing.Id == site.Id).Any();
			Action raiseEvent;

			if (isNew)
			{
				Site newSite = this.Context.AttachClone(site);
				this.Context.Entry(newSite).State = EntityState.Added;

				// New site records can not save a default site alias, or any of the roles, because they won't exist yet
				this.Context.Entry(newSite).Reference(site => site.AdministratorsRole).CurrentValue = null;
				this.Context.Entry(newSite).Reference(site => site.AllUsersRole).CurrentValue = null;
				this.Context.Entry(newSite).Reference(site => site.AnonymousUsersRole).CurrentValue = null;
				this.Context.Entry(newSite).Reference(site => site.RegisteredUsersRole).CurrentValue = null;

				await this.Context.SaveChangesAsync<Site>();
				site.Id = newSite.Id;

				raiseEvent = () => this.EventManager.RaiseEvent<Site, Create>(site);
			}
			else
			{
				this.Context.Attach(site);

				// We have to handle default site alias manually, because it is .Ignored in Nucleus.Data.EntityFramework.CoreDbContextBuilderExtensions
				this.Context.Entry(site).Property<Guid?>("DefaultSiteAliasId").CurrentValue = site.DefaultSiteAlias?.Id;

				this.Context.Entry(site).State = EntityState.Modified;
				await this.Context.SaveChangesAsync<Site>();
				raiseEvent = () => this.EventManager.RaiseEvent<Site, Update>(site);
			}

			await SaveSiteSettings(site.Id, site.SiteSettings);

			raiseEvent.Invoke();
		}

		public async Task<SiteAlias> GetSiteAlias(Guid id)
		{
			return await this.Context.SiteAlias
				.Where(alias => alias.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveSiteAlias(Guid siteId, SiteAlias alias)
		{
			Site site = await this.Context.Sites.Where(site => site.Id == siteId)
				.Include(site => site.Aliases)
				.FirstOrDefaultAsync();

			SiteAlias existingAlias = site.Aliases
				.Where(existing => existing.Id == alias.Id)
				.FirstOrDefault();

			if (existingAlias != null)
			{
				// update alias	
				existingAlias.Alias = alias.Alias;
				this.Context.Entry(existingAlias).State = EntityState.Modified;
			}
			else
			{
				// new alias
				site.Aliases.Add(alias);
				this.Context.Entry(alias).State = EntityState.Added;
			}

			await this.Context.SaveChangesAsync<SiteAlias>();
		}

		public async Task DeleteSiteAlias(Guid siteId, Guid id)
		{
			Site site = await this.Context.Sites.Where(site => site.Id == siteId)
				.Include(site => site.Aliases)
				.FirstOrDefaultAsync();

			this.Context.Remove(site.Aliases.Where(existing => existing.Id == id).FirstOrDefault());
			await this.Context.SaveChangesAsync<SiteAlias>();
		}

		#endregion

		#region "    Page methods    "

		public async Task<Page> GetPage(Guid pageId)
		{
			Page result = await this.Context.Pages.Where(page => page.Id == pageId)
				.Include(page => page.LayoutDefinition)
				.Include(page => page.DefaultContainerDefinition)
				.Include(page => page.Modules)
					.OrderBy(module => module.SortOrder)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ContainerDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleSettings)
				.Include(page => page.Routes)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				// Entity framework doesn't correctly order "child" collections so we have to do it client-side
				result.Modules = result.Modules.OrderBy(module => module.SortOrder).ToList();

				foreach (PageModule module in result.Modules)
				{
					module.Permissions = await ListPermissions(module.Id, PageModule.URN);
				}

				result.IsFirst = await GetFirstPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;
				result.IsLast = await GetLastPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;

				// we have to deal with the permissions table "manually" because entity framework can't understand that it serves three
				// different entities (page, module and folder, as well as any other extensions that might use it)
				result.Permissions = await ListPermissions(result.Id, Page.URN);

			}

			return result;
		}

		public async Task<Guid> FindPage(Site site, string path)
		{
			return await this.Context.Pages
				.Where(page => page.SiteId == site.Id && page.Routes.Where(route => route.Path == path).Any())
				.AsNoTracking()
				.Select(page => page.Id)
				.FirstOrDefaultAsync();
		}

		public async Task<List<Page>> ListPages(Guid siteId)
		{
			List<Page> results = await this.Context.Pages.Where(page => page.SiteId == siteId)
				.Include(page => page.LayoutDefinition)
				.Include(page => page.DefaultContainerDefinition)
				.Include(page => page.Modules)
					.OrderBy(module => module.SortOrder)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ContainerDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleSettings)
				.Include(page => page.Routes)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			foreach (Page result in results)
			{
				result.Permissions = await ListPermissions(result.Id, Page.URN);

				result.IsFirst = await GetFirstPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;
				result.IsLast = await GetLastPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;
			}

			return results;
		}

		public async Task<List<Page>> ListPages(Guid siteId, Guid? parentId)
		{
			List<Page> results = await this.Context.Pages.Where(page => page.SiteId == siteId && page.ParentId == parentId)
				.Include(page => page.LayoutDefinition)
				.Include(page => page.DefaultContainerDefinition)
				.Include(page => page.Modules)
					.OrderBy(module => module.SortOrder)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ContainerDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleDefinition)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleSettings)
				.Include(page => page.Routes)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			foreach (Page result in results)
			{
				result.Permissions = await ListPermissions(result.Id, Page.URN);
				result.IsFirst = await GetFirstPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;
				result.IsLast = await GetLastPageSortOrder(result.SiteId, result.ParentId) == result.SortOrder;
			}

			return results;
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Page>> SearchPages(Guid siteId, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<Page> results;

			pagingSettings.TotalCount = await this.Context.Pages.Where(page =>
				page.SiteId == siteId &&
					(
						EF.Functions.Like(page.Name, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Title, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Description, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Keywords, $"%{searchTerm}%")
					)
				)
				.CountAsync();

			results = await this.Context.Pages.Where(page =>
				page.SiteId == siteId &&
					(
						EF.Functions.Like(page.Name, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Title, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Description, $"%{searchTerm}%") ||
						EF.Functions.Like(page.Keywords, $"%{searchTerm}%")
					)
				)
				.Include(page => page.LayoutDefinition)
				.Include(page => page.DefaultContainerDefinition)
				.Include(page => page.Modules)
				.Include(page => page.Routes)
				.OrderBy(page => page.SortOrder)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<Page>(pagingSettings, results);
		}


		public async Task SavePage(Site site, Page page)
		{
			List<Permission> currentPermissions = await this.Context.Permissions
				.Where(permission => permission.RelatedId == page.Id && permission.PermissionType.Scope.StartsWith(Page.URN))
				.AsNoTracking()
				.ToListAsync();

			Guid? currentParentId = await this.Context.Pages
				.Where(existing => existing.Id == page.Id)
				.AsNoTracking()
				.Select(page => page.ParentId)
				.FirstOrDefaultAsync();

			Page existingPage = await this.Context.Pages
				.Where(existing => existing.Id == page.Id)
				.Include(existing => existing.Routes)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (page.ParentId == Guid.Empty)
			{
				page.ParentId = null;
			}

			// new record
			if (existingPage == null && page.SortOrder == 0)
			{
				page.SortOrder = await GetLastPageSortOrder(site.Id, page.ParentId) + 10;
			}
			else
			{
				if (currentParentId != page.ParentId)
				{
					// the user has moved this page to a different parent, reset sort order to prevent collisions
					page.SortOrder = await GetLastPageSortOrder(site.Id, page.ParentId) + 10;
				}
			}

			// if no default route is selected set the first route as default
			if (!page.DefaultPageRouteId.HasValue || page.DefaultPageRouteId == Guid.Empty)
			{
				page.DefaultPageRouteId = page.Routes.FirstOrDefault()?.Id;
			}

			// We need to create a new <Page> CLR object to work with, because EF alters properties when SaveChanges is called, and we want
			// to retain the original values so we can call SavePageRoutes and SavePermissions.
			Page pageClone = this.Context.AttachClone(page);

			this.Context.Entry(pageClone).Property("SiteId").CurrentValue = site.Id;

			this.Context.Entry(pageClone).Property("DefaultContainerDefinitionId").CurrentValue =
				page.DefaultContainerDefinition == null || page.DefaultContainerDefinition.Id == Guid.Empty ? null : page.DefaultContainerDefinition?.Id;

			this.Context.Entry(pageClone).Property("LayoutDefinitionId").CurrentValue =
				page.LayoutDefinition == null || page.LayoutDefinition.Id == Guid.Empty ? null : page.LayoutDefinition?.Id;

			this.Context.Entry(pageClone).State = (existingPage == null) ? EntityState.Added : EntityState.Modified;

			// New pages can't save a default page route, because routes can't be created until the page exists.  So we have to 
			// store the default page route value, save the page, then save the routes, then update the page with the selected default
			// page route (to avoid a database foreign key violation error)
			if (existingPage == null)
			{
				this.Context.Entry(pageClone).Property("DefaultPageRouteId").CurrentValue = null;
				await this.Context.SaveChangesAsync<Page>();
				page.Id = pageClone.Id;
			}
			else
			{
				await this.Context.SaveChangesAsync<Page>();
			}

			// add/update/remove related records
			this.Context.ChangeTracker.Clear();
			await SavePageRoutes(site, page, existingPage?.Routes);

			this.Context.ChangeTracker.Clear();
			await SavePermissions(page.Id, page.Permissions, currentPermissions);

			// update default page route & raise events
			if (existingPage == null)
			{
				// new record
				this.Context.Entry(pageClone).Property("DefaultPageRouteId").CurrentValue = page.DefaultPageRouteId;
				this.Context.Update(pageClone);
				await this.Context.SaveChangesAsync<Page>();

				this.EventManager.RaiseEvent<Page, Create>(page);
			}
			else
			{
				this.EventManager.RaiseEvent<Page, Update>(page);
			}
		}

		private async Task<int> GetFirstPageSortOrder(Guid siteId, Guid? parentId)
		{
			Page topPage = await this.Context.Pages
				.Where(page => page.ParentId == parentId && page.SiteId == siteId)
				.OrderBy(page => page.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return topPage == null ? 0 : topPage.SortOrder;
		}

		private async Task<int> GetLastPageSortOrder(Guid siteId, Guid? parentId)
		{
			Page lastPage = await this.Context.Pages
				.Where(page => page.ParentId == parentId && page.SiteId == siteId)
				.OrderByDescending(page => page.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return lastPage == null ? 0 : lastPage.SortOrder;
		}

		private async Task SavePageRoutes(Site site, Page page, List<PageRoute> originalRoutes)
		{
			if (page.Routes != null)
			{
				// remove deleted page routes
				if (originalRoutes != null)
				{
					foreach (PageRoute originalPageRoute in originalRoutes.Where(route => !page.Routes.Where(pageRoute => pageRoute.Id == route.Id).Any()))
					{
						this.Context.Remove(originalPageRoute);
					}
				}

				// add/update routes
				if (page.Routes.Any() == true)
				{
					foreach (PageRoute newPageRoute in page.Routes.ToList())
					{
						if (!newPageRoute.Path.StartsWith('/'))
						{
							newPageRoute.Path = $"/{newPageRoute.Path}";
						}

						PageRoute existingRoute = await this.Context.Set<PageRoute>()
							.Where(existing => existing.Id == newPageRoute.Id)
							.FirstOrDefaultAsync();

						if (existingRoute != null)
						{
							// route already exists for page
							existingRoute.Path = newPageRoute.Path;
							existingRoute.Type = newPageRoute.Type;
							this.Context.Entry(existingRoute).State = EntityState.Modified;
						}
						else
						{
							// route does not exist for page
							this.Context.Set<PageRoute>().Add(newPageRoute);
							this.Context.Entry(newPageRoute).Property("PageId").CurrentValue = page.Id;
							this.Context.Entry(newPageRoute).Property("SiteId").CurrentValue = site.Id;
							this.Context.Entry(newPageRoute).State = EntityState.Added;
						}
					}
				}

				await this.Context.SaveChangesAsync<PageRoute>();
			}
		}

		/// <summary>
		/// Delete a page record
		/// </summary>
		/// <param name="page"></param>
		public async Task DeletePage(Page page)
		{
			Page result = await this.Context.Pages.Where(existing => existing.Id == page.Id)
				.Include(page => page.Modules)
					.ThenInclude(pageModule => pageModule.ModuleSettings)
				.Include(page => page.Routes)
				.AsSplitQuery()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				if (result.Routes != null)
				{
					// we must set DefaultPageRouteId to null so that we don't trigger FK_Pages_DefaultPageRouteId when we delete PageRoutes
					result.DefaultPageRouteId = null;
					await this.Context.SaveChangesAsync<Page>();

					foreach (PageRoute pageRoute in result.Routes)
					{
						this.Context.Remove(pageRoute);
					}
				}

				this.Context.Remove(result);
				await this.Context.SaveChangesAsync();
			}
		}
		#endregion

		#region "    Module methods    "
		public async Task<PageModule> GetPageModule(Guid moduleId)
		{
			PageModule result = await this.Context.PageModules
				.Where(module => module.Id == moduleId)
				.Include(module => module.ModuleDefinition)
				.Include(module => module.ContainerDefinition)
				.Include(module => module.ModuleSettings)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				result.Permissions = await ListPermissions(result.Id, PageModule.URN);
			}

			return result;
		}


		//public async Task<List<ModuleSetting>> ListPageModuleSettings(Guid moduleId)
		//{
		//	return await this.Context.Set<ModuleSetting>()
		//		.Where(setting => EF.Property<Guid>(setting, "ModuleId") == moduleId)
		//		.AsNoTracking()
		//		.ToListAsync();
		//}

		public async Task<List<PageModule>> ListPageModules(Guid pageId)
		{
			List<PageModule> results = await this.Context.PageModules
				.Where(module => module.PageId == pageId)
				.Include(module => module.ModuleDefinition)
				.Include(module => module.ContainerDefinition)
				.Include(module => module.ModuleSettings)
				.OrderBy(module => module.SortOrder)
				.AsNoTracking()
				.ToListAsync();

			foreach (PageModule result in results)
			{
				result.Permissions = await ListPermissions(result.Id, PageModule.URN);
			}

			return results;
		}

		public async Task<List<PageModule>> ListPageModules(ModuleDefinition moduleDefinition)
		{
			List<PageModule> results = await this.Context.PageModules
				.Where(module => module.ModuleDefinition.Id == moduleDefinition.Id)
				.Include(module => module.ModuleDefinition)
				.Include(module => module.ContainerDefinition)
				.Include(module => module.ModuleSettings)
				.AsNoTracking()
				.ToListAsync();

			foreach (PageModule result in results)
			{
				result.Permissions = await ListPermissions(result.Id, PageModule.URN);
			}

			return results;
		}

		public async Task DeletePageModule(PageModule module)
		{
			this.Context.Remove(module);
			await this.Context.SaveChangesAsync<PageModule>();

			this.EventManager.RaiseEvent<PageModule, Delete>(module);
		}

		public async Task SavePageModule(Guid pageId, PageModule module)
		{
			Boolean isNew = !this.Context.PageModules.Where(existing => existing.Id == module.Id).Any();

			if (isNew && module.SortOrder == 0)
			{
				module.SortOrder = await GetLastPageModuleSortOrder(pageId) + 10;
			}

			this.Context.Attach(module);
			this.Context.Entry(module).Property("PageId").CurrentValue = pageId;
			this.Context.Entry(module).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync<PageModule>();

			this.Context.ChangeTracker.Clear();
			await SavePageModuleSettings(module.Id, module.ModuleSettings);

			if (isNew)
			{
				this.EventManager.RaiseEvent<PageModule, Create>(module);
			}
			else
			{
				this.EventManager.RaiseEvent<PageModule, Update>(module);
			}

		}

		private async Task<int> GetLastPageModuleSortOrder(Guid pageId)
		{
			PageModule module = await this.Context.PageModules
				.Where(module => module.PageId == pageId)
				.OrderByDescending(module => module.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return module == null ? 0 : module.SortOrder;
		}

		#endregion

		#region "    Module Settings    "
		public async Task SavePageModuleSettings(Guid moduleId, List<ModuleSetting> moduleSettings)
		{
			PageModule module = await this.Context.PageModules
				.Where(existing => existing.Id == moduleId)
				.Include(module => module.ModuleSettings)
				.FirstOrDefaultAsync();

			foreach (ModuleSetting setting in moduleSettings)
			{
				ModuleSetting existingSetting = module.ModuleSettings.Where(existing => existing.SettingName == setting.SettingName).FirstOrDefault();

				if (existingSetting != null)
				{
					existingSetting.SettingValue = setting.SettingValue;
					this.Context.Entry(existingSetting).State = EntityState.Modified;
				}
				else
				{
					this.Context.Add(setting);
					this.Context.Entry(setting).State = EntityState.Added;
					this.Context.Entry(setting).Property("PageModuleId").CurrentValue = moduleId;
				}
			}

			await this.Context.SaveChangesAsync<ModuleSetting>();
			this.EventManager.RaiseEvent<PageModule, Update>(module);
		}


		#endregion

		#region "    Module Definitions    "
		public async Task<List<ModuleDefinition>> ListModuleDefinitions()
		{
			return await this.Context.ModuleDefinitions
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<ModuleDefinition> GetModuleDefinition(Guid moduleDefinitionId)
		{
			return await this.Context.ModuleDefinitions
				.Where(moduleDefinition => moduleDefinition.Id == moduleDefinitionId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveModuleDefinition(ModuleDefinition moduleDefinition)
		{
			Boolean isNew = !this.Context.ModuleDefinitions.Where(existing => existing.Id == moduleDefinition.Id).AsNoTracking().Any();
			moduleDefinition.ClassTypeName = "";  // temporary use, this property will be removed
			this.Context.Attach(moduleDefinition);
			this.Context.Entry(moduleDefinition).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<ModuleDefinition, Create>(moduleDefinition);
			}
			else
			{
				this.EventManager.RaiseEvent<ModuleDefinition, Update>(moduleDefinition);
			}
		}

		/// <summary>
		/// Delete the specified module definition, and all module instances.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public async Task DeleteModuleDefinition(ModuleDefinition moduleDefinition)
		{
      ModuleDefinition existing = await this.Context.ModuleDefinitions
        .Where(existing => existing.Id == moduleDefinition.Id)
        .FirstOrDefaultAsync();

      if (existing != null)
      {
        this.Context.ModuleDefinitions.Remove(existing);
        await this.Context.SaveChangesAsync<ModuleDefinition>();
      }
		}

		#endregion

		#region "    Users    "

		public async Task<long> CountSystemAdministrators()
		{
			return await this.Context.Users
				.Where(user => user.IsSystemAdministrator == true && user.SiteId == null)
				.AsNoTracking()
				.CountAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListSystemAdministrators(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<User> results;

			var query = this.Context.Users
				.Where(user => user.IsSystemAdministrator == true && user.SiteId == null);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(user => user.UserName)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<User>(pagingSettings, results);
		}

		public async Task SaveSystemAdministrator(User user)
		{
			Boolean isNew = !this.Context.Users
        .Where(existing => existing.Id == user.Id)
        .Any();

      if (isNew)
      {
        // extra check to make sure that we are not creating a system administrator with the same user name as a
        // site user.
        User existing = this.Context.Users
          .Where(existing => existing.UserName == user.UserName)
          .AsNoTracking()
          .FirstOrDefault();

        if (existing != null)
        {
          throw new InvalidOperationException($"A site user named '{user.UserName}' already exists, so you can't create a system administrator with this user name.");
        }
      }

      user.SiteId = null;
			user.IsSystemAdministrator = true;

			this.Context.Attach(user);
			this.Context.Entry(user).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<User, Create>(user);
			}
			else
			{
				this.EventManager.RaiseEvent<User, Update>(user);
			}
		}

		public async Task<IList<User>> ListUsers(Site site)
		{
			return await this.Context.Users
				.Where(user => user.IsSystemAdministrator == false && user.SiteId == site.Id)
				.Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
					.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.OrderBy(user => user.UserName)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsers(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<User> results;
			var query = this.Context.Users
				.Where(user => user.IsSystemAdministrator == false && user.SiteId == site.Id);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
        .Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
          .ThenInclude(profilevalue => profilevalue.UserProfileProperty)
        .OrderBy(user => user.UserName)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
        .AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<User>(pagingSettings, results);
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> SearchUsers(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<User> results;

			pagingSettings.TotalCount = await this.Context.Users.Where(user =>
				user.SiteId == site.Id &&
					(
						EF.Functions.Like(user.UserName, $"%{searchTerm}%")
					)
				)
				.CountAsync();

			results = await this.Context.Users.Where(user =>
				user.SiteId == site.Id &&
					(
						EF.Functions.Like(user.UserName, $"%{searchTerm}%")
					)
				)
        .Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
					.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.OrderBy(user => user.UserName)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
        .AsSplitQuery()
        .AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<User>(pagingSettings, results);
		}


		public async Task<User> GetUserByName(Site site, string userName)
		{
			return await this.Context.Users
				.Where(user => user.SiteId == site.Id && user.UserName == userName)
				.Include(user => user.Secrets)
				.Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
					.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsNoTracking()
				.AsSplitQuery()
				.FirstOrDefaultAsync();

		}

		public async Task<User> GetUserByEmail(Site site, string email)
		{
			List<User> results = await this.Context.Users
				.Where(user => user.SiteId == site.Id && user.Profile.Where(value => value.UserProfileProperty.TypeUri == ClaimTypes.Email && value.Value == email).Any())
				.Include(user => user.Secrets)
				.Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
					.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			if (results.Count == 0)
			{
				return default;
			}
			else if (results.Count > 1)
			{
				throw new InvalidOperationException("The supplied email address matches more than one account.");
			}
			else
			{
				return results.First();
			}
		}

		public async Task<User> GetSystemAdministrator(string userName)
		{
			return await this.Context.Users
				.Where(user => user.UserName == userName && user.SiteId == null && user.IsSystemAdministrator == true)
				.Include(user => user.Secrets)
				// System admins don't have roles
				.Include(user => user.Profile)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<User> GetUser(Guid userId)
		{
			return await this.Context.Users
				.Where(user => user.Id == userId)
				.Include(user => user.Secrets)
				.Include(user => user.Roles)
          .ThenInclude(role => role.RoleGroup)
        .Include(user => user.Profile)
					.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsNoTracking()
				.AsSplitQuery()
				.FirstOrDefaultAsync();
		}

		public async Task SaveUser(Site site, User user)
		{
			user.SiteId = site.Id;

			User existing = await this.Context.Users
				.Where(existing => existing.Id == user.Id)
				.Include(existing => existing.Profile)
					.ThenInclude(profileValue => profileValue.UserProfileProperty)
				.Include(existing => existing.Secrets)
				.AsNoTracking()
				.AsSplitQuery()
				.FirstOrDefaultAsync();

      if (existing == null)
      {
        // extra check to make sure that we are not creating a user with the same user name as an existing 
        // system administrator.
        User existingSystemAdmin = this.Context.Users
          .Where(existing => existing.UserName == user.UserName && existing.IsSystemAdministrator)
          .AsNoTracking()
          .FirstOrDefault();

        if (existingSystemAdmin != null)
        {
          throw new InvalidOperationException($"A user named '{user.UserName}' already exists, so you can't create a new user with this user name.");
        }
      }

      // We must remove roles from the user object before calling .Attach) so that when we add newly-assigned roles later, we don't get
      // a change-tracking error telling us that the role is already being tracked
      List<Role> userRoles = user.Roles;
			user.Roles = new();

			this.Context.Attach(user);
			this.Context.Entry(user).State = (existing == null) ? EntityState.Added : EntityState.Modified;

			foreach (UserProfileValue value in user.Profile)
			{
				this.Context.Entry(value).State = (existing == null || (!existing.Profile.Where(existingValue => existingValue.UserProfileProperty.Id == value.UserProfileProperty.Id).Any())) ? EntityState.Added : EntityState.Modified;
			}

			if (user.Secrets != null)
			{
				this.Context.Entry(user.Secrets).State = (existing == null || existing.Secrets == null) ? EntityState.Added : EntityState.Modified;
			}

			await this.Context.SaveChangesAsync();

			this.Context.ChangeTracker.Clear();
			if (userRoles != null)
			{
				// Make sure that the role.Users property for roles assigned to the user is null so that entity-framework doesn't start tracking
				// them & generate an error 
				foreach(Role role in userRoles)
				{
					role.Users = null;
				}
				user.Roles = userRoles;
				await SaveUserRoles(user);
			}

			if (existing == null)
			{
				this.EventManager.RaiseEvent<User, Create>(user);
			}
			else
			{
				this.EventManager.RaiseEvent<User, Update>(user);
			}

		}

		private async Task SaveUserRoles(User user)
		{
			User existingUser = await this.Context.Users
				.Where(existing => existing.Id == user.Id)
				.Include(existing => existing.Roles)
				.FirstOrDefaultAsync();
			
			// don't update roles if user.roles is null (our data provider convention is to interpret a null collection property as a signal
			// to not do anything to the data represented by that collection)
			if (existingUser != null && user.Roles != null)
			{
				// delete removed role assignments
				foreach (Role existingRole in existingUser.Roles.ToList())
				{
					if (!user.Roles.Where(userRole => userRole.Id == existingRole.Id).Any())
					{
						// delete role assignment
						this.Context.Entry(existingUser).Collection(userModel => userModel.Roles).FindEntry(existingRole).State = EntityState.Deleted;
						// we don't want to change the actual role data, just the user-role assignment
						this.Context.Entry(existingRole).State = EntityState.Unchanged;
					}
				}

				// add new role assignments				
				foreach (Role role in user.Roles)
				{
					if (!existingUser.Roles.Where(existingRole => existingRole.Id == role.Id).Any())
					{
            // add new role assignment  

            // we need to copy the role & remove the group to prevent unwanted updates to role groups
            Role newRole = role.Copy<Role>();
            newRole.RoleGroup = null;

            existingUser.Roles.Add(newRole);
						this.Context.Entry(existingUser).Collection(userModel => userModel.Roles).FindEntry(newRole).State = EntityState.Added;
						// set audit field values on the shadow entity
						this.Context.Entry(existingUser).Collection(userModel => userModel.Roles).FindEntry(newRole).Property<Guid?>(nameof(ModelBase.AddedBy)).CurrentValue = this.Context.CurrentUserId();
						this.Context.Entry(existingUser).Collection(userModel => userModel.Roles).FindEntry(newRole).Property<DateTime?>(nameof(ModelBase.DateAdded)).CurrentValue = DateTime.UtcNow;           // we don't want to change the actual role data, just the user-role assignment
						
						// we don't want to change the actual role data, just the user-role assignment
						this.Context.Entry(newRole).State = EntityState.Unchanged;
					}
					// existing role assignments do not need to be updated
				}
				
				await this.Context.SaveChangesAsync();
			}
		}

		public async Task DeleteUser(User user)
		{
			this.Context.Users.Remove(user);
			await this.Context.SaveChangesAsync();
		}

		public async Task SaveUserSecrets(User user)
		{
			Boolean isNew = !this.Context.UserSecrets
				.Where(existing => EF.Property<Guid>(existing, "UserId") == user.Id)
				.AsNoTracking()
				.Any();

			this.Context.UserSecrets.Attach(user.Secrets);
			this.Context.Entry(user.Secrets).Property("UserId").CurrentValue = user.Id;
			this.Context.Entry(user.Secrets).State = isNew ? EntityState.Added : EntityState.Modified;

			await this.Context.SaveChangesAsync<UserSecrets>();
		}

		#endregion

		#region "    Role Groups    "

		public async Task<IEnumerable<RoleGroup>> ListRoleGroups(Site site)
		{
			return await this.Context.RoleGroups
				.Where(rolegroup => EF.Property<Guid>(rolegroup, "SiteId") == site.Id)
				.OrderBy(rolegroup => rolegroup.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<RoleGroup>> ListRoleGroups(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<RoleGroup> results;

			var query = this.Context.RoleGroups
				.Where(rolegroup => EF.Property<Guid>(rolegroup, "SiteId") == site.Id);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(rolegroup => rolegroup.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<RoleGroup>(pagingSettings, results);
		}

		public async Task<RoleGroup> GetRoleGroup(Guid roleGroupId)
		{
			return await this.Context.RoleGroups
				.Where(rolegroup => rolegroup.Id == roleGroupId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

    public async Task<RoleGroup> GetRoleGroupByName(Site site, string name)
    {
      return await this.Context.RoleGroups
        .Where(rolegroup => EF.Property<Guid>(rolegroup, "SiteId") == site.Id && rolegroup.Name == name)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task SaveRoleGroup(Site site, RoleGroup roleGroup)
		{
			Action raiseEvent;

			Boolean isNew = !this.Context.RoleGroups.Where(rolegroup => rolegroup.Id == roleGroup.Id).Any();

			this.Context.Attach(roleGroup);
			this.Context.Entry(roleGroup).Property("SiteId").CurrentValue = site.Id;

			if (isNew)
			{
				// new role group
				this.Context.Entry(roleGroup).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<RoleGroup, Create>(roleGroup); });
			}
			else
			{
				// existing role group
				this.Context.Entry(roleGroup).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<RoleGroup, Update>(roleGroup); });
			}

			await this.Context.SaveChangesAsync<RoleGroup>();

			raiseEvent.Invoke();
		}

		public async Task DeleteRoleGroup(RoleGroup roleGroup)
		{
			this.Context.RoleGroups.Remove(roleGroup);
			await this.Context.SaveChangesAsync<RoleGroup>();

			this.EventManager.RaiseEvent<RoleGroup, Delete>(roleGroup);
		}

		#endregion

		#region "    Roles    "

		public async Task<IEnumerable<Role>> ListRoles(Site site)
		{
			return await this.Context.Roles
				.Where(role => EF.Property<Guid>(role, "SiteId") == site.Id)
				.Include(role => role.RoleGroup)
				.OrderBy(role => role.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Role>> ListRoles(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<Role> results;

			var query = this.Context.Roles
				.Where(role => EF.Property<Guid>(role, "SiteId") == site.Id);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.Include(role => role.RoleGroup)
				.OrderBy(role => role.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<Role>(pagingSettings, results);

		}

		public async Task<List<Role>> ListRoleGroupRoles(Guid roleGroupId)
		{
			return await this.Context.Roles
				.Where(existing => existing.RoleGroup.Id == roleGroupId)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Role> GetRole(Guid roleId)
		{
			return await this.Context.Roles
				.Where(existing => existing.Id == roleId)
				.Include(role => role.RoleGroup)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<Role> GetRoleByName(string name)
		{
			return await this.Context.Roles
				.Where(existing => existing.Name == name)
				.Include(role => role.RoleGroup)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveRole(Site site, Role role)
		{
			Action raiseEvent;

			Role existing = await this.Context.Roles
				.Where(existing => existing.Id == role.Id)
				.FirstOrDefaultAsync();

			if (existing == null)
			{
				// New role
				this.Context.Roles.Add(role);
				this.Context.Entry(role).State = EntityState.Added;
				this.Context.Entry(role).Property("SiteId").CurrentValue = site.Id;

				raiseEvent = new(() => { this.EventManager.RaiseEvent<Role, Create>(role); });
			}
			else
			{
				// existing role
				this.Context.Entry(existing).CurrentValues.SetValues(role);
				this.Context.Entry(existing).State = EntityState.Modified;
				this.Context.Entry(existing).Property<Guid?>("RoleGroupId").CurrentValue = role.RoleGroup?.Id;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Role, Update>(existing); });
			}

			await this.Context.SaveChangesAsync<Role>();
			raiseEvent.Invoke();
		}

		public async Task DeleteRole(Role role)
		{
			using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await this.Context.Database.BeginTransactionAsync();

			try
			{
				await this.Context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM UserRoles WHERE RoleId={role.Id}");
				await this.Context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Permissions WHERE RoleId={role.Id}");

				this.Context.Remove(role);
				await this.Context.SaveChangesAsync();

				transaction.Commit();
			}
			catch (Exception)
			{
				transaction.Rollback();
				throw;
			}
		}

		#endregion

		#region "    Permissions    "

		public async Task<IList<User>> ListUsersInRole(Guid roleId)
		{
			return await this.Context.Users
				.Where(user => user.Roles.Where(role => role.Id == roleId).Any())
				.Include(user => user.Roles)
				.Include(user => user.Profile)
					.ThenInclude(value => value.UserProfileProperty)
				.OrderBy(user => user.UserName)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsersInRole(Guid roleId, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<User> results;

			pagingSettings.TotalCount = await this.Context.Users
				.Where(user => user.Roles.Where(role => role.Id == roleId).Any())
				.AsNoTracking()
				.CountAsync();

			results = await this.Context.Users
				.Where(user => user.Roles.Where(role => role.Id == roleId).Any())
				.AsNoTracking()
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.Include(user => user.Profile)
					.ThenInclude(value => value.UserProfileProperty)
				.OrderBy(user => user.UserName)
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<User>(pagingSettings, results);
		}

		public async Task<Guid> AddPermissionType(PermissionType permissionType)
		{
			if (!this.Context.PermissionTypes.Where(existing => existing.Id == permissionType.Id).Any())
			{
				this.Context.PermissionTypes.Add(permissionType);
				await this.Context.SaveChangesAsync();
			}
			return permissionType.Id;
		}

		public async Task<List<PermissionType>> ListPermissionTypes(string scopeNamespace)
		{
			// if you get a weird error in debug mode saying this can't be parsed, restart the application in Visual Studio.  The error
			// is caused by a bug in entity framework with visual studio edit & continue.
			return await this.Context.PermissionTypes
				.Where(typ => EF.Functions.Like(typ.Scope, $"%{scopeNamespace}%"))
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<Permission>> ListPermissions(Guid Id, string permissionNameSpace)
		{
			return await this.Context.Permissions
				.Where(permission => permission.RelatedId == Id && permission.PermissionType.Scope.StartsWith(permissionNameSpace))
				.Include(permission => permission.PermissionType)
				.Include(permission => permission.Role)
					.ThenInclude(role => role.RoleGroup)
				.AsNoTracking()
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task DeletePermissions(IEnumerable<Permission> permissions)
		{
			// We have to loop through the permissions to delete rather than using .RemoveRange because we must
			// detach the related entities (role, rolegroup, permission type) as we go, as entity framework 
			// throws an exception saying that they are already being tracked otherwise. 
			foreach (Permission oldPermission in permissions)
			{
				this.Context.Remove(oldPermission);

				if (oldPermission.Role != null)
				{
					this.Context.Entry(oldPermission.Role).State = EntityState.Detached;
					if (oldPermission.Role.RoleGroup != null)
					{
						this.Context.Entry(oldPermission.Role.RoleGroup).State = EntityState.Detached;
					}
				}
				if (oldPermission.PermissionType != null)
				{
					this.Context.Entry(oldPermission.PermissionType).State = EntityState.Detached;
				}

				await this.Context.SaveChangesAsync<Permission>();
			}
		}

		public async Task SavePermissions(Guid relatedId, IEnumerable<Permission> newPermissions, IList<Permission> existingPermissions)
		{
			// delete removed permissions
			foreach (Permission oldPermission in existingPermissions)
			{
				if (!newPermissions.Where(permission => permission.Id == oldPermission.Id).Any())
				{
					this.Context.Remove(oldPermission);

					if (oldPermission.Role != null)
					{
						this.Context.Entry(oldPermission.Role).State = EntityState.Detached;
						if (oldPermission.Role.RoleGroup != null)
						{
							this.Context.Entry(oldPermission.Role.RoleGroup).State = EntityState.Detached;
						}
					}
					if (oldPermission.PermissionType != null)
					{
						this.Context.Entry(oldPermission.PermissionType).State = EntityState.Detached;
					}
				}

				await this.Context.SaveChangesAsync<Permission>();
			}

			// add/update permissions
			foreach (Permission newPermission in newPermissions.Where(permission => permission.PermissionType.Scope != PermissionType.PermissionScopeNamespaces.Disabled))
			{
				Permission existing = existingPermissions.Where(existing => existing.Id == newPermission.Id).FirstOrDefault();

				if (existing == null)
				{
					newPermission.RelatedId = relatedId;

					this.Context.Set<Permission>().Attach(newPermission);
					this.Context.Entry(newPermission).State = EntityState.Added;

					this.Context.Entry(newPermission.Role).State = EntityState.Detached;
					if (newPermission.Role.RoleGroup != null)
					{
						this.Context.Entry(newPermission.Role.RoleGroup).State = EntityState.Detached;
					}
					this.Context.Entry(newPermission.PermissionType).State = EntityState.Detached;
				}
				else
				{
					existing.AllowAccess = newPermission.AllowAccess;
					this.Context.Set<Permission>().Attach(existing);
					this.Context.Entry(existing).State = EntityState.Modified;

					if (existing.Role != null)
					{
						this.Context.Entry(existing.Role).State = EntityState.Detached;
						if (existing.Role.RoleGroup != null)
						{
							this.Context.Entry(existing.Role.RoleGroup).State = EntityState.Detached;
						}
					}
					if (existing.PermissionType != null)
					{
						this.Context.Entry(existing.PermissionType).State = EntityState.Detached;
					}
				}

				await this.Context.SaveChangesAsync<Permission>();
			}
		}

		#endregion

		#region "    Session    "

		public async Task<UserSession> GetUserSession(Guid userSessionId)
		{
			return await this.Context.UserSessions
				.Where(session => session.Id == userSessionId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task DeleteUserSession(UserSession userSession)
		{
			this.Context.UserSessions.Remove(userSession);

			try
			{
				await this.Context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				// DeleteExpiredSessions can also delete sessions, and runs in a scheduled task (so can happen any time).  Ignore 
				// DbUpdateConcurrencyException error thrown if the record is already deleted.
			}
		}

		public async Task DeleteExpiredSessions()
		{
			List<UserSession> sessions = await this.Context.UserSessions.Where(session => session.ExpiryDate < DateTime.UtcNow).ToListAsync();
			this.Context.RemoveRange(sessions);

			try
			{
				await this.Context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				// DeleteUserSession can also delete sessions.  DeleteExpiredSessions runs in a scheduled task (so can happen any time).  Ignore 
				// DbUpdateConcurrencyException error thrown if the record is already deleted.
			}

		}

		public async Task<long> CountUsersOnline(Site site)
		{
			return await this.Context.UserSessions
				.Where(session => session.SiteId == site.Id && session.ExpiryDate > DateTime.UtcNow && session.LastUpdated > DateTime.UtcNow.AddMinutes(-5))
				.CountAsync();
		}

		public async Task SaveUserSession(UserSession userSession)
		{
			this.Context.Attach(userSession);

			if (!this.Context.UserSessions.Where(existing => existing.Id == userSession.Id).Any())
			{
				this.Context.Entry(userSession).State = EntityState.Added;
			}
			else
			{
				this.Context.Entry(userSession).State = EntityState.Modified;
			}

			await this.Context.SaveChangesAsync<UserSession>();
		}

		#endregion

		#region "    Site User Profile Properties    "

		public async Task<UserProfileProperty> GetUserProfileProperty(Guid id)
		{
			return await this.Context.UserProfileProperties
				.Where(prop => prop.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}


		public async Task SaveUserProfileProperty(Guid siteId, UserProfileProperty property)
		{
			Boolean isNew = !this.Context.UserProfileProperties.Where(existing => existing.Id == property.Id).Any();

			this.Context.Attach(property);
			this.Context.Entry(property).Property("SiteId").CurrentValue = siteId;

			if (isNew)
			{
				// new property
				property.SortOrder = await GetLastProfilePropertySortOrder(siteId) + 10;
				this.Context.Entry(property).State = EntityState.Added;
			}
			else
			{
				// Update property
				this.Context.Entry(property).State = EntityState.Modified;
			}

			await this.Context.SaveChangesAsync<UserProfileProperty>();
		}

		private async Task<int> GetLastProfilePropertySortOrder(Guid siteId)
		{
			UserProfileProperty property = await this.Context.UserProfileProperties
				.Where(prop => EF.Property<Guid>(prop, "SiteId") == siteId)
				.OrderByDescending(prop => prop.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return property == null ? 0 : property.SortOrder;
		}

		public async Task DeleteUserProfileProperty(UserProfileProperty property)
		{
			this.Context.Remove(property);
			await this.Context.SaveChangesAsync();
		}

		#endregion

		#region "    Mail Templates    "
		public async Task<IEnumerable<MailTemplate>> ListMailTemplates(Site site)
		{
			return await this.Context.MailTemplates
				.Where(template => EF.Property<Guid>(template, "SiteId") == site.Id)
				.OrderBy(template => template.Name)
				.AsNoTracking()
				.ToListAsync();
		}


		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate>> ListMailTemplates(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<MailTemplate> results;

			var query = this.Context.MailTemplates
				.Where(template => EF.Property<Guid>(template, "SiteId") == site.Id);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(template => template.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate>(pagingSettings, results);
		}

		public async Task<MailTemplate> GetMailTemplate(Guid templateId)
		{
			return await this.Context.MailTemplates
				.Where(template => template.Id == templateId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveMailTemplate(Site site, MailTemplate mailTemplate)
		{
			Action raiseEvent;

			this.Context.Attach(mailTemplate);
			this.Context.Entry(mailTemplate).Property("SiteId").CurrentValue = site.Id;

			if (!this.Context.MailTemplates.Where(template => template.Id == mailTemplate.Id).Any())
			{
				// new record 
				this.Context.Entry(mailTemplate).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<MailTemplate, Create>(mailTemplate); });
			}
			else
			{
				// update existing
				this.Context.Entry(mailTemplate).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<MailTemplate, Update>(mailTemplate); });
			}

			await this.Context.SaveChangesAsync<MailTemplate>();
			raiseEvent.Invoke();
		}

		public async Task DeleteMailTemplate(MailTemplate mailTemplate)
		{
			this.Context.MailTemplates.Remove(mailTemplate);
			await this.Context.SaveChangesAsync();

			this.EventManager.RaiseEvent<MailTemplate, Delete>(mailTemplate);
		}

		#endregion

		#region "    Layout Definitions    "
		public async Task<List<LayoutDefinition>> ListLayoutDefinitions()
		{
			return await this.Context.LayoutDefinitions
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<LayoutDefinition> GetLayoutDefinition(Guid layoutDefinitionId)
		{
			return await this.Context.LayoutDefinitions
				.Where(existing => existing.Id == layoutDefinitionId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			Boolean isNew = !this.Context.LayoutDefinitions.Where(existing => existing.Id == layoutDefinition.Id).Any();

			this.Context.Attach(layoutDefinition);
			this.Context.Entry(layoutDefinition).State = isNew ? EntityState.Added : EntityState.Modified;

			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<LayoutDefinition, Create>(layoutDefinition);
			}
			else
			{
				this.EventManager.RaiseEvent<LayoutDefinition, Update>(layoutDefinition);
			}
		}

		public async Task DeleteLayoutDefinition(LayoutDefinition layoutDefinition)
		{
      LayoutDefinition existing = await this.Context.LayoutDefinitions
        .Where(existing=>existing.Id == layoutDefinition.Id)
        .FirstOrDefaultAsync();

      if (existing != null)
      {
        this.Context.LayoutDefinitions.Remove(existing);
        await this.Context.SaveChangesAsync();
      }
		}

		#endregion

		#region "    Container Definitions    "
		public async Task<List<ContainerDefinition>> ListContainerDefinitions()
		{
			return await this.Context.ContainerDefinitions
				.AsNoTracking()
				.OrderBy(container => container.FriendlyName)
				.ToListAsync();
		}

		public async Task<ContainerDefinition> GetContainerDefinition(Guid containerDefinitionId)
		{
			return await this.Context.ContainerDefinitions
				.Where(existing => existing.Id == containerDefinitionId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveContainerDefinition(ContainerDefinition containerDefinition)
		{
			Boolean isNew = !this.Context.ContainerDefinitions.Where(existing => existing.Id == containerDefinition.Id).Any();

			this.Context.Attach(containerDefinition);
			this.Context.Entry(containerDefinition).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<ContainerDefinition, Create>(containerDefinition);
			}
			else
			{
				this.EventManager.RaiseEvent<ContainerDefinition, Update>(containerDefinition);
			}
		}

		public async Task DeleteContainerDefinition(ContainerDefinition containerDefinition)
		{
      ContainerDefinition existing = await this.Context.ContainerDefinitions
        .Where(existing => existing.Id == containerDefinition.Id)
        .FirstOrDefaultAsync();

      if (existing != null)
      {
        this.Context.ContainerDefinitions.Remove(existing);
        await this.Context.SaveChangesAsync();
      }
		}

		#endregion

		#region "    ControlPanelExtension Definitions    "
		public async Task<List<ControlPanelExtensionDefinition>> ListControlPanelExtensionDefinitions()
		{
			return await this.Context.ControlPanelExtensionDefinitions
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<ControlPanelExtensionDefinition> GetControlPanelExtensionDefinition(Guid controlPanelExtensionDefinitionId)
		{
			return await this.Context.ControlPanelExtensionDefinitions
				.Where(existing => existing.Id == controlPanelExtensionDefinitionId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition)
		{
			Boolean isNew = !this.Context.ControlPanelExtensionDefinitions
				.Where(existing => existing.Id == controlPanelExtensionDefinition.Id)
				.AsNoTracking()
				.Any();

			this.Context.Attach(controlPanelExtensionDefinition);
			this.Context.Entry(controlPanelExtensionDefinition).State = isNew ? EntityState.Added : EntityState.Modified;

			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<ControlPanelExtensionDefinition, Create>(controlPanelExtensionDefinition);
			}
			else
			{
				this.EventManager.RaiseEvent<ControlPanelExtensionDefinition, Update>(controlPanelExtensionDefinition);
			}
		}

		public async Task DeleteControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition)
		{
      ControlPanelExtensionDefinition existing = await this.Context.ControlPanelExtensionDefinitions
        .Where(existing => existing.Id == controlPanelExtensionDefinition.Id)
        .FirstOrDefaultAsync();

      if (existing != null)
      {
        this.Context.ControlPanelExtensionDefinitions.Remove(existing);
        await this.Context.SaveChangesAsync();
      }

		}

		#endregion

		#region "    ScheduledTasks    "

		public async Task<List<ScheduledTask>> ListScheduledTasks()
		{
			return await this.Context.ScheduledTasks
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<ScheduledTask>> ListScheduledTasks(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<ScheduledTask> results;

			var query = this.Context.ScheduledTasks;

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(scheduledTask => scheduledTask.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<ScheduledTask>(pagingSettings, results);
		}


		public async Task<ScheduledTask> GetScheduledTask(Guid scheduledTaskId)
		{
			return await this.Context.ScheduledTasks
				.Where(task => task.Id == scheduledTaskId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		//public async Task ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime)
		//{
		//	ScheduledTask task = await this.Context.ScheduledTasks
		//		.Where(task => task.Id == scheduledTask.Id)
		//		.FirstOrDefaultAsync();

		//	task.NextScheduledRun = nextRunDateTime;
		//	await this.Context.SaveChangesAsync<ScheduledTask>();
		//	this.EventManager.RaiseEvent<ScheduledTask, Update>(scheduledTask);
		//}

		public async Task<ScheduledTaskHistory> GetMostRecentHistory(ScheduledTask scheduledTask, string server)
		{
			var query = this.Context.ScheduledTaskHistory
				.Where(history => history.ScheduledTaskId == scheduledTask.Id);

			if (!String.IsNullOrEmpty(server))
			{
				query = query.Where(history => history.Server == server);
			}

			return await query
				.OrderByDescending(history => history.FinishDate)
				.FirstOrDefaultAsync();
		}

		public async Task<ScheduledTask> GetScheduledTaskByTypeName(string typeName)
		{
			return await this.Context.ScheduledTasks
				.Where(task => task.TypeName.Equals(typeName))
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveScheduledTask(ScheduledTask scheduledTask)
		{
			Boolean isNew = !this.Context.ScheduledTasks.Where(existing => existing.Id == scheduledTask.Id).Any();

			this.Context.Attach(scheduledTask);
			this.Context.Entry(scheduledTask).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<ScheduledTask, Create>(scheduledTask);
			}
			else
			{
				this.EventManager.RaiseEvent<ScheduledTask, Update>(scheduledTask);
			}
		}

		public async Task DeleteScheduledTask(ScheduledTask scheduledTask)
		{
			this.Context.ScheduledTasks.Remove(scheduledTask);
			await this.Context.SaveChangesAsync();
		}

		public async Task<List<ScheduledTaskHistory>> ListScheduledTaskHistory(Guid scheduledTaskId)
		{
			return await this.Context.ScheduledTaskHistory
				.Where(item => item.ScheduledTaskId == scheduledTaskId)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task SaveScheduledTaskHistory(ScheduledTaskHistory history)
		{
			Boolean isNew = !this.Context.ScheduledTaskHistory
				.Where(existing => existing.Id == history.Id)
				.AsNoTracking()
				.Any();

			this.Context.ScheduledTaskHistory.Attach(history);
			this.Context.Entry(history).State = isNew ? EntityState.Added : EntityState.Modified;

			await this.Context.SaveChangesAsync<ScheduledTaskHistory>();
		}

		public async Task DeleteScheduledTaskHistory(ScheduledTaskHistory history)
		{
			this.Context.ScheduledTaskHistory.Remove(history);
			await this.Context.SaveChangesAsync<ScheduledTaskHistory>();
		}

		#endregion

		#region "    IFileSystemDataProvider    "

		public async Task<Folder> GetFolder(Guid folderId)
		{
			return await this.Context.Folders
				.Where(folder => folder.Id == folderId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<Folder> GetFolder(Site site, string provider, string path)
		{
			if (path == null)
			{
				path = string.Empty;
			}

			return await this.Context.Folders
				.Where(folder => EF.Property<Guid>(folder, "SiteId") == site.Id && folder.Provider == provider && folder.Path == path)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<Folder> SaveFolder(Site site, Folder folder)
		{
			Boolean isNew = !this.Context.Folders.Where(existing => existing.Id == folder.Id).Any();

			this.Context.Attach(folder);
			this.Context.Entry(folder).Property("SiteId").CurrentValue = site.Id;
			this.Context.Entry(folder).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<Folder, Create>(folder);
			}
			else
			{
				this.EventManager.RaiseEvent<Folder, Update>(folder);
			}

			return folder;
		}


		public async Task DeleteFolder(Folder folder)
		{
			this.Context.Folders.Remove(folder);
			await this.Context.SaveChangesAsync<Folder>();
		}


		public async Task<File> GetFile(Guid fileId)
		{
			return await this.Context.Files
				.Where(file => file.Id == fileId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<File> GetFile(Site site, string provider, string path)
		{
			return await this.Context.Files
				.Where(file => EF.Property<Guid>(file, "SiteId") == site.Id && file.Provider == provider && file.Path == path)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<File> SaveFile(Site site, File file)
		{
			Boolean exists = await this.Context.Files.Where(existing => existing.Id == file.Id)
				.AsNoTracking()
				.AnyAsync();

			this.Context.Attach(file);
			this.Context.Entry(file).Property("SiteId").CurrentValue = site.Id;
			this.Context.Entry(file).State = exists ? EntityState.Modified : EntityState.Added;

			await this.Context.SaveChangesAsync();

			if (exists)
			{
				this.EventManager.RaiseEvent<File, Update>(file);
			}
			else
			{
				this.EventManager.RaiseEvent<File, Create>(file);
			}

			return file;
		}

		public async Task DeleteFile(File file)
		{
			this.Context.Files.Remove(file);
			await this.Context.SaveChangesAsync<File>();
		}

		#endregion

		#region "    Site Groups    "

		public async Task<IEnumerable<SiteGroup>> ListSiteGroups()
		{
			return await this.Context.SiteGroups
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<SiteGroup>> ListSiteGroups(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<SiteGroup> results;

			var query = this.Context.SiteGroups;

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(group => group.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<SiteGroup>(pagingSettings, results);
		}

		public async Task<SiteGroup> GetSiteGroup(Guid siteGroupId)
		{
			return await this.Context.SiteGroups
				.Where(group => group.Id == siteGroupId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveSiteGroup(SiteGroup siteGroup)
		{
			Boolean isNew = !this.Context.SiteGroups.Where(group => group.Id == siteGroup.Id).Any();

			this.Context.Attach(siteGroup);
			this.Context.Entry(siteGroup).State = isNew ? EntityState.Added : EntityState.Modified;

			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<SiteGroup, Create>(siteGroup);
			}
			else
			{
				this.EventManager.RaiseEvent<SiteGroup, Update>(siteGroup);
			}
		}

		public async Task DeleteSiteGroup(SiteGroup siteGroup)
		{
			this.Context.Remove(siteGroup);
			await this.Context.SaveChangesAsync<SiteGroup>();
		}


		#endregion

		#region "    Lists    "
		public async Task<IEnumerable<List>> ListLists(Site site)
		{
			return await this.Context.Lists
				.Where(list => EF.Property<Guid>(list, "SiteId") == site.Id)
				.Include(list => list.Items)
				.OrderBy(list => list.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<List>> ListLists(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<List> results;

			var query = this.Context.Lists
				.Where(list => EF.Property<Guid>(list, "SiteId") == site.Id);

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.Include(list => list.Items)
				.OrderBy(list => list.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<List>(pagingSettings, results);
		}

		public async Task<List> GetList(Guid listId)
		{
			return await this.Context.Lists
				.Where(list => list.Id == listId)
				.Include(list => list.Items.OrderBy(item => item.Value))
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveList(Site site, List list)
		{
			List existing = await this.Context.Lists
				.Where(existing => existing.Id == list.Id)
				.Include(existing => existing.Items)
				.FirstOrDefaultAsync();

			Action raiseEvent;

			if (existing == null)
			{
				// new list
				this.Context.Attach(list);
				this.Context.Entry(list).Property("SiteId").CurrentValue = site.Id;
				this.Context.Entry(list).State = EntityState.Added;
				raiseEvent = new Action(() => { this.EventManager.RaiseEvent<List, Create>(list); });

			}
			else
			{
				this.Context.Entry(existing).CurrentValues.SetValues(list);
				this.Context.Entry(existing).Property("SiteId").CurrentValue = site.Id;
				this.Context.Entry(existing).State = EntityState.Modified;
				raiseEvent = new Action(() => { this.EventManager.RaiseEvent<List, Update>(list); });
			}

			if (list.Items != null)
			{
				// delete removed list items
				if (existing != null)
				{
					foreach (ListItem existingItem in existing.Items.ToList())
					{
						if (!list.Items.Where(item => item.Id == existingItem.Id).Any())
						{
							this.Context.Remove(existingItem);
						}
					}
				}

				// Add new list items and update existing list items
				foreach (ListItem item in list.Items)
				{
					ListItem existingItem = existing?.Items.Where(existingItem => existingItem.Id == item.Id).FirstOrDefault();

					if (existingItem != null)
					{
						// Update list item
						this.Context.Entry(existingItem).CurrentValues.SetValues(item);
						this.Context.Entry(existingItem).State = EntityState.Modified;

					}
					else
					{
            // Add setting
            if (existing != null)
            {
              existing.Items.Add(item);
              this.Context.Entry(item).State = EntityState.Added;
            }
            else
            {
              // list will already contain the item
            }
					}
				}
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task DeleteList(List list)
		{
			// Prevent entity framework from trying to delete the list items.  A database cascade-delete referential action removes list items when a list is
			// deleted.
			list.Items.Clear();

			this.Context.Remove(list);
			await this.Context.SaveChangesAsync<List>();
		}

		public async Task<ListItem> GetListItem(Guid listItemId)
		{
			return await this.Context.ListItems
				.Where(item => item.Id == listItemId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task DeleteListItem(ListItem listItem)
		{
			this.Context.Remove(listItem);
			await this.Context.SaveChangesAsync<ListItem>();
		}

		#endregion

		#region "    Content    "


		public async Task<List<Content>> ListContent(PageModule pageModule)
		{
			return await this.Context.Contents
				.Where(content => content.PageModuleId == pageModule.Id)
				.OrderBy(content => content.SortOrder)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Content> GetContent(Guid id)
		{
			return await this.Context.Contents
				.Where(content => content.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveContent(PageModule module, Content content)
		{
			content.PageModuleId = module.Id;

			Boolean isNew = !this.Context.Contents.Where(existing => existing.Id == content.Id).Any();

			if (isNew)
			{
				content.SortOrder = await GetLastContentSortOrder(module.Id) + 10;
			}

			this.Context.Attach(content);
			this.Context.Entry(content).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();
		}

		private async Task<int> GetLastContentSortOrder(Guid moduleId)
		{
			Content content = await this.Context.Contents
				.Where(content => content.PageModuleId == moduleId)
				.OrderByDescending(content => content.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return content == null ? 0 : content.SortOrder;
		}

		public async Task DeleteContent(Content content)
		{
			this.Context.Remove(content);
			await this.Context.SaveChangesAsync<Content>();
		}

		#endregion

		#region "    API Keys    "


		public async Task<IEnumerable<ApiKey>> ListApiKeys()
		{
			return await this.Context.ApiKeys
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<PagedResult<ApiKey>> ListApiKeys(PagingSettings pagingSettings)
		{
			List<ApiKey> results;

			var query = this.Context.ApiKeys;

			pagingSettings.TotalCount = await query
				.CountAsync();

			results = await query
				.OrderBy(ApiKey => ApiKey.Name)
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey>(pagingSettings, results);
		}

		public async Task<ApiKey> GetApiKey(Guid id)
		{
			return await this.Context.ApiKeys
				.Where(content => content.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveApiKey(ApiKey apiKey)
		{
			Boolean isNew = !this.Context.ApiKeys.Where(existing => existing.Id == apiKey.Id).Any();

			this.Context.Attach(apiKey);
			this.Context.Entry(apiKey).State = isNew ? EntityState.Added : EntityState.Modified;
			await this.Context.SaveChangesAsync();

			if (isNew)
			{
				this.EventManager.RaiseEvent<ApiKey, Create>(apiKey);
			}
			else
			{
				this.EventManager.RaiseEvent<ApiKey, Update>(apiKey);
			}
		}

		public async Task DeleteApiKey(ApiKey apiKey)
		{
			this.Context.ApiKeys.Remove(apiKey);
			await this.Context.SaveChangesAsync();
		}

    #endregion

    #region "    Organizations    "


    public async Task<IList<Organization>> ListOrganizations(Site site)
    {
      return await this.Context.Organizations.Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id)        
        .AsNoTracking()
        .OrderBy(organization => organization.Name)
        .ToListAsync();
    }

    public async Task<PagedResult<Organization>> ListOrganizations(Site site, PagingSettings pagingSettings)
    {
      List<Organization> results;

      pagingSettings.TotalCount = await this.Context.Organizations
        .Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id)
        .AsNoTracking()
        .CountAsync();

      results = await this.Context.Organizations
        .Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id)
        .AsNoTracking()
        .Skip(pagingSettings.FirstRowIndex)
        .Take(pagingSettings.PageSize)
        .OrderBy(organization => organization.Name)
        .ToListAsync();

      return new Nucleus.Abstractions.Models.Paging.PagedResult<Organization>(pagingSettings, results);
    }

    public async Task<PagedResult<Organization>> SearchOrganizations(Site site, string searchTerm, PagingSettings pagingSettings)
    {
      List<Organization> results;

      pagingSettings.TotalCount = await this.Context.Organizations
        .Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id && EF.Functions.Like(organization.Name, $"%{searchTerm}%"))
        .AsNoTracking()
        .CountAsync();

      results = await this.Context.Organizations
        .Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id && EF.Functions.Like(organization.Name, $"%{searchTerm}%"))
        .AsNoTracking()
        .Skip(pagingSettings.FirstRowIndex)
        .Take(pagingSettings.PageSize)
        .OrderBy(organization => organization.Name)
        .ToListAsync();

      return new Nucleus.Abstractions.Models.Paging.PagedResult<Organization>(pagingSettings, results);
    }

    public async Task<Organization> GetOrganization(Guid organizationId)
    {
      return await this.Context.Organizations.Where(organization => organization.Id == organizationId)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task<Organization> GetOrganizationByName(Site site, string name)
    {
      return await this.Context.Organizations.Where(organization => EF.Property<Guid>(organization, "SiteId") == site.Id && organization.Name == name)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task<Boolean> IsOrganizationMember(Organization organization, Guid userId)
    {
      return await this.Context.OrganizationUsers.Where(organizationUser => organizationUser.Organization.Id == organization.Id && organizationUser.User.Id == userId)
       .AsNoTracking()
       .AnyAsync();
    }

    public async Task<Boolean> IsOrganizationAdministrator(Organization organization, Guid userId)
    {
      return await this.Context.OrganizationUsers.Where(organizationUser => organizationUser.Organization.Id == organization.Id && organizationUser.User.Id == userId && organizationUser.UserType == OrganizationUser.UserTypes.Administrator)
       .AsNoTracking()
       .AnyAsync();
    }

    public async Task SaveOrganization(Site site, Organization organization)
    {
      Boolean isNew = !this.Context.Organizations.Where(existing => existing.Id == organization.Id).Any();
      organization.EncodedName= organization.Name.FriendlyEncode();
      
      this.Context.Attach(organization);
      this.Context.Entry(organization).Property("SiteId").CurrentValue = site.Id;
      this.Context.Entry(organization).State = isNew ? EntityState.Added : EntityState.Modified;
      await this.Context.SaveChangesAsync();

      if (isNew)
      {
        this.EventManager.RaiseEvent<Organization, Create>(organization);
      }
      else
      {
        this.EventManager.RaiseEvent<Organization, Update>(organization);
      }
    }

    public async Task DeleteOrganization(Organization organization)
    {
      this.Context.Organizations.Remove(organization);
      await this.Context.SaveChangesAsync();

      this.EventManager.RaiseEvent<Organization, Delete>(organization);
    }

    public async Task AddOrganizationUser(OrganizationUser organizationUser)
    {
      Boolean isNew = this.Context.OrganizationUsers
        .Where(existing => existing.Organization.Id == organizationUser.Organization.Id && existing.User.Id == organizationUser.User.Id)
        .AsNoTracking()
        .Any();

      if (isNew)
      {
        this.Context.OrganizationUsers.Add(organizationUser);
      }
      else
      {
        this.Context.OrganizationUsers.Update(organizationUser);
      }

      if (isNew)
      {
        this.EventManager.RaiseEvent<OrganizationUser, Create>(organizationUser);
      }
      else
      {
        this.EventManager.RaiseEvent<OrganizationUser, Update>(organizationUser);
      }

      await this.Context.SaveChangesAsync();
    }

    public async Task RemoveOrganizationUser(OrganizationUser organizationUser)
    {
      this.Context.OrganizationUsers.Add(organizationUser);
      await this.Context.SaveChangesAsync();
      this.EventManager.RaiseEvent<OrganizationUser, Delete>(organizationUser);
    }

    #endregion

    #region "    Instance Settings    "

    public async Task SaveExtensionsStoreSettings(ExtensionsStoreSettings setting)
    {
      Action raiseEvent;

      ExtensionsStoreSettings existing = await this.Context.ExtensionsStoreSettings
        .Where(existing => existing.StoreUri == setting.StoreUri)
        .FirstOrDefaultAsync();

      if (existing == null)
      {
        // New role
        this.Context.ExtensionsStoreSettings.Add(setting);
        this.Context.Entry(setting).State = EntityState.Added;

        raiseEvent = new(() => { this.EventManager.RaiseEvent<ExtensionsStoreSettings, Create>(setting); });
      }
      else
      {
        // existing role
        this.Context.Entry(existing).CurrentValues.SetValues(setting);
        this.Context.Entry(existing).State = EntityState.Modified;
        raiseEvent = new(() => { this.EventManager.RaiseEvent<ExtensionsStoreSettings, Update>(existing); });
      }

      await this.Context.SaveChangesAsync<ExtensionsStoreSettings>();
      raiseEvent.Invoke();
    }

    public async Task<ExtensionsStoreSettings> GetExtensionsStoreSettings(string storeUri)
    {
      return await this.Context.ExtensionsStoreSettings
        .Where(existing => existing.StoreUri == storeUri)
        .AsNoTracking()
        .FirstOrDefaultAsync();

    }
    #endregion
  }
}

