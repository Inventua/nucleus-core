using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// The ContentMetaData class provides an entry to be submitted to a search index.
	/// </summary>
	public class ContentMetaData
	{
		/// <summary>
		/// This site which the resource belongs to.
		/// </summary>
		public Site Site { get; set; }

		/// <summary>
		/// Url used to access the resource for the resource represented by this ContentMetaData instance.
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
		public string Scope { get; set; }

		/// <summary>
		/// Unique Id for the search entry source.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If set, it can be used to manage the individual search result for update and delete operations.
		/// </remarks>
		public Guid? SourceId { get; set; }

		/// <summary>
		/// Search entry content, used for content indexing.
		/// </summary>
		public byte[] Content { get; set; } = Array.Empty<byte>();

		/// <summary>
		/// Search entry MIME type.
		/// </summary>
		/// <remarks>
		/// This value should be set to the expected MIME type when a user-agent request the <see cref="Url"/> for this search entry.  This 
		/// is not necessarily the MIME type of the Content field.  This value is intended for search result filtering.
		/// </remarks>
		public string ContentType { get; set; }

		/// <summary>
		/// File attachments for the search entry.  
		/// </summary>
		/// <remarks>
		/// This value is optional.  If set, file names and content are added to the search index.  Not all file types can be parsed for content.
		/// </remarks>
		public IEnumerable<File> Attachments { get; set; }

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
		public long? Size { get; set; }

		/// <summary>
		/// A list of keywords for the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If supplied, keywords contribute to the search result weighting.
		/// </remarks>
		public IEnumerable<string> Keywords { get; set; }

		/// <summary>
		/// A list of categories for the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If supplied, categories contribute to the search result weighting, and may also be used to filter results.
		/// </remarks>
		public IEnumerable<ListItem> Categories { get; set; }

		/// <summary>
		/// A list of roles with view access to the resource.
		/// </summary>
		/// <remarks>
		/// This value is optional.  If it not specified, search feeders will try to fill in roles by using the roles for the relevant
		/// page, module or folder.  Roles are used to filter search results to resources which the current user can view.
		/// </remarks>
		public IEnumerable<Role> Roles { get; set; }

		
	}
}
