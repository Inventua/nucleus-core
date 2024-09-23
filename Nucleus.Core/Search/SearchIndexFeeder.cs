using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Search;
using Nucleus.Core.Logging;
using Nucleus.Extensions;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Search
{
  [System.ComponentModel.DisplayName("Nucleus Core: Search Index Feeder")]
  public class SearchIndexFeeder : IScheduledTask
  {
    private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }
    private ISiteManager SiteManager { get; }
    private IEnumerable<IContentMetaDataProducer> SearchContentProviders { get; }
    private IEnumerable<ISearchIndexManager> SearchIndexManagers { get; }
    private ILogger<SearchIndexFeeder> Logger { get; }

    public SearchIndexFeeder(ISearchIndexHistoryManager searchIndexHistoryManager, IEnumerable<IContentMetaDataProducer> searchContentProviders, IEnumerable<ISearchIndexManager> searchIndexManagers, ISiteManager siteManager, ILogger<SearchIndexFeeder> logger)
    {
      this.SearchIndexHistoryManager = searchIndexHistoryManager;
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
      Dictionary<Guid, List<ISearchIndexManager>> activeSiteSearchIndexManagers = new();
      List<Site> sites = new();

      foreach (Site site in await this.SiteManager.List())
      {
        List<ISearchIndexManager> activeSearchIndexManagers = new();

        // .List doesn't fully populate the site object, so we call .Get 
        Site fullSite = await this.SiteManager.Get(site.Id);
        sites.Add(fullSite);

        // Test connections for each site/search index manager
        foreach (ISearchIndexManager searchIndexManager in this.SearchIndexManagers)
        {
          try
          {
            if (await searchIndexManager.CanConnect(fullSite))
            {
              activeSearchIndexManagers.Add(searchIndexManager);              
            }
            else
            {
              this.Logger?.LogWarning("Search index provider {providername} did not connect using the settings for site '{site}', and will not receive data.", searchIndexManager.GetType().FullName, fullSite.Name);
            }
          }
          catch (Exception e)
          {
            this.Logger?.LogError(e, "Search index provider {providername} returned an error when connecting using the settings for site '{site}', and will not receive data.", searchIndexManager.GetType().FullName, fullSite.Name);
          }
        }

        activeSiteSearchIndexManagers.Add(site.Id, activeSearchIndexManagers);
        await ClearIndexes(fullSite, activeSearchIndexManagers);
      }

      foreach (IContentMetaDataProducer contentProvider in this.SearchContentProviders)
      {
        foreach (Site site in sites)
        {
          List<ISearchIndexManager> activeSearchIndexManagers = activeSiteSearchIndexManagers[site.Id];
                               
          if (!activeSearchIndexManagers.Any())
          {
            this.Logger?.LogError("There are no available search index providers for site '{site}', so the search feed was terminated.", site.Name);
          }
          else
          {
            // get content for the site (once) and send each item to each available search index manager for indexing
            try
            {
              this.Logger.LogTrace("Getting search content from {type} for site '{site}'.", contentProvider.GetType().FullName, site.Name);

              await foreach (ContentMetaData item in contentProvider.ListItems(site))
              {
                // allow IContentMetaDataProducer implementation to return nulls (and silently skip them)
                if (item != null)
                {
                  Boolean indexSuccess = false;

                  item.Url = ParseUrl(item.Url);
                  this.Logger.LogTrace("Adding [{scope}] {url} to index.", item.Scope, item.Url);

                  foreach (ISearchIndexManager searchIndexManager in activeSearchIndexManagers)
                  {
                    try
                    {
                      await searchIndexManager.Index(item);
                      indexSuccess = true;
                      this.Logger.LogInformation("Added [{scope}] {url} to index ({searchIndexManager}).", item.Scope, item.Url, searchIndexManager.GetType());
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

                  // store the search index last updated data for the content item
                  if (indexSuccess && item.SourceId != null && !String.IsNullOrEmpty(item.Scope))
                  {
                    // search indexing history is indexed by site, sourceid, scope
                    await this.SearchIndexHistoryManager.Save(new() { SiteId = item.Site.Id, Scope = item.Scope, SourceId = item.SourceId.Value, Url = item.Url, LastIndexedDate = DateTime.UtcNow });                    
                  }
                  item.Dispose();
                }
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
      }

      this.Logger.LogInformation("Search Index Feeder Completed.");
      progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
    }

    private async Task ClearIndexes(Site site, List<ISearchIndexManager> searchIndexManagers)
    {
      if (site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.CLEAR_INDEX, out Boolean clearIndex))
      {
        if (clearIndex)
        {
          foreach (ISearchIndexManager searchIndexManager in searchIndexManagers)
          {
            this.Logger.LogTrace("Clearing index for provider {providername}, site '{site}'.", searchIndexManager.GetType().FullName, site.Name);
            try
            {
              await searchIndexManager.ClearIndex(site);
            }
            catch (Exception e)
            {
              // don't fail if ClearIndex failed, but log it
              this.Logger?.LogError(e, "Clearing Index for provider {providername}, site '{site}'.", searchIndexManager.GetType().FullName, site.Name);
            }
          }

          await this.SearchIndexHistoryManager.Delete(site.Id);
        }
      }
    }

    private string ParseUrl(string url)
    {
      if (url.StartsWith('~'))
      {
        url = url[1..];
      }

      if (!url.EndsWith('/') && !url.Contains('#'))
      {
        url += "/";
      }

      return url;
    }
  }
}
