using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using HtmlAgilityPack;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.TypeSense;

#nullable enable

/// <summary>
/// Class which represents an index entry ("document") in TypeSense Search.
/// </summary>
/// <remarks>
/// Most properties are nullable, because the TypeSense Search index is populated by both the TypeSense Search indexer, and by the "push" feed. When the
/// indexer creates entries, most of the meta-data properties are null - and the Json serializer can only deserialize null values when the 
/// property is nullable.
/// </remarks>
internal class TypeSenseDocument : IDisposable
{
  private bool disposedValue;
  private readonly string[] HtmlElements = ["div", "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", ""];

  /// <summary>
  /// Constructor used by deserialization.
  /// </summary>
  public TypeSenseDocument() { }

  /// <summary>
  /// Constructor used when generating a feed.
  /// </summary>
  /// <param name="content"></param>
  public TypeSenseDocument(ContentMetaData content, Models.Settings settings)
  {
    this.Id = GenerateId(content);

    this.SiteId = content.Site.Id.ToString();
    this.Url = content.Url;
    this.Title = content.Title;
    this.Summary = content.Summary;

    this.Scope = content.Scope;
    this.SourceId = content.SourceId.ToString();

    this.ContentType = content.ContentType;
    this.Type = content.Type;
        
    // set content if the content size is:
    // - less than "max size"
    // - Can be convered to text by ToText(). ToText can return null if the content type can't be converted to text
    this.Content = ToText(content);
   
    if (content.PublishedDate.HasValue)
    {
      this.PublishedDate = content.PublishedDate.Value.Date;
    }

    if (content.Size.HasValue)
    {
      this.Size = content.Size.Value;
    }

    this.Keywords = content.Keywords?.ToList() ?? [];
    this.Categories = content.Categories?.Select(category => category.Id.ToString()).ToList() ?? [];
    this.Roles = content.Roles?.Select(role => role.Id.ToString()).ToList() ?? [];

    this.IsSecure = !IsPublic(content.Site, content.Roles ?? []);
  }

  public string? ToText(ContentMetaData metaData)
  {
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

  public static string GenerateId(ContentMetaData content)
  {
    string key = content.Url;
    return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(System.Web.HttpUtility.UrlPathEncode(key)));
  }

  /// <summary>
  /// Unique id for the index entry.
  /// </summary>
  public string Id { get; set; } = "";

  /// <summary>
  /// Special field for document chunks (pages). ParentId contains the Id of the document for which this is a "chunk"
  /// </summary>
  public string? ParentId { get; set; }

  /// <summary>
  /// Page number for document chunks (pages). 
  /// </summary>
  public int? PageNumber { get; set; }

  /// <summary>
  /// This Id of the site which the resource belongs to.
  /// </summary>
  public string? SiteId { get; set; }

  /// <summary>
  /// Url used to access the resource for this search item.
  /// </summary>
  /// <remarks>
  /// This value is required.
  /// </remarks>
  public string? Url { get; set; }

  /// <summary>
  /// Title for the resource.
  /// </summary>
  /// <remarks>
  /// The title displayed in search results.  Title is optional, but highly recommended.  If the title is not set, the search result
  /// will display the Url in place of a title.
  /// </remarks>
  public string? Title { get; set; } = "";

  /// <summary>
  /// This field supports vector search, if populated by TypeSense search skill sets
  /// </summary>
  public Single[]? TitleVector { get; set; }

  /// <summary>
  /// Short summary for the resource.
  /// </summary>
  /// <remarks>
  /// The summary displayed in search results.  Summary is optional.  If it is not set, the search result will not display
  /// a summary.
  /// </remarks>
  public string? Summary { get; set; }

  /// <summary>
  /// This field supports vector search, if populated by TypeSense search skill sets
  /// </summary>
  public Single[]? SummaryVector { get; set; }

  /// <summary>
  /// URN of the entity which was used to create this search entry.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If set, it can be used to allow users to select that only specified result types are included in their
  /// search results.
  /// </remarks>
  public string? Scope { get; set; } = "";

  /// <summary>
  /// Unique Id for the search entry source.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If set, it can be used to manage the individual search result for update and delete operations.
  /// </remarks>
  public string? SourceId { get; set; } = "";

  /// <summary>
  /// Search entry content, used for content indexing.
  /// </summary>
  public string? Content { get; set; }

  /// <summary>
  /// This field supports vector search, if populated by TypeSense search skill sets
  /// </summary>
  public Single[]? ContentVector { get; set; }

  /// <summary>
  /// Search entry MIME type.
  /// </summary>
  /// <remarks>
  /// This value should be set to the MIME type of the Content field.  This value is used for search result filtering and also
  /// to tell TypeSense Search what content type is in the content field.
  /// </remarks>
  public string? ContentType { get; set; } = "";  // metadata_content_type

  /// <summary>
  /// Search entry display type.
  /// </summary>
  public string? Type { get; set; } = "";

  /// <summary>
  /// Source entity published date
  /// </summary>
  /// <remarks>
  /// This value is optional.  If specified, it can be displayed in search results.
  /// </remarks>
  public DateTime? PublishedDate { get; set; }

  /// <summary>
  /// The size of the resource in bytes.
  /// </summary>
  /// <remarks>
  /// This value is optional and should only be supplied if it is relevant to the resource.  If specified, it can be displayed in search results.
  /// </remarks>
  public long? Size { get; set; } = 0;

  /// <summary>
  /// A list of keywords for the resource.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If supplied, keywords contribute to the search result weighting.
  /// </remarks>
  public List<string> Keywords { get; set; } = [];

  /// <summary>
  /// A list of categories for the resource.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If supplied, categories contribute to the search result weighting, and may also be used to filter results.
  /// </remarks>
  public List<string> Categories { get; set; } = [];

  /// <summary>
  /// A list of roles with view access to the resource.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If it not specified, search feeders will try to fill in roles by using the roles for the relevant
  /// page, module or folder.  Roles are used to filter search results to resources which the current user can view.
  /// </remarks>
  public List<string> Roles { get; set; } = [];

  /// <summary>
  /// Specifies whether the document is visible to anonymous users.
  /// </summary>
  public Boolean? IsSecure { get; set; }

  /// <summary>
  /// Return whether the specified roles list represents a resource which is visible to all users.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="roles"></param>
  /// <returns></returns>
  private static bool IsPublic(Site site, IEnumerable<Role> roles)
  {
    foreach (Role role in roles)
    {
      if (role.Equals(site.AnonymousUsersRole) || role.Equals(site.AllUsersRole))
      {
        return true;
      }
    }

    return false;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposedValue)
    {
      if (disposing)
      {
        // dispose managed state (managed objects).  Attachments and content can consume quite a bit of memory, so we 
        // clean them up here.					
        this.Content = null;
      }

      disposedValue = true;
    }
  }

  public void Dispose()
  {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
