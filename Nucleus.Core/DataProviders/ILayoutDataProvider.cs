using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for layout classes <see cref="Site"/>, <see cref="SiteAlias"/>, <see cref="UserProfileProperty"/>,
	/// <see cref="Page"/>, <see cref="PageModule"/>, <see cref="ModuleDefinition"/>, <see cref="LayoutDefinition"/>, <see cref="ContainerDefinition"/>.
	/// </summary>
	internal interface ILayoutDataProvider : IDisposable//, IDataProvider<ILayoutDataProvider>
	{
		// site methods
		abstract Task<Site> GetSite(Guid id);
		abstract Task<Guid> DetectSite(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase);
		abstract Task SaveSite(Site site);
		abstract Task<List<Site>> ListSites();
		abstract Task<long> CountSites();
		
		abstract Task<SiteAlias> GetSiteAlias(Guid id);		
		abstract Task SaveSiteAlias(Guid siteId, SiteAlias alias);
		abstract Task DeleteSiteAlias(Guid siteId, Guid id);

		abstract Task<UserProfileProperty> GetUserProfileProperty(Guid id);
		abstract Task SaveUserProfileProperty(Guid siteId, UserProfileProperty property);
		abstract Task DeleteUserProfileProperty(UserProfileProperty property);
		abstract Task<List<UserProfileProperty>> ListSiteUserProfileProperties(Guid siteId);

		abstract Task DeleteSite(Site site);


		// page methods
		abstract Task<List<Page>> ListPages(Guid siteId);
    abstract Task<List<Page>> ListSitePages(Guid siteId);

    abstract Task<List<Page>> ListPages(Guid siteId, Guid? parentId);

    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<Page>> SearchPages(Site site, string searchTerm, IEnumerable<Role> userRoles, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		abstract Task<Page> GetPage(Guid pageId);

		abstract Task<Guid> FindPage(Site site, string path);
		abstract Task SavePage(Site site, Page page);
		abstract Task DeletePage(Page page);

		// module methods
		abstract Task<List<PageModule>> ListPageModules(Guid pageId);
		abstract Task<PageModule> GetPageModule(Guid moduleId);
		abstract Task SavePageModule(Guid pageId, PageModule module);
		abstract Task SavePageModuleSettings(Guid moduleId, List<ModuleSetting> moduleSettings);
		abstract Task DeletePageModule(PageModule module);
		abstract Task<List<PageModule>> ListPageModules(ModuleDefinition moduleDefinition);


		// module definition methods
		abstract Task<List<ModuleDefinition>> ListModuleDefinitions();
		abstract Task SaveModuleDefinition(ModuleDefinition moduleDefinition);
		abstract Task<ModuleDefinition> GetModuleDefinition(Guid id);
		abstract Task DeleteModuleDefinition(ModuleDefinition moduleDefinition);

		// layout definition methods
		abstract Task<List<LayoutDefinition>> ListLayoutDefinitions();
		abstract Task SaveLayoutDefinition(LayoutDefinition layoutDefinition);
		abstract Task<LayoutDefinition> GetLayoutDefinition(Guid id);
		abstract Task DeleteLayoutDefinition(LayoutDefinition layoutDefinition);

		// container definition methods
		abstract Task<List<ContainerDefinition>> ListContainerDefinitions();
		abstract Task SaveContainerDefinition(ContainerDefinition containerDefinition);
		abstract Task<ContainerDefinition> GetContainerDefinition(Guid id);
		abstract Task DeleteContainerDefinition(ContainerDefinition containerDefinition);

		// control panel extension definition methods
		abstract Task<List<ControlPanelExtensionDefinition>> ListControlPanelExtensionDefinitions();
		abstract Task SaveControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition);
		abstract Task<ControlPanelExtensionDefinition> GetControlPanelExtensionDefinition(Guid id);
		abstract Task DeleteControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition);

		// site groups
		abstract Task<IEnumerable<SiteGroup>> ListSiteGroups(); 
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<SiteGroup>> ListSiteGroups(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task<SiteGroup> GetSiteGroup(Guid Id);
		abstract Task SaveSiteGroup(SiteGroup SiteGroup);
		abstract Task DeleteSiteGroup(SiteGroup SiteGroup);
	}
}
