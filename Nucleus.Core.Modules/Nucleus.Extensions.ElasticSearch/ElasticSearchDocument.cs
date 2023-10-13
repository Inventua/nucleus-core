using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.ElasticSearch
{
	[Nest.ElasticsearchType(IdProperty = nameof(Id))]
	public class ElasticSearchDocument : IDisposable
	{
		private bool disposedValue;

		/// <summary>
		/// Constructor used by NEST deserialization for search results.
		/// </summary>
		public ElasticSearchDocument() { }	

		/// <summary>
		/// Constructor used when generating a feed.
		/// </summary>
		/// <param name="content"></param>
		public ElasticSearchDocument(ContentMetaData content, ConfigSettings settings)
		{
			this.Id = GenerateId(content);

			this.SiteId = content.Site.Id;
			this.Url = content.Url;
			this.Title = content.Title;
			this.Summary = content.Summary;

			this.Scope = content.Scope;
			this.SourceId = content.SourceId;

			this.ContentType = content.ContentType;
      this.Type = content.Type;

			if (content.Content.Any() && content.Content.Length <= settings.AttachmentMaxSize)
			{
				this.Content = Convert.ToBase64String(content.Content);
			}
			else
			{
				this.Content = string.Empty;
			}

			if (content.PublishedDate.HasValue)
			{
				this.PublishedDate = content.PublishedDate.Value.Date;
			}

			this.Size = content.Size;
			this.Keywords = content.Keywords?.ToList();

			if (content.Categories == null)
			{
				this.Categories = new List<Guid>();
			}
			else
			{
				this.Categories = content.Categories.Select(category => category.Id).ToList();
			}

			if (content.Roles == null)
			{
				this.Roles = new List<Guid>();
			}
			else
			{
				this.Roles = content.Roles?.Select(role => role.Id).ToList();
			}

			this.IsSecure = !this.IsPublic(content.Site, content.Roles);
		}

		public string GenerateId(ContentMetaData content)
		{
			if (content.SourceId.HasValue)
			{
				return Encode($"{content.Scope}/{content.Url}/{content.SourceId}");
			}
			else
			{
				return Encode($"{content.Scope}/{content.Url}/");
			}
		}

		/// <summary>
		/// Encode the url as a base 64 encoded SHA384 hash.  
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is used to generate a key for Elastic search.  Elastic search supports keys up to 512 bytes.  Base 64 string 
		/// lengths (where n=original length) are 4*(n/3), so an input value of 512 bits = 64 bytes/3*4 = 85.3 (84) bytes + 4 bytes padding, 
		/// which fits easily.
		/// </remarks>
		private string Encode(string value)
		{
			System.Security.Cryptography.SHA512 sha = System.Security.Cryptography.SHA512.Create();

			return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value)));
		}

		/// <summary>
		/// Elastic search unique id for the document
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// This Id of the site which the resource belongs to.
		/// </summary>
		[Nest.Keyword]
		public Guid SiteId { get; set; }

		/// <summary>
		/// Url used to access the resource for this search item.
		/// </summary>
		/// <remarks>
		/// This value is required.
		/// </remarks>
		public string Url { get; set; }

		/// <summary>
		/// Title for the resource.
		/// </summary>
		/// <remarks>
		/// The title displayed in search results.  Title is optional, but highly recommended.  If the title is not set, the search result
		/// will display the Url in place of a title.
		/// </remarks>
		public string Title { get; set; }

		/// <summary>
		/// Short summary for the resource.
		/// </summary>
		/// <remarks>
		/// The summary displayed in search results.  Summary is optional.  If it is not set, the search result will not display
		/// a summary.
		/// </remarks>
		public string Summary { get; set; }

		/// <summary>
		/// URN of the entity which was used to create this search entry.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If set, it can be used to allow users to select that only specified result types are included in their
		/// search results.
		/// </remarks>
		[Nest.Keyword]
		public string Scope { get; set; }

		/// <summary>
		/// Unique Id for the search entry source.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If set, it can be used to manage the individual search result for update and delete operations.
		/// </remarks>
		[Nest.Keyword]
		public Guid? SourceId { get; set; }

		/// <summary>
		/// Search entry content, used for content indexing.
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// Search entry MIME type.
		/// </summary>
		/// <remarks>
		/// This value should be set to the expected MIME type when a user-agent requests the <see cref="Url"/> for this search entry.  This 
		/// is not necessarily the MIME type of the Content field.  This value is intended for search result filtering.
		/// </remarks>
		[Nest.Keyword]
		public string ContentType { get; set; }

    /// <summary>
		/// Search entry display type.
		/// </summary>
		[Nest.Keyword]
    public string Type { get; set; }

    /// <summary>
    /// Source entity published date
    /// </summary>
    /// <remarks>
    /// This value is optional.  If specified, it can be displayed in search results.
    /// </remarks>
    [Nest.Keyword]
		public DateTime? PublishedDate { get; set; }

		/// <summary>
		/// The size of the resource in bytes.
		/// </summary>
		/// <remarks>
		/// This value is optional and should only be supplied if it is relevant to the resource.  If specified, it can be displayed in search results.
		/// </remarks>
		public long? Size { get; set; }

		/// <summary>
		/// A list of keywords for the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If supplied, keywords contribute to the search result weighting.
		/// </remarks>
		[Nest.Keyword]
		public List<string> Keywords { get; set; }

		/// <summary>
		/// A list of categories for the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If supplied, categories contribute to the search result weighting, and may also be used to filter results.
		/// </remarks>
		[Nest.Keyword]
		public List<Guid> Categories { get; set; }

		/// <summary>
		/// A list of roles with view access to the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If it not specified, search feeders will try to fill in roles by using the roles for the relevant
		/// page, module or folder.  Roles are used to filter search results to resources which the current user can view.
		/// </remarks>
		[Nest.Keyword]
		public List<Guid> Roles { get; set; }

		/// <summary>
		/// Specifies whether the document is visible to anonymous users.
		/// </summary>
		public Boolean IsSecure { get; set; }

		/// <summary>
    /// The date/time that the item was processed by elastic search.
    /// </summary>
    /// <remarks>
    /// This is auto-populated by the ingest pipeline
    /// </remarks>
		public string FeedProcessingDateTime { get; set; }


    /// <summary>
    /// Search result score.
    /// </summary>
    /// <remarks>
    /// This is populated for returned search results and can be used to sort results by relevance.
    /// </remarks>
    public double? Score { get; set; }

		// This is populated in the elastic search database by the attachment pipeline.  It is never populated by search results, but is
		// used to represent the field in Elastic search (to include it for search queries)
		public Nest.Attachment Attachment { get; set; }

		/// <summary>
		/// Content ingest status or error message.
		/// </summary>
		/// <remarks>
		/// This value is generated by the attachment pipeline.
		/// </remarks>
		public string Status { get; set; }

		/// <summary>
		/// Completion suggester title.
		/// </summary>
		/// <remarks>
		/// This field is used by the completion suggester.  It is auto-populated by the ingest pipeline.  This is required so
		/// that we can specify [Nest.Completion] in order to make Elastic Search index this value with an indexing type which
		/// is optimized for speed.
		/// </remarks>
		[Nest.Completion()]
		public string SuggesterTitle { get; set; } = "empty";

		private bool IsPublic(Site site, IEnumerable<Role> roles)
		{
			if (roles?.Any() == false)
			{
				return true;
			}
			else
			{
				foreach (Role role in roles)
				{
					if (role == site.AnonymousUsersRole || role == site.AllUsersRole)
					{
						return true;
					}
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
					this.Attachment = null;
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
}
