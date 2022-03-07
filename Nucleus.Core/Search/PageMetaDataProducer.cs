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
using System.Net.Http;

namespace Nucleus.Core.Search
{
	public class PageMetaDataProducer : IContentMetaDataProducer
	{
		private IPageManager PageManager { get; }
		private ILogger<PageMetaDataProducer> Logger { get; }
		private HttpClient HttpClient { get; }

		public PageMetaDataProducer(HttpClient httpClient, IPageManager pageManager, ILogger<PageMetaDataProducer> logger)
		{
			this.PageManager = pageManager;
			this.HttpClient = httpClient;
			this.Logger = logger;
		}

		public async override Task<IEnumerable<ContentMetaData>> ListItems(Site site)
		{
			List<ContentMetaData> results = new();

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {siteId} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				foreach (Page page in (await this.PageManager.List(site)).Where(page => !page.Disabled))
				{
					Logger.LogInformation("Building meta-data for page {pageId} [{pageName}]", page.Id, page.Name);
					// we have to .Get the site and page because the .List methods don't return fully-populated page objects
					ContentMetaData metaData = await BuildContentMetaData(site, await this.PageManager.Get(page.Id));

					if (metaData != null)
					{
						results.Add(metaData);
					}
				}
			}

			return results;
		}

		private async Task<ContentMetaData> BuildContentMetaData(Site site, Page page)
		{
			System.IO.MemoryStream htmlContent = new();
			string pageRelativeUrl = PageLink(page);

			if (!String.IsNullOrEmpty(pageRelativeUrl))
			{
				Uri pageUri = new(new Uri("http" + Uri.SchemeDelimiter + site.DefaultSiteAlias.Alias), pageRelativeUrl);

				ContentMetaData contentItem = new()
				{
					Site = site,
					Title = !string.IsNullOrEmpty(page.Title) ? page.Title : page.Name,
					Summary = page.Description,
					Url = pageUri.ToString(),
					PublishedDate = page.DateChanged.HasValue ? page.DateChanged : page.DateAdded,
					SourceId = page.Id,
					Scope = Page.URN,
					Keywords = page.Keywords?.Split(',').ToList(),
					Roles = await GetViewRoles(page)
				};

				// todo: write a magic cookie/header to so pages are rendered with all permissions (API key)
				// todo: add a header/something to render the page in a simple form
				// todo: consider System.Net.Http.HttpClient

				//System.Net.WebRequest request = System.Net.WebRequest.Create(pageUri);
				//System.Net.WebResponse response = request.GetResponse();

				System.Net.Http.HttpRequestMessage request = new(HttpMethod.Get, pageUri);
				System.Net.Http.HttpResponseMessage response = this.HttpClient.Send(request);

				using (System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync()) // GetResponseStream())
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

				return contentItem;
			}

			Logger.LogWarning("Could not build page meta-data because the page has no routes.");
			return null;
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
