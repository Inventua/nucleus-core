using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch
{
  // https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/elasticsearch-net-getting-started.html

  public class AzureSearchIndexManager : ISearchIndexManager
  {
    private ILogger<AzureSearchIndexManager> Logger { get; }

    private AzureSearchRequest _request { get; set; }

    public AzureSearchIndexManager(ILogger<AzureSearchIndexManager> logger)
    {
      this.Logger = logger;
    }

    private AzureSearchRequest Request(Site site)
    {
      ConfigSettings settings = new(site);

      if (String.IsNullOrEmpty(settings.ServerUrl))
      {
        throw new InvalidOperationException($"The Azure search server url is not set for site '{site.Name}'.");
      }

      if (String.IsNullOrEmpty(settings.IndexName))
      {
        throw new InvalidOperationException($"The Azure search index name is not set for site '{site.Name}'.");
      }

      if (_request == null || !_request.Equals(new System.Uri(settings.ServerUrl), settings.IndexName, ConfigSettings.DecryptApiKey(site, settings.EncryptedApiKey)))
      {
        _request = new(new System.Uri(settings.ServerUrl), ConfigSettings.DecryptApiKey(site, settings.EncryptedApiKey), settings.IndexName, TimeSpan.FromSeconds(settings.IndexingPause));
      }

      return _request;
    }


    public async Task ClearIndex(Site site)
    {
      if (site == null)
      {
        throw new NullReferenceException("site must not be null.");
      }

      await this.Request(site).DeleteIndex();

      // re-create the index
      _request = null;
      await this.Request(site).Connect();
    }
  

    public async Task Index(ContentMetaData metadata)
    {
      if (metadata.Site == null)
      {
        throw new NullReferenceException("metaData.Site must not be null.");
      }

      ConfigSettings settings = new(metadata.Site);

      AzureSearchDocument document = new(metadata, settings);

      // test use
      if (document.ContentType == "text/html")
      {
        await this.Request(metadata.Site).IndexContent(document);
      }

      // free up memory - file content is part of the feed data, and this could exhaust available memory 
      document.Dispose();
    }

    public async Task Remove(ContentMetaData metaData)
    {
      ConfigSettings settings = new(metaData.Site);

      AzureSearchDocument document = new(metaData, settings);

      await this.Request(metaData.Site).RemoveContent(document);
    }

  }
}
