using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a route (path) for a page.
	/// </summary>
	public class PageRoute : ModelBase
	{
		/// <summary>
		/// Represents a page route.
		/// </summary>
		/// <remarks>
		/// Pages may have one or more (or no) routes, and will navigated to if any of their routes are in the request local path.
		/// </remarks>
		public enum PageRouteTypes
		{
			/// <summary>
			/// Page is active and can be navigated to.
			/// </summary>
			Active,
			/// <summary>
			/// Page is a redirect, a 301-Moved permanently response is returned if this route is requested.
			/// </summary>
			PermanentRedirect
		}

		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Page route local path.  
		/// </summary>
		/// <remarks>
		/// Page route paths must start with a / character.
		/// </remarks>
		[Required(ErrorMessage="Page Route path is required.")]
		public string Path { get; set; }

		/// <summary>
		/// Page route type.
		/// </summary>
		/// <remarks>
		/// If page route of type "PermanentRedirect" is visited, the system automatically issues a "301 Moved Permanently" response to 
		/// redirect the browser to the page's default route.  This feature can be useful for maintaining backward compatibility with old 
		/// sites, or in other cases where you want the page to have more than one Url.
		/// </remarks>
		public PageRouteTypes Type { get; set; }

	}
}
