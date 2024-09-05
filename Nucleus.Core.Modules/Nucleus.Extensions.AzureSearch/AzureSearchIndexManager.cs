using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch;

public class AzureSearchIndexManager : ISearchIndexManager
{
  private ILogger<AzureSearchIndexManager> Logger { get; }

  private AzureSearchRequest _request { get; set; }
  private Boolean HasStartedIndexer { get; set; }

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
      _request = new
      (
        new System.Uri(settings.ServerUrl),
        ConfigSettings.DecryptApiKey(site, settings.EncryptedApiKey),
        settings.IndexName,
        settings.IndexerName,
        settings.SemanticConfigurationName,
        settings.VectorizationEnabled,
        settings.AzureOpenAIEndpoint,
        ConfigSettings.DecryptApiKey(site, settings.EncryptedAzureOpenAIApiKey),
        settings.AzureOpenAIDeploymentName,
        this.Logger
      );
    }

    return _request;
  }

  public async Task ClearIndex(Site site)
  {
    if (site == null)
    {
      throw new NullReferenceException("site must not be null.");
    }

    await this.Request(site).ClearIndex();
  }

  public async Task Index(ContentMetaData metadata)
  {
    if (metadata.Site == null)
    {
      throw new NullReferenceException("metaData.Site must not be null.");
    }

    ConfigSettings settings = new(metadata.Site);

    if (!HasStartedIndexer)
    {
      this.HasStartedIndexer = true;
      if (!string.IsNullOrEmpty(settings.IndexerName))
      {
        await this.Request(metadata.Site).RunIndexer(settings.IndexerName);
      }
    }

    AzureSearchDocument document = new(metadata, settings);

    await this.Request(metadata.Site).IndexContent(document);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  public async Task Remove(ContentMetaData metadata)
  {
    ConfigSettings settings = new(metadata.Site);

    AzureSearchDocument document = new(metadata, settings);

    await this.Request(metadata.Site).RemoveContent(document);
  }
}
