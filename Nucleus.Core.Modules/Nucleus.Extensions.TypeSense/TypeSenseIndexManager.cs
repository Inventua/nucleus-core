using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

  private readonly string[] HtmlElements = ["div", "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", ""];


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

    try
    {
      document.Content = await ToText(metadata, settings);
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "An error was encounted deriving text content for {url}. The index entry will be created with meta-data (no content) only.", metadata.Url);      
    }

    await this.Request(metadata.Site).IndexContent([document]);

    // free up memory - file content is part of the feed data, and this could exhaust available memory 
    document.Dispose();
  }

  public async Task<string?> ToText(ContentMetaData metaData, Models.Settings settings)
  {
    if (metaData.Content.Length == 0)
    {
      return null;
    }
    else
    {
      if (!String.IsNullOrEmpty(settings.TikaServerUrl))
      {
        HttpClient client = this.HttpClientFactory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Put, new System.Uri( new System.Uri(settings.TikaServerUrl), "/tika"));

        ByteArrayContent content = new(metaData.Content);

        if (!string.IsNullOrEmpty(metaData.ContentType))
        {
          content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(metaData.ContentType);
        }

        request.Content = content;

        request.Headers.Accept.Add(new("text/plain"));

        HttpResponseMessage response = client.Send(request);

        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();       
      }
      else
      {
        // tika server not specified, handle content from text-based files only
        switch (metaData.ContentType)
        {
          case "text/html":
            HtmlAgilityPack.HtmlDocument htmlContent = new();
            htmlContent.LoadHtml(System.Text.Encoding.UTF8.GetString(metaData.Content));

            return ConvertHtmlToPlainText(new StringBuilder(), htmlContent.DocumentNode).ToString();

          case "text/plain":
          case "text/csv":
          case "text/markdown":
            return System.Text.Encoding.UTF8.GetString(metaData.Content);

          default:
            // TypeSense search cannot process file content from a feed, the search service must be set up with "Indexers" that run in the TypeSense Cloud
            // to collect and parse file content.
            return null;
        }
      }
    }
  }

  private StringBuilder ConvertHtmlToPlainText(StringBuilder builder, HtmlNode node)
  {
    foreach (HtmlNode subnode in node.ChildNodes)
    {
      if (subnode.NodeType == HtmlNodeType.Text && HtmlElements.Contains(node.Name, StringComparer.OrdinalIgnoreCase))
      {
        // Append the text of the current node to the StringBuilder
        if (!String.IsNullOrWhiteSpace(subnode.InnerText))
        {
          builder.AppendLine(System.Web.HttpUtility.HtmlDecode(subnode.InnerText.Trim()));
        }
      }
      else if (subnode.NodeType == HtmlNodeType.Element)
      {
        // Recursively convert the child nodes to plain text
        ConvertHtmlToPlainText(builder, subnode);
      }
    }

    return builder;
  }


  public async Task Remove(ContentMetaData metadata)
  {
    Models.Settings settings = new(metadata.Site);

    await this.Request(metadata.Site).RemoveContent(TypeSenseDocument.GenerateId(metadata));
  }
}
