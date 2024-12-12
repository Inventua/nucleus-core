using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch;

#nullable enable

/// <summary>
/// Class which represents an index entry ("document") in Azure Search.
/// </summary>
/// <remarks>
/// Most properties are nullable, because the Azure Search index is populated by both the Azure Search indexer, and by the "push" feed. When the
/// indexer creates entries, most of the meta-data properties are null - and the Json serializer can only deserialize null values when the 
/// property is nullable.
/// </remarks>
internal class AzureSearchDocument : IDisposable
{
  private bool disposedValue;

  /// <summary>
  /// Constructor used by deserialization.
  /// </summary>
  public AzureSearchDocument() { }

  /// <summary>
  /// Constructor used when generating a feed.
  /// </summary>
  /// <param name="content"></param>
  public AzureSearchDocument(ContentMetaData content)
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

    this.IndexingDate = DateTime.UtcNow;
  } 

  private static string GenerateId(ContentMetaData content)
  {
    // for Azure Search, we do not use Scope or SourceId in the key, because we want to maintain compatibility with the
    // built-in Azure Search indexers, which encode the Url as base64.
    /// We use a base-64 string for Azure Search (rather than the base 64 encoded SHA384 hash that we use for Elastic Search) to maintain
    /// compatibility with the built-in Azure Search indexers.
    // The documentation states that Azure does "Url-safe" base64 encoding, which is why we use WebEncoders.Base64UrlEncode
    // method rather than Convert.ToBase64String

    string key = !String.IsNullOrEmpty(content.RawUri) ? content.RawUri : content.Url;
    return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(System.Web.HttpUtility.UrlPathEncode(key)));
  }

  /// <summary>
  /// Unique id for the index entry.
  /// </summary>
  [SearchableField(IsKey = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Values.Keyword)]
  public string Id { get; set; } = "";

  /// <summary>
  /// Special field for document chunks (pages). ParentId contains the Id of the document for which this is a "chunk"
  /// </summary>
  [SearchableField(IsFilterable = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Values.Keyword)]
  public string? ParentId { get; set; }

  /// <summary>
  /// Page number for document chunks (pages). 
  /// </summary>
  [SimpleField(IsFilterable = true, IsFacetable = true)]
  public int? ChunkNumber { get; set; }

  /// <summary>
  /// This Id of the site which the resource belongs to.
  /// </summary>
  [SimpleField(IsFilterable = true, IsFacetable = true)]
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
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
  public string? Title { get; set; } = "";

  /// <summary>
  /// This field supports vector search, if populated by Azure search skill sets
  /// </summary>
  [VectorSearchField()] 
  public Single[]? TitleVector { get; set; }
  
  /// <summary>
  /// Short summary for the resource.
  /// </summary>
  /// <remarks>
  /// The summary displayed in search results.  Summary is optional.  If it is not set, the search result will not display
  /// a summary.
  /// </remarks>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
  public string? Summary { get; set; }

  /// <summary>
  /// This field supports vector search, if populated by Azure search skill sets
  /// </summary>
  [VectorSearchField()]
  public Single[]? SummaryVector { get; set; }

  /// <summary>
  /// URN of the entity which was used to create this search entry.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If set, it can be used to allow users to select that only specified result types are included in their
  /// search results.
  /// </remarks>
  [SimpleField(IsFilterable = true, IsFacetable = true)]
  public string? Scope { get; set; } = "";

  /// <summary>
  /// Unique Id for the search entry source.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If set, it can be used to manage the individual search result for update and delete operations.
  /// </remarks>
  [SimpleField(IsFilterable = true)]
  public string? SourceId { get; set; } = "";

  /// <summary>
  /// Language is required for many Azure skill sets.
  /// </summary>
  [SimpleField()]
  public string? Language { get; set; } = "en";

  /// <summary>
  /// Search entry content, used for content indexing.
  /// </summary>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
  public string? Content { get; set; }

  /// <summary>
  /// This field supports vector search, if populated by Azure search skill sets
  /// </summary>
  [VectorSearchField()]
  public Single[]? ContentVector { get; set; }

  /// <summary>
  /// Search entry MIME type.
  /// </summary>
  /// <remarks>
  /// This value should be set to the MIME type of the Content field.  This value is used for search result filtering and also
  /// to tell Azure Search what content type is in the content field.
  /// </remarks>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft, IsFacetable = true, IsFilterable = true)]
  public string? ContentType { get; set; } = "";  // metadata_content_type

  /// <summary>
  /// Search entry display type.
  /// </summary>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft, IsFilterable = true)]
  public string? Type { get; set; } = "";

  /// <summary>
  /// Source entity published date
  /// </summary>
  /// <remarks>
  /// This value is optional.  If specified, it can be displayed in search results.
  /// </remarks>
  [SimpleField(IsFilterable = true, IsSortable = true)]
  public DateTime? PublishedDate { get; set; }


  /// <summary>
  /// Indexed date
  /// </summary>
  /// <remarks>
  /// Records the date that the index entry was created or updated.
  /// </remarks>
  [SimpleField(IsFilterable = true, IsSortable = true)]
  public DateTime? IndexingDate { get; set; }

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
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.Keyword, IsFilterable = true, IsFacetable = true)]
  public List<string> Keywords { get; set; } = [];

  /// <summary>
  /// A list of categories for the resource.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If supplied, categories contribute to the search result weighting, and may also be used to filter results.
  /// </remarks>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.Keyword, IsFilterable = true, IsFacetable = true)]
  public List<string> Categories { get; set; } = [];

  /// <summary>
  /// A list of roles with view access to the resource.
  /// </summary>
  /// <remarks>
  /// This value is optional.  If it not specified, search feeders will try to fill in roles by using the roles for the relevant
  /// page, module or folder.  Roles are used to filter search results to resources which the current user can view.
  /// </remarks>
  [SimpleField(IsFilterable = true, IsFacetable = true)]
  public List<string> Roles { get; set; } = [];

  /// <summary>
  /// Specifies whether the document is visible to anonymous users.
  /// </summary>
  [SimpleField(IsFilterable = true)]
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

  /// <summary>
  /// Return a concatenated list of keywords and categories. This field supports autocomplete on the values of those fields, 
  /// and is required because suggesters can only contain fields that use the default analyzer and the keywords/category fields use the
  /// keyword analyzer because they require an exact match).
  /// </summary>
  [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
  public string AutoCompleteValues
  {
    get
    {
      List<string> values = new();

      if (this.Keywords.Any())
      {
        values.AddRange(this.Keywords);
      }

      if (this.Categories.Any())
      {
        values.AddRange(this.Categories);
      }

      return string.Join(",", values
        .Where(value => !String.IsNullOrEmpty(value))
        .Select(value => value.Trim())
        .Distinct());
    }
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
