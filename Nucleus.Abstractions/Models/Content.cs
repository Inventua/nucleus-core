using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a general-use list of items.  
	/// </summary> 
	/// <remarks>
	/// </remarks>
	public class Content : ModelBase
	{
	
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Module instance which the content belongs to.
		/// </summary>
		public Guid PageModuleId { get; set; }

		/// <summary>
		/// Content sub-title
		/// </summary>
		/// <remarks>
		/// This value is used by the multi-content module and may be used by other modules.  The value is optional.
		/// </remarks>		
		public string Title { get; set; }

		/// <summary>
		/// Content value.
		/// </summary>
		/// <remarks>
		/// This value is typically in HTML format, but can be in any other format that may be required.  Use the ContentType property
		/// to specify the format of the value property.
		/// </remarks>		
		public string Value { get; set; }

		/// <summary>
		/// MIME type of the content value.
		/// </summary>
		/// <remarks>
		/// </remarks>		
		public string ContentType { get; set; } = "text/html";

		/// <summary>
		/// Content sort order.
		/// </summary>
		/// <remarks>
		/// Some modules can have multiple content items, which are rendered in order.
		/// </remarks>
		public int SortOrder { get; set; }

	}
}
