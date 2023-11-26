using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.Layout;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Net.Http;

namespace Nucleus.Core.Search
{
	public class PageMetaDataProducer : IContentMetaDataProducer
	{
    private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }
    private IPageManager PageManager { get; }
		private IApiKeyManager ApiKeyManager { get; }
		private ILogger<PageMetaDataProducer> Logger { get; }
		private HttpClient HttpClient { get; }

		public PageMetaDataProducer(HttpClient httpClient, ISearchIndexHistoryManager searchIndexHistoryManager, IPageManager pageManager, IApiKeyManager apiKeyManager, ILogger<PageMetaDataProducer> logger)
		{
      this.SearchIndexHistoryManager = searchIndexHistoryManager;
      this.PageManager = pageManager;
			this.ApiKeyManager = apiKeyManager;
			this.HttpClient = httpClient;
			this.Logger = logger;
		}

		public async override IAsyncEnumerable<ContentMetaData> ListItems(Site site)
		{
			//List<ContentMetaData> results = new();
			ApiKey apiKey = null;
			Boolean indexPublicPagesOnly = false;

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {siteId} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				if (site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.APIKEY_ID, out Guid result))
				{
					apiKey = await this.ApiKeyManager.Get(result);
				}

				site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_PAGES_ONLY, out indexPublicPagesOnly);

				foreach (Page page in (await this.PageManager.List(site)).Where(page => !page.Disabled))
				{
					if (!page.IncludeInSearch)
					{
						Logger.LogInformation("Skipping page {pageId} [{pageName}] because its 'Include in search' setting is false.", page.Id, page.Name);
					}
					else
					{
						if (!indexPublicPagesOnly || page.Permissions.Where(permission => permission.IsPageViewPermission() && permission.AllowAccess).Any())
						{
              SearchIndexHistory historyItem = await this.SearchIndexHistoryManager.Get(site.Id, Page.URN, page.Id);
              if (historyItem == null || historyItem.LastIndexedDate < PageModifiedDate(page))
              {
                Logger.LogTrace("Building meta-data for page {pageId} [{pageName}]", page.Id, page.Name);
                // we have to .Get the site and page because the .List methods don't return fully-populated page objects
                ContentMetaData metaData = await BuildContentMetaData(site, apiKey, await this.PageManager.Get(page.Id));

                if (metaData != null)
                {
                  yield return metaData;
                }
              }
              else
              {
                Logger.LogTrace("Skipped page {pageId} [{pageName}] because it has not been changed since the last time it was indexed.", page.Id, page.Name);
              }
            }
						else
						{
							Logger.LogInformation("Skipped page {pageId} [{pageName}] because it is not visible to all users, and 'Index Public Pages Only' is set.", page.Id, page.Name);
						}
					}
				}
			}
		}


    /// <summary>
    /// Return the page updated date or the latest page module updated date, which 
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    private DateTime PageModifiedDate(Page page)
    {
      DateTime? modifiedDate = page.DateChanged ?? page.DateAdded;

      PageModule lastChangedModule = page.Modules.OrderBy(module => module.DateChanged ?? module.DateAdded).FirstOrDefault();
      
      if (modifiedDate == null || (lastChangedModule != null && modifiedDate < (lastChangedModule.DateChanged ?? lastChangedModule.DateAdded)))
      {
        modifiedDate = lastChangedModule.DateChanged ?? lastChangedModule.DateAdded;
      }

      return modifiedDate ?? DateTime.MaxValue;
    }

		private async Task<ContentMetaData> BuildContentMetaData(Site site, ApiKey apiKey, Page page)
		{
      System.IO.MemoryStream htmlContent = new();
			string pageRelativeUrl = PageLink(page);

			site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);

			if (!String.IsNullOrEmpty(pageRelativeUrl))
			{
				Uri pageUri = new(new Uri((useSsl ? "https": "http") + Uri.SchemeDelimiter + site.DefaultSiteAlias.Alias), pageRelativeUrl);

				ContentMetaData contentItem = new()
				{
					Site = site,
					Title = !string.IsNullOrEmpty(page.Title) ? page.Title : page.Name,
					Summary = page.Description,
					Url = pageRelativeUrl,
					PublishedDate = page.DateChanged.HasValue ? page.DateChanged : page.DateAdded,
					SourceId = page.Id,
					Scope = Page.URN,
          Type = "Page",
					Keywords = page.Keywords?.Split(',').ToList(),
					Roles = await GetViewRoles(page)
				};

				System.Net.Http.HttpRequestMessage request = new(HttpMethod.Get, pageUri);
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

				return contentItem;
			}
			else
			{
				Logger.LogWarning("Could not build page meta-data because the page has no routes.");
				return null;
			}
		}

		private async Task<List<Role>> GetViewRoles(Page page)
		{
			return
				(await this.PageManager.ListPermissions(page))
					.Where(permission => permission.AllowAccess && permission.IsPageViewPermission())
					.Select(permission => permission.Role).ToList();
		}

		private static string PageLink(Page page)
		{
			if (page == null || page.Disabled || page.DefaultPageRoute() == null) return "";
			string path = page.DefaultPageRoute().Path;

			// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
			return path + (path.EndsWith("/") ? "" : "/");
		}
	}
}
