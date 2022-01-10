using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a page and its children, used by menu controls.
	/// </summary>
	public class PageMenu
	{
		/// <summary>
		/// Reference to the page.
		/// </summary>
		public Page Page { get; }
		
		/// <summary>
		/// List of the page's children.
		/// </summary>
		public IEnumerable<PageMenu> Children { get; }

		/// <summary>
		/// Flag indicating whether the page represented by this PageMenu has child pages.
		/// </summary>
		public Boolean HasChildren { get; set; }

		/// <summary>
		/// Create a PageMenu instance
		/// </summary>
		/// <param name="page"></param>
		/// <param name="children"></param>
		/// <param name="hasChildren"></param>
		public PageMenu(Page page, IEnumerable<PageMenu> children, Boolean hasChildren)
		{
			this.Page = page;
			this.Children = children;
			this.HasChildren = hasChildren;
		}

		/// <summary>
		/// Gets whether this instance represents the "top level".
		/// </summary>
		/// <returns>True, if this instance represents the top level, or false otherwise.</returns>
		public Boolean IsTopLevel()
		{
			return this.Page == null;
		}

	}
}
