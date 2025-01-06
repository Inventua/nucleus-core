using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
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
      Dictionary<Site, List<ISearchIndexManager>> sites = new();
      //List<Site> sites = new();

      foreach (Site baseSite in await this.SiteManager.List())
      {
        List<ISearchIndexManager> activeSearchIndexManagers = new();

        // .List doesn't fully populate the site object, so we call .Get 
        Site site = await this.SiteManager.Get(baseSite.Id);

        // Test connections for each site/search index manager
        foreach (ISearchIndexManager searchIndexManager in this.SearchIndexManagers)
        {
          Boolean indexManagerEnabled = true;

          if (site.SiteSettings.TryGetValue($"{Site.SiteSearchSettingsKeys.SEARCH_INDEX_MANAGER_PREFIX}:{searchIndexManager.GetType().FullName.ToLower()}:enabled", out Boolean isEnabled))
          {
            indexManagerEnabled = isEnabled;
          }
          
          try
          {
            if (indexManagerEnabled)
            {
              if (await searchIndexManager.CanConnect(site))
              {
                activeSearchIndexManagers.Add(searchIndexManager);
              }
              else
              {
                this.Logger?.LogWarning("Search index provider {providername} did not connect using the settings for site '{site}', and will not receive data.", searchIndexManager.GetType().FullName, site.Name);
              }
            }
            else
            {
              this.Logger?.LogWarning("Search index provider {providername} is disabled for site '{site}', and will not receive data.", searchIndexManager.GetType().FullName, site.Name);
            }
          }
          catch (Exception e)
          {
            this.Logger?.LogError(e, "Search index provider {providername} returned an error when connecting using the settings for site '{site}', and will not receive data.", searchIndexManager.GetType().FullName, site.Name);
          }
        }

        sites.Add(site, activeSearchIndexManagers);
        await ClearIndexes(site, activeSearchIndexManagers);
      }

      foreach (IContentMetaDataProducer contentProvider in this.SearchContentProviders)
      {
        foreach (KeyValuePair<Site, List<ISearchIndexManager>> siteInfo in sites)
        {
          siteInfo.Key.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);

          if (!siteInfo.Value.Any())
          {
            this.Logger?.LogError("There are no available search index providers for site '{site}', so the search feed was terminated.", siteInfo.Key.Name);
          }
          else
          {
            // get content for the site (once) and send each item to each available search index manager for indexing
            try
            {
              this.Logger.LogTrace("Getting search content from {type} for site '{site}'.", contentProvider.GetType().FullName, siteInfo.Key.Name);

              await foreach (ContentMetaData item in contentProvider.ListItems(siteInfo.Key))
              {
                // allow IContentMetaDataProducer implementation to return nulls (and silently skip them)
                if (item != null)
                {
                  Boolean indexSuccess = false;
                  item.Url = ToAbsolute(useSsl, siteInfo.Key.DefaultSiteAlias.Alias, ParseUrl(item.Url));
                                
                  this.Logger.LogTrace("Adding [{scope}] {url} to index.", item.Scope, item.Url);

                  foreach (ISearchIndexManager searchIndexManager in siteInfo.Value)
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
                      this.Logger.LogError(e, "Error adding [{scope}] {url} ({title}) to index using {type}.Index()", item.Scope, item.Url, item.Title, searchIndexManager.GetType().FullName);
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

    private static string ToAbsolute(Boolean useSsl, string alias, string url)
    {
      // convert relative Urls to absolute. If item.Url is already absolute, the System.Uri(Uri, string) constructor ignores baseUri.
      // https://learn.microsoft.com/en-us/dotnet/api/system.uri.-ctor?view=net-9.0#system-uri-ctor(system-uri-system-string)
      // "If relativeUri is an absolute URI (containing a scheme, host name, and optionally a port number), the Uri
      // instance is created using only relativeUri.")
      Uri absoluteUri = new(new Uri((useSsl ? "https" : "http") + Uri.SchemeDelimiter + alias), url);
      return absoluteUri.ToString();
    }

    private static string ParseUrl(string url)
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
