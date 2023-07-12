using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Encapsulates information about the current request.
	/// </summary>
	/// <remarks>
	/// Get a reference to the Context class by using dependency injection.  The class is populated by Nucleus.Core.LayoutPageRoutingMiddleware 
	/// and Nucleus.Core.LayoutModuleRoutingMiddleware and can be used in extensions.
	/// </remarks>
	public class Context
	{
		/// <summary>
		/// The site being accessed.
		/// </summary>
		public Site Site { get; set; }

		/// <summary>
		/// The page being accessed.
		/// </summary>		
		public Page Page { get; set; }

		/// <summary>
		/// The module being accessed.
		/// </summary>
		/// <remarks>
		/// This property is set when rendering a module and when an extension route is used to call a controller action, or to get the output of a 
		/// single module only.  It is NULL otherwise  See Nucleus.Core.LayoutModuleRoutingMiddleware for information on extension routes.
		/// </remarks>
		public PageModule Module { get; set; }

		/// <summary>
		/// Represents parameters which are specified at the end of the local (page route) path.
		/// </summary>
		public LocalPath LocalPath { get; set; } = new("");

    /// <summary>
		/// The page route which was matched to find the page.
		/// </summary>
		public PageRoute MatchedRoute { get; set; }
  }
}
