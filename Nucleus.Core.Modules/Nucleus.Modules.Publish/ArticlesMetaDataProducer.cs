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
using System.Net.Http;

namespace Nucleus.Modules.Publish
{
	public class ArticlesMetaDataProducer : IContentMetaDataProducer
	{
		private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider  { get; } = new();

		private IFileSystemManager FileSystemManager { get; }
		private ArticlesManager ArticlesManager { get; }
		private IExtensionManager ExtensionManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IApiKeyManager ApiKeyManager { get; }
		private HttpClient HttpClient { get; }

		private ILogger<ArticlesMetaDataProducer> Logger { get; }

		public ArticlesMetaDataProducer(HttpClient httpClient, ISiteManager siteManager, ArticlesManager articlesManager, IApiKeyManager apiKeyManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IExtensionManager extensionManager, ILogger<ArticlesMetaDataProducer> logger)
		{
			this.HttpClient = httpClient;
			this.FileSystemManager = fileSystemManager;
			this.ArticlesManager = articlesManager;
			this.ApiKeyManager = apiKeyManager;
			this.ExtensionManager = extensionManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.Logger = logger;
		}

		public async override IAsyncEnumerable<ContentMetaData> ListItems(Site site)
		{
			ApiKey apiKey = null;
			if (site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.APIKEY_ID, out Guid result))
			{
				apiKey = await this.ApiKeyManager.Get(result);
			}

			site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);

			// This must match the value in package.xml
			Guid moduleDefinitionId = Guid.Parse("20af00b8-1d72-4c94-bce7-b175e0b173af");
			
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
						Logger?.LogInformation("Skipping publish module on page {pageid}/{pagename} because the page's 'Include in search' setting is false.", page.Id, page.Name);
					}
					foreach (Models.Article article in await this.ArticlesManager.List(module))
					{
						yield return await BuildContentMetaData(site, module, apiKey, useSsl, article);
					}
				}
			}
		}
				
		/// <summary>
		/// Return a meta-data entry for the article meta-data
		/// </summary>
		/// <param name="site"></param>
		/// <param name="article"></param>
		/// <returns></returns>
		private async Task<ContentMetaData> BuildContentMetaData(Site site, PageModule module, ApiKey apiKey, Boolean useSsl, Models.Article article)
		{		
			// Get article meta-data
			Page page = await this.PageManager.Get(module.PageId);
			
			if (page != null && article.Enabled)
			{
				string pageUrl = UrlHelperExtensions.RelativePageLink(page);

				ContentMetaData articleContentItem = new()
				{
					Site = site,
					Title = (!String.IsNullOrEmpty(module.Title) ? module.Title : !String.IsNullOrEmpty(page.Title) ? page.Title : page.Name) + (!String.IsNullOrEmpty(article.Title) ? " - " + article.Title : ""),
					Url =  pageUrl + article.Title.FriendlyEncode(),
					PublishedDate = article.PublishDate,
					SourceId = article.Id,
					Scope = Models.Article.URN,
					Attachments = article.Attachments.Select(attachment=>attachment.File),
					Keywords = article.Categories.Select(category=>category.CategoryListItem.Name),
					Summary = article.Summary,
					Roles = await GetViewRoles(module),
					ContentType = "text/html"
				};

				await GetContent(site, apiKey, articleContentItem, useSsl);
								
				return articleContentItem;
			}

			return null;
		}

		private async Task GetContent(Site site, ApiKey apiKey, ContentMetaData contentItem, Boolean useSsl)
		{
			System.IO.MemoryStream htmlContent = new();
			Uri uri = new(new Uri((useSsl ? "https" : "http") + Uri.SchemeDelimiter + site.DefaultSiteAlias.Alias), contentItem.Url);

			System.Net.Http.HttpRequestMessage request = new(HttpMethod.Get, uri);

			if (apiKey != null)
			{
				request.Headers.Host = site.DefaultSiteAlias.Alias;
				request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Nucleus-Search-Feeder", this.GetType().Assembly.GetName().Version.ToString()));
				request.Sign(apiKey.Id, apiKey.Secret);
			}

			// Signal Nucleus to only render module content
			request.Headers.Add("X-Nucleus-OverrideLayout", "ContentOnly");

			System.Net.Http.HttpResponseMessage response = this.HttpClient.Send(request);

			if (!response.IsSuccessStatusCode)
			{
				// response was an error, use page meta-data only
			}
			else
			{
				using (System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync())
				{
					// Kestrel doesn't return a content-length, so we have to read into a memory stream first in order to determine the 
					// size of the content array.
					await responseStream.CopyToAsync(htmlContent);
					responseStream.Close();
				}

				contentItem.ContentType = "text/html";

				contentItem.Size = htmlContent.Length;

				htmlContent.Position = 0;
				contentItem.Content = htmlContent.ToArray();
				htmlContent.Close();
			}
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
