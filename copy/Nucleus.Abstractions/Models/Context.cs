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
	/// and Nucleus.Core.LayoutModuleRoutingMiddleware
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
		/// This property is set when rendering a module, or when a module route is used to get the output of a single module only.  It 
		/// is NULL otherwise  See Nucleus.Core.LayoutModuleRoutingMiddleware for information on module routes.
		/// </remarks>
		public PageModule Module { get; set; }

		/// <summary>
		/// Parameters at the end of the local path
		/// </summary>
		public string Parameters { get; set; }
	}
}
