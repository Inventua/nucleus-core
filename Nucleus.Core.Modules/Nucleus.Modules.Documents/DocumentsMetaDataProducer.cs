using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Documents
{
	public class DocumentsMetaDataProducer : IContentMetaDataProducer
	{
		private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider  { get; } = new();

		private IFileSystemManager FileSystemManager { get; }
		private DocumentsManager DocumentsManager { get; }
		private IExtensionManager ExtensionManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }

		private ILogger<DocumentsMetaDataProducer> Logger { get; }

		public DocumentsMetaDataProducer(ISiteManager siteManager, DocumentsManager documentsManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IExtensionManager extensionManager, ILogger<DocumentsMetaDataProducer> logger)
		{
			this.FileSystemManager = fileSystemManager;
			this.DocumentsManager = documentsManager;
			this.ExtensionManager = extensionManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.Logger = logger;
		}

		public override async Task<IEnumerable<ContentMetaData>> ListItems(Site site)
		{
			// This must match the value in package.xml
			Guid moduleDefinitionId = Guid.Parse("28df7ff3-6407-459e-8608-c1ef4181807c");
			
			List<ContentMetaData> results = new();

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {0} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
				{
					Page page = await this.PageManager.Get(module.PageId);

					if (!page.IncludeInSearch )
					{
						Logger?.LogInformation("Skipping documents module on page {pageid}/{pagename} because the page's 'Include in search' setting is false.", page.Id, page.Name);
					}
					foreach (Models.Document document in await this.DocumentsManager.List(site, module))
					{
						results.Add(await BuildContentMetaData(site, module, document));
					}
				}
			}

			return results.Where(result=>result != null);
		}

		
		/// <summary>
		/// Return a meta-data entry for the document meta-data
		/// </summary>
		/// <param name="site"></param>
		/// <param name="document"></param>
		/// <returns></returns>
		private async Task<ContentMetaData> BuildContentMetaData(Site site, PageModule module, Models.Document document)
		{

			// Get document meta-data, using the linked file as "content"
			//Guid pageId = this.PageModuleManager.GetPageId(module);
			Page page = await this.PageManager.Get(module.PageId);
			
			if (page != null && document.File != null)
			{
				string pageUrl = UrlHelperExtensions.RelativePageLink(page);

				ContentMetaData documentContentItem = new()
				{
					Site = site,
					Title = (!String.IsNullOrEmpty(module.Title) ? module.Title : !String.IsNullOrEmpty(page.Title) ? page.Title : page.Name) + (!String.IsNullOrEmpty(document.Title) ? " - " + document.Title : ""),
					Url =  pageUrl,
					PublishedDate = document.File.DateChanged.HasValue ? document.File.DateChanged : document.File.DateAdded,
					SourceId = document.Id,
					Scope = Models.Document.URN,
					Roles = await GetViewRoles(module),
					ContentType = "text/html"
				};

				// include the linked file as content
				using (System.IO.Stream responseStream = this.FileSystemManager.GetFileContents(site, document.File))
				{
					documentContentItem.Content = new byte[responseStream.Length];
					responseStream.Read(documentContentItem.Content, 0, documentContentItem.Content.Length);
					responseStream.Close();
				}

				return documentContentItem;
			}

			return null;
		}


		private async Task<List<Role>> GetViewRoles(Folder folder)
		{
			return
				(await this.FileSystemManager.ListPermissions(folder))
					.Where(permission => permission.AllowAccess && permission.IsFolderViewPermission())
					.Select(permission => permission.Role).ToList();
		}

		private async Task<List<Role>> GetViewRoles(PageModule module)
		{
			return
				(await this.PageModuleManager.ListPermissions(module))
					.Where(permission => permission.AllowAccess && permission.IsModuleViewPermission())
					.Select(permission => permission.Role).ToList();
		}

	}
}
