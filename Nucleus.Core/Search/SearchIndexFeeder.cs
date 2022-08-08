using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Core.Logging;

namespace Nucleus.Core.Search
{
	[System.ComponentModel.DisplayName("Nucleus Core: Search Index Feeder")]
	public class SearchIndexFeeder : IScheduledTask
	{
		private ISiteManager SiteManager { get; }
		private IEnumerable<IContentMetaDataProducer> SearchContentProviders { get; }
		private IEnumerable<ISearchIndexManager> SearchIndexManagers { get; }
		private ILogger<SearchIndexFeeder> Logger { get; }

		public SearchIndexFeeder(IEnumerable<IContentMetaDataProducer> searchContentProviders, IEnumerable<ISearchIndexManager> searchIndexManagers, ISiteManager siteManager, ILogger<SearchIndexFeeder> logger)
		{
			this.SiteManager = siteManager;
			this.SearchContentProviders = searchContentProviders;
			this.SearchIndexManagers = searchIndexManagers;
			this.Logger = logger;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			this.Logger.LogInformation("Search Index Feeder Started.");

			if (!this.SearchIndexManagers.Any())
			{
				this.Logger.LogWarning("There are no search index managers configured, search feed indexing cancelled.");
				return Task.CompletedTask;
			}

			if (!this.SearchContentProviders.Any())
			{
				this.Logger.LogWarning("There are no search content providers configured, search feed indexing cancelled.");
				return Task.CompletedTask;
			}

			return CreateAndSubmitFeed(task, progress, cancellationToken);
		}

		private async Task CreateAndSubmitFeed(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			foreach (IContentMetaDataProducer contentProvider in this.SearchContentProviders)
			{
				foreach (Site site in await this.SiteManager.List())
				{
					// .List doesn't fully populate the site object, so we call .Get 
					Site fullSite = await this.SiteManager.Get(site.Id);

					try
					{
						this.Logger.LogTrace("Getting search content from {type} for site '{site}'.", contentProvider.GetType().FullName, site.Name);
						await foreach (ContentMetaData item in contentProvider.ListItems(fullSite))
						{
							item.Url = ParseUrl(item.Url);
							this.Logger.LogTrace("Adding [{scope}] {url} to index.", item.Scope, item.Url);

							foreach (ISearchIndexManager searchIndexManager in this.SearchIndexManagers)
							{
								try
								{
									searchIndexManager.Index(item);
									this.Logger.LogTrace("Added [{scope}] {url} to index ({searchIndexManager}).", item.Scope, item.Url, searchIndexManager.GetType());
								}
								catch (NotImplementedException)
								{
									// ignore
								}
								catch (Exception e)
								{
									// error in .Index implementation
									this.Logger.LogError(e, "Error adding [{scope}] {url} to index using {type}.Index()", item.Scope, item.Url, searchIndexManager.GetType().FullName);
								}
							}

							item.Dispose();
						}
					}
					catch (NotImplementedException)
					{
						// ignore
					}
					catch (Exception e)
					{
						// error in .ListItems implementation
						this.Logger.LogError(e, "Error in {0}.ListItems()", contentProvider.GetType().FullName);
					}
				}
			}

			this.Logger.LogInformation("Search Index Feeder Completed.");
			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
		}

		private string ParseUrl(string url)
		{
			if (url.StartsWith('~'))
			{
				url = url.Substring(1);
			}
			
			if (!url.EndsWith('/'))
			{
				url += "/";
			}

			return url;
		}
	}
}
