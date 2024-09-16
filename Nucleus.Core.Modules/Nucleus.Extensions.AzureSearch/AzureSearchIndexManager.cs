using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Extensions.AzureSearch;

public class AzureSearchIndexManager : ISearchIndexManager
{
  private ILogger<AzureSearchIndexManager> Logger { get; }
  private List<string> AzureFileSystemProviders { get; } = [];

  private AzureSearchRequest _request { get; set; }
  private Boolean HasStartedIndexer { get; set; }

  public AzureSearchIndexManager(IConfiguration configuration, IFileSystemManager fileSystemManager, ILogger<AzureSearchIndexManager> logger)
  {
    this.AzureFileSystemProviders= GetAzureFileSystemProviders(configuration, fileSystemManager);
    this.Logger = logger;
  }

  /// <summary>
  /// Return a list of service Urls for all configured Azure file system providers.
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="fileSystemManager"></param>
  /// <remarks>
  /// The returned list is used to determine whether a ContentMetaData item represents a file which is stored in Azure Blob Storage. The
  /// code in the AzureSearchDocument constructor needs to set the content property to NULL for files in Azure Blob Storage, so that we 
  /// don't overwrite content which was populated by the Azure Search indexer.
  /// </remarks>
  private List<string> GetAzureFileSystemProviders(IConfiguration configuration, IFileSystemManager fileSystemManager)
  {
    List<string> results = [];

    List<Abstractions.FileSystemProviders.FileSystemProviderInfo> providers = fileSystemManager.ListProviders()
      .ToList()
      .Where(provider => provider.ProviderType.Contains("AzureBlobStorageFileSystemProvider"))
      .ToList();

    for (int count = 1; count < providers.Count + 1; count++)
    {
      string configKeyPrefix = $"{Nucleus.Abstractions.Models.Configuration.FileSystemProviderFactoryOptions.Section}:Providers:{count}";
      if (configuration.GetValue<string>($"{configKeyPrefix}:Key") == providers.First().Key)
      {
        string connectionString = configuration.GetValue<string>($"{configKeyPrefix}:ConnectionString");

        Azure.Storage.Blobs.BlobServiceClient azureBlobStorageClient = new(connectionString);
        results.Add(azureBlobStorageClient.Uri.ToString());

        //string[] connectionStringParts = connectionString.Split(';');
        //string defaultEndpointsProtocol = connectionStringParts
        // .Where(part => part.StartsWith("DefaultEndpointsProtocol", StringComparison.OrdinalIgnoreCase))
        // .Select(part => part.Split('=').LastOrDefault())
        // .FirstOrDefault();
        //string accountName = connectionStringParts
        //  .Where(part => part.StartsWith("AccountName", StringComparison.OrdinalIgnoreCase))
        //  .Select(part => part.Split('=').LastOrDefault())
        //  .FirstOrDefault();
        //string endPointSuffix = connectionStringParts
        //  .Where(part => part.StartsWith("EndpointSuffix", StringComparison.OrdinalIgnoreCase))
        //  .Select(part => part.Split('=').LastOrDefault())
        //  .FirstOrDefault();

        //if (!String.IsNullOrEmpty(defaultEndpointsProtocol) && !String.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(endPointSuffix))
        //{
        //  results.Add($"{defaultEndpointsProtocol}{Uri.SchemeDelimiter}{accountName}.blob.{endPointSuffix}");
        //}
      }
    }

    return results;
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

    AzureSearchDocument document = new(metadata, settings, this.AzureFileSystemProviders);

    await this.Request(metadata.Site).IndexContent(document);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  public async Task Remove(ContentMetaData metadata)
  {
    ConfigSettings settings = new(metadata.Site);

    AzureSearchDocument document = new(metadata, settings, this.AzureFileSystemProviders);

    await this.Request(metadata.Site).RemoveContent(document);
  }
}
