using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="LayoutDefinition"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface ILayoutManager
	{
		/// <summary>
		/// Default layout filename
		/// </summary>
		public const string DEFAULT_LAYOUT = "default.cshtml";

		/// <summary>
		/// Get the specifed <see cref="LayoutDefinition"/>.
		/// </summary>
		/// <returns></returns>
		public Task<LayoutDefinition> Get(Guid id);

		/// <summary>
		/// Returns a list of all installed <see cref="LayoutDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<IList<LayoutDefinition>> List();

		/// <summary>
		/// Scans a layout and returns a list of all panes in the layout.
		/// </summary>
		/// <param name="layout"></param>
		/// <returns></returns>
		/// <remarks>
		/// Panes are detected by scanning for RenderPaneAsync("PaneName") statements in the layout.
		/// </remarks>
		public Task<IEnumerable<string>> ListLayoutPanes(LayoutDefinition layout);

	}
}
