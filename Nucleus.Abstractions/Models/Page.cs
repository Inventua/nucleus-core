using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represent a page in a site.
	/// </summary>
	public class Page : ModelBase
	{
		/// <summary>
		/// Page entity Namespace for permissions, other scoped references
		/// </summary>
		public const string URN = "urn:nucleus:entities:page";
		
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Read-only Id of the site which this page belongs to.
		/// </summary>
		[System.Xml.Serialization.XmlIgnore]
		public Guid SiteId { get; private set; }

		/// <summary>
		/// Id of the page's parent in the menu structure, or NULL for pages which reside at the site root.  
		/// </summary>
		public Guid? ParentId { get; set; }

		/// <summary>
		/// Friendly name for the page.
		/// </summary>
		/// <remarks>
		/// The page name is used in the administrative interface, and in menus.
		/// </remarks>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Page Title.
		/// </summary>
		/// <remarks>
		/// The page title is rendered as the document <![CDATA[<title>]]> element.  Some browsers display the document title as the 
		/// browser window's caption, and it is also used by search engines as meta-data for the page.
		/// </remarks>
		public string Title { get; set; }

		/// <summary>
		/// Page description.
		/// </summary>
		/// <remarks>
		/// The page description is rendered as document meta-data, to be used by search engines.  Page descriptions may be used to 
		/// populate a site search, and/or shown on-screen in search results and other cases where an extended description is required.
		/// </remarks>
		public string Description { get; set; }

		/// <summary>
		/// The Id of a PageRoute representing the default route for the page.
		/// </summary>
		/// <remarks>
		/// When the system links to or redirects users to a page, it always uses the default route.
		/// </remarks>
		public Guid? DefaultPageRouteId { get; set; }
		
		/// <summary>
		/// A list of routes for the page.
		/// </summary>
		public List<PageRoute> Routes { get; } = new();

		/// <summary>
		/// Page keywords.
		/// </summary>
		/// <remarks>
		/// Page keywords is rendered as document meta-data, to be used by search engines.  Page keywords may also be used to populate site 
		/// search indexes, and may be shown on-screen in search results or in other cases.
		/// </remarks>
		public string Keywords { get; set; }

		/// <summary>
		/// Flag used to determine whether the page is disabled.
		/// </summary>
		public Boolean Disabled { get; set; } = false;

		/// <summary>
		/// Flag used to determine whether the page is included in menus.
		/// </summary>
		public Boolean ShowInMenu { get; set; } = true;

		/// <summary>
		/// Flag used to render the page in menus without a hyperlink.
		/// </summary>
		/// <remarks>
		/// This flag would be used in cases where you want a page to contain other pages, but which does not contain any content itself.
		/// </remarks>
		public Boolean DisableInMenu { get; set; }

		/// <summary>
		/// Selected Layout for the page, or NULL to use the site default.
		/// </summary>
		public LayoutDefinition LayoutDefinition { get; set; }

		/// <summary>
		/// Gets or sets the default container for the page, or NULL to use the site default.
		/// </summary>
		/// <remarks>
		/// Pages which do not specify a default container use the site's default container.
		/// </remarks>
		public ContainerDefinition DefaultContainerDefinition { get; set; }

		/// <summary>
		/// Page sort order.
		/// </summary>
		/// <remarks>
		/// Page sort order for display in menus.
		/// </remarks>
		public int SortOrder { get; set; }

		/// <summary>
		/// List of modules in a page.
		/// </summary>
		public List<PageModule> Modules { get; set; } = new();

		/// <summary>
		/// List of page permissions.
		/// </summary>
		public List<Permission> Permissions { get; set; } = new();

		/// <summary>
		/// Specifies whether the page is the first in the sort order of its parents children
		/// </summary>
		[System.Xml.Serialization.XmlIgnore]
		public Boolean IsFirst { get; set; } = false;

		/// <summary>
		/// Specifies whether the page is the last in the sort order of its parents children
		/// </summary>
		[System.Xml.Serialization.XmlIgnore]
		public Boolean IsLast { get; set; } = false;
	}
}
