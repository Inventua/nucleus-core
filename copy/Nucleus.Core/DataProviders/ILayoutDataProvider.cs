using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for layout classes <see cref="Site"/>, <see cref="SiteAlias"/>, <see cref="UserProfileProperty"/>,
	/// <see cref="Page"/>, <see cref="PageModule"/>, <see cref="ModuleDefinition"/>, <see cref="LayoutDefinition"/>, <see cref="ContainerDefinition"/>.
	/// </summary>
	internal interface ILayoutDataProvider : IDisposable, Abstractions.IDataProvider
	{
		// site methods
		abstract Site GetSite(Guid id);
		abstract Guid DetectSite(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase);
		abstract void SaveSite(Site site);
		abstract List<Site> ListSites();
		abstract List<SiteAlias> ListSiteAliases(Guid siteId);
		abstract SiteAlias GetSiteAlias(Guid id);		
		abstract void SaveSiteAlias(Guid siteId, SiteAlias alias);
		abstract void DeleteSiteAlias(Guid id);

		abstract UserProfileProperty GetUserProfileProperty(Guid id);
		abstract void SaveUserProfileProperty(Guid siteId, UserProfileProperty property);
		abstract void DeleteUserProfileProperty(Guid id);
		abstract IEnumerable<UserProfileProperty> ListSiteUserProfileProperties(Guid siteId);

		abstract void DeleteSite(Site site);


		// page methods
		abstract List<Page> ListPages(Guid siteId);
		abstract List<Page> ListPages(Guid siteId, Guid parentId);

		abstract List<Page> SearchPages(Guid siteId, string searchTerm);

		abstract Page GetPage(Guid pageId);
		abstract Guid GetPageSiteId(Page page);
		abstract Guid GetPageModulePageId(PageModule pageModule);
		abstract Guid FindPage(Site site, string path);
		abstract void SavePage(Site site, Page page);
		abstract void DeletePage(Page page);

		// module methods
		abstract List<PageModule> ListPageModules(Guid pageId);
		abstract PageModule GetPageModule(Guid moduleId);
		abstract void SavePageModule(Guid pageId, PageModule module);
		abstract void SavePageModuleSettings(Guid moduleId, Dictionary<string, string> moduleSettings);
		abstract void DeletePageModule(PageModule module);

		// module definition methods
		abstract List<ModuleDefinition> ListModuleDefinitions();
		abstract void SaveModuleDefinition(ModuleDefinition moduleDefinition);
		abstract ModuleDefinition GetModuleDefinition(Guid id);
		abstract void DeleteModuleDefinition(ModuleDefinition moduleDefinition);

		// layout definition methods
		abstract List<LayoutDefinition> ListLayoutDefinitions();
		abstract void SaveLayoutDefinition(LayoutDefinition layoutDefinition);
		abstract LayoutDefinition GetLayoutDefinition(Guid id);
		abstract void DeleteLayoutDefinition(LayoutDefinition layoutDefinition);

		// container definition methods
		abstract List<ContainerDefinition> ListContainerDefinitions();
		abstract void SaveContainerDefinition(ContainerDefinition containerDefinition);
		abstract ContainerDefinition GetContainerDefinition(Guid id);
		abstract void DeleteContainerDefinition(ContainerDefinition containerDefinition);

		// site groups
		abstract List<SiteGroup> ListSiteGroups();
		abstract SiteGroup GetSiteGroup(Guid Id);
		abstract void SaveSiteGroup(SiteGroup SiteGroup);
		abstract void DeleteSiteGroup(SiteGroup SiteGroup);
	}
}
