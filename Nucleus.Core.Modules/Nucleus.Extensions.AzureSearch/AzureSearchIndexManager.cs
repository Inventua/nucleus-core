using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.Logging;

namespace Nucleus.Extensions.AzureSearch;

[DisplayName("Azure Search")]
public class AzureSearchIndexManager : ISearchIndexManager
{
  private ILogger<AzureSearchIndexManager> Logger { get; }
  private List<string> AzureFileSystemProviders { get; } = [];
  private IContentConverterFactory ContentConverter { get; }

  private AzureSearchRequest _request { get; set; }
  private Boolean HasStartedIndexers { get; set; }

  public AzureSearchIndexManager(IConfiguration configuration, IFileSystemManager fileSystemManager, IContentConverterFactory contentConverter, ILogger<AzureSearchIndexManager> logger)
  {
    this.AzureFileSystemProviders= GetAzureFileSystemProviders(configuration, fileSystemManager);
    this.ContentConverter = contentConverter;
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
      }
    }

    return results;
  }

  private AzureSearchRequest AzureRequest(Site site)
  {
    AzureSearchSettings settings = new(site);

    if (String.IsNullOrEmpty(settings.AzureSearchServiceEndpoint))
    {
      throw new InvalidOperationException($"The Azure search server url is not set for site '{site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Azure search index name is not set for site '{site.Name}'.");
    }

    if (_request == null || !_request.Equals(settings.AzureSearchServiceEndpoint, settings.IndexName, settings.AzureSearchServiceEncryptedApiKey))
    {
      _request = new
      (
        site,
        settings,
        this.Logger
      );
    }

    return _request;
  }

  public Task ClearIndex(Site site)
  {
    throw new NotSupportedException("The Azure Search extension does not support the 'Clear Index' operation.");
  }

  public async Task Index(ContentMetaData metadata)
  {
    if (metadata.Site == null)
    {
      throw new NullReferenceException("metaData.Site must not be null.");
    }

    AzureSearchSettings settings = new(metadata.Site);

    if (!HasStartedIndexers)
    {
      this.HasStartedIndexers = true;
      await this.AzureRequest(metadata.Site).RunIndexers();
    }

    AzureSearchDocument document = new(metadata);

    if (metadata.Content != null && !IsAzureFile(metadata, this.AzureFileSystemProviders) && metadata.Content.Length != 0 && (settings.AttachmentMaxSize == 0 || metadata.Content.Length <= settings.AttachmentMaxSize * 1024 * 1024))
    {
      // set content if the content size is:
      // - not stored in Azure Blob Storage
      // - less than "max size"
      // - Can be converted to text by a content converter (except for Html content, which is converted to Markdown). ConverterFactory.ConvertTo will throw a InvalidOperationException if the content type
      //   can't be converted. If we get an exception we set content to null, which prevents the content property in the index entry from being modified (see comments below) 

      try
      {
        switch (document.ContentType)
        {
          case "application/pdf":
            // Convert to plain text and perform special post-processing 
            document.Content = RemoveInvalidChars(ToString(await this.ContentConverter.ConvertTo(metadata.Site, metadata.Content, metadata.ContentType, "text/plain")));
            break;

          case "text/html":
            //  Remove non-content HTML elements and attributes, then try to convert html format content to markdown, which maintains the structure of
            //  headings, tables, etc but is less verbose than HTML.
            byte[] htmlContent = CleanHtml(metadata.Content, metadata.ContentType, ["header", "footer", "script", "style", "noscript", "iframe", "link", "input", "img", "svg", "nav"], ["id", "class", "href", "method", "action", "enctype", "tabindex"], true);
            document.Content = ToString(await this.ContentConverter.ConvertTo(metadata.Site, htmlContent, metadata.ContentType, "text/markdown"));
            break;

          case "application/x-zip-compressed":
            // zip files can't be converted
            document.Content = null;
            break;

          default:
            // other formats, convert to plain text
            document.Content = ToString(await this.ContentConverter.ConvertTo(metadata.Site, metadata.Content, metadata.ContentType, "text/plain"));
            break;
        }
      }
      catch (InvalidOperationException ex)
      {
        // content could not be converted. Set content to null.
        this.Logger.LogWarning("{url}: {message}", metadata.Url, ex.Message);
        document.Content = null;
      }
    }
    else
    {
      // setting content to null prevents the content property from being overwritten by IndexDocumentsAsync when we use the MergeOrUpload option,
      // and when the Json serializer DefaultIgnoreCondition option is set to JsonIgnoreCondition.WhenWritingNull. This is important, as it
      // allows the two-part feed to work (the Azure Search indexer sets .Content, and the push feed doesn't overwrite it)
      document.Content = null;
    }

    await this.AzureRequest(metadata.Site).IndexContent(document);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  private static string ToString(byte[] value)
  {
    return System.Text.Encoding.UTF8.GetString(value);
  }

  /// <summary>
  /// Remove non-English characters.
  /// </summary>
  /// <param name="word"></param>
  /// <returns></returns>
  /// <remarks>
  /// Non-English characters cause problems for our word-splitting code, which uses English/Latin word breaking characters.
  /// </remarks>
  private static string RemoveInvalidChars(string value)
  {
    // some documents use unicode character \u0019 "end of medium" instead of a space. There are other control
    // characters that may be used that way - we replace all "control" characters (00-31) with spaces
    //word = Regex.Replace(word, "[\u0000\u001f]*", " ").Trim();
    value = Regex.Replace(value, "[\u0000-\u001f]", " ").Trim();

    // !-z means all characters with a character code between ! and z, which includes 
    // most punctuation characters, numbers, and alphabetic characters.
    return Regex.Replace(value, "[^!-z{|} ]*", "").Trim();
  }

  internal static Boolean IsAzureFile(ContentMetaData content, List<string> azureFileSystemProviders)
  {
    if (content.Scope != Nucleus.Abstractions.Models.FileSystem.File.URN)
    {
      // if the resource is not a file, it can't be stored in Azure Blob Storage
      return false;
    }
    else
    {
      return azureFileSystemProviders.Any(provider => content.RawUri?.StartsWith(provider, StringComparison.OrdinalIgnoreCase) == true);
    }
  }

  public static byte[] CleanHtml(byte[] content, string contentType, IEnumerable<string> removeTags, IEnumerable<string> removeAttributes, Boolean removeEmptyElements)
  {
    if (contentType != "text/html") return content;
    if (content == null || content.Length == 0) return content;

    HtmlAgilityPack.HtmlDocument htmlDocument = new();
    htmlDocument.LoadHtml(System.Text.Encoding.UTF8.GetString(content));

    // remove comments
    HtmlAgilityPack.HtmlNodeCollection commentNodes = htmlDocument.DocumentNode.SelectNodes("//comment()");
    if (commentNodes != null)
    {
      foreach (HtmlNode comment in commentNodes)
      {
        comment.ParentNode.RemoveChild(comment);
      }
    }

    // remove specified elements
    foreach (string tag in removeTags)
    {
      HtmlAgilityPack.HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes($"//{tag}");
      if (nodes != null)
      {
        foreach (HtmlAgilityPack.HtmlNode element in nodes.ToList())
        {
          element.ParentNode.RemoveChild(element);
        }
      }
    }

    // remove specified attributes
    foreach (string attribute in removeAttributes)
    {
      HtmlAgilityPack.HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes($"//*[@{attribute}]");
      if (nodes != null)
      {
        foreach (HtmlAgilityPack.HtmlNode element in nodes.ToList())
        {
          element.Attributes.Remove(attribute);
        }
      }
    }

    // remove elements with no children or text
    if (removeEmptyElements)
    {
      HtmlAgilityPack.HtmlNode element = htmlDocument.DocumentNode.SelectSingleNode($"//*[count(*)=0 and normalize-space(.)='']");
      while (element != null)
      {
        element.ParentNode.RemoveChild(element);
        element = htmlDocument.DocumentNode.SelectSingleNode($"//*[count(*)=0 and normalize-space(.)='']");
      }
    }

    // return as UTF-encoded byte[] array 
    return System.Text.Encoding.UTF8.GetBytes(htmlDocument.DocumentNode.OuterHtml);
  }

  public async Task Remove(ContentMetaData metadata)
  {
    AzureSearchDocument document = new(metadata);

    await this.AzureRequest(metadata.Site).RemoveContent(document);
  }
}
