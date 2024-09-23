using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.Logging;

namespace Nucleus.Extensions.TypeSense;

public class TypeSenseIndexManager : ISearchIndexManager
{
  private IHttpClientFactory HttpClientFactory { get;}

  private ILogger<TypeSenseIndexManager> Logger { get; }

  private TypeSenseRequest _request { get; set; }

  public TypeSenseIndexManager(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<TypeSenseIndexManager> logger)
  {
    this.HttpClientFactory = httpClientFactory;
    this.Logger = logger;
  }

  private TypeSenseRequest Request(Site site)
  {
    Models.Settings settings = new(site);

    if (String.IsNullOrEmpty(settings.ServerUrl))
    {
      throw new InvalidOperationException($"The TypeSense search server url is not set for site '{site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The TypeSense search index name is not set for site '{site.Name}'.");
    }

    if (_request == null || !_request.Equals(new System.Uri(settings.ServerUrl), settings.IndexName, Models.Settings.DecryptApiKey(site, settings.EncryptedApiKey)))
    {
      _request = new
      (
        this.HttpClientFactory,
        new System.Uri(settings.ServerUrl),
        settings.IndexName,
        Models.Settings.DecryptApiKey(site, settings.EncryptedApiKey),
        TimeSpan.FromSeconds(settings.IndexingPause)
      );
    }

    return _request;
  }

  public async Task<Boolean> CanConnect(Site site)
  {
    if (site == null)
    {
      throw new NullReferenceException("site must not be null.");
    }

    return await this.Request(site).Connect();
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

    Models.Settings settings = new(metadata.Site);
      
    TypeSenseDocument document = new(metadata, settings);

    await this.Request(metadata.Site).IndexContent([document]);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  public async Task Remove(ContentMetaData metadata)
  {
    Models.Settings settings = new(metadata.Site);

    await this.Request(metadata.Site).RemoveContent(TypeSenseDocument.GenerateId(metadata));
  }
}
