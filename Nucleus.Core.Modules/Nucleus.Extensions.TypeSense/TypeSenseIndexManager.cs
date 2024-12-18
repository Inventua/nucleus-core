using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.Logging;

namespace Nucleus.Extensions.TypeSense;

[DisplayName("Typesense")]
public class TypeSenseIndexManager : ISearchIndexManager
{
  private IHttpClientFactory HttpClientFactory { get;}
  private IContentConverterFactory ContentConverter { get; }

  private ILogger<TypeSenseIndexManager> Logger { get; }

  private TypeSenseRequest CachedRequest { get; set; }

  private readonly string[] ARCHIVE_MIME_TYPES = ["application/x-zip-compressed", "application/x-gzip", "application/x-tar", "application/x-7z-compressed"];

  public TypeSenseIndexManager(IHttpClientFactory httpClientFactory, IContentConverterFactory contentConverter, ILogger<TypeSenseIndexManager> logger)
  {
    this.HttpClientFactory = httpClientFactory;
    this.ContentConverter = contentConverter;
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

    if (CachedRequest == null || !CachedRequest.Equals(new System.Uri(settings.ServerUrl), settings.IndexName, Models.Settings.DecryptApiKey(site, settings.EncryptedApiKey)))
    {
      CachedRequest = new
      (
        this.HttpClientFactory,
        new System.Uri(settings.ServerUrl),
        settings.IndexName,
        Models.Settings.DecryptApiKey(site, settings.EncryptedApiKey),
        TimeSpan.FromSeconds(settings.IndexingPause), 
        this.Logger
      );
    }

    return CachedRequest;
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

    try
    {
      document.Content = await ToText(metadata, settings);      
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "An error was encounted converting to text content for {url}. The index entry will be created with meta-data (no content) only.", metadata.Url);      
    }

    await this.Request(metadata.Site).IndexContent(document);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  public async Task<string> ToText(ContentMetaData metaData, Models.Settings settings)
  {
    if (metaData.Content.Length > settings.AttachmentMaxSize * 1024 * 1024)
    {
      return null;
    }
    else if (metaData.Content.Length == 0)
    {
      return null;
    }
    else if (ARCHIVE_MIME_TYPES.Contains(metaData.ContentType))
    {
      // prevent conversion of archives. If the Tika server converter is installed, it can convert archive formats to text by extracting text from every
      // file in the archive, which can generate huge numbers of index entries, which are mostly unwanted, as the contents of archive files are often
      // install sets, etc rather than content that we want to index.
      return null;
    }
    else
    {
      return System.Text.Encoding.UTF8.GetString(await this.ContentConverter.ConvertTo(metaData.Site, metaData.Content, metaData.ContentType, "text/plain"));      
    }
  }

  public async Task Remove(ContentMetaData metadata)
  {    
    await this.Request(metadata.Site).RemoveContent(TypeSenseDocument.GenerateId(metadata));
  }
}
