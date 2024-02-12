using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Search;

namespace Nucleus.Core.Services.HealthChecks;

public class SearchProvidersHealthCheck : IHealthCheck
{
  private ISiteManager SiteManager { get; }
  private IEnumerable<ISearchIndexManager> SearchIndexManagers { get; }

  public SearchProvidersHealthCheck(ISiteManager siteManager, IEnumerable<ISearchIndexManager> searchIndexManagers)
  {
    this.SiteManager = siteManager;
    this.SearchIndexManagers = searchIndexManagers;
  }

  /// <summary>
  /// Check search index managers.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    Dictionary<string, object> searchProviderCheckResults = new();

    foreach (Site site in await this.SiteManager.List())
    {
      Site fullSite = await this.SiteManager.Get(site.Id);

      // Test connections for each site/search index manager
      foreach (ISearchIndexManager searchIndexManager in this.SearchIndexManagers)
      {
        try
        {
          if (!await searchIndexManager.CanConnect(fullSite))
          {
            searchProviderCheckResults.Add("Connection Failure", $"Search index provider '{searchIndexManager.GetType().FullName}' did not connect using the settings for site '{fullSite.Name}', and will not receive data.");
          }
        }
        catch (Exception)
        {
          searchProviderCheckResults.Add("Connection Error", $"Search index provider '{searchIndexManager.GetType().FullName}' returned an error when connecting using the settings for site '{fullSite.Name}', and will not receive data.");
        }
      }
    }

    if (searchProviderCheckResults.Any())
    {
      // search index provider connection errors are not fatal, so we report "degraded" rather than "Unhealthy"
      return new HealthCheckResult(HealthStatus.Degraded, "One or more search index provider connection tests failed.", data: searchProviderCheckResults);
    }
    else
    {
      return HealthCheckResult.Healthy();
    }
  }

}
