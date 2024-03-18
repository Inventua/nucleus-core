using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a module on a page.
	/// </summary>
	public class PageModule : ModelBase
	{
		/// <summary>
		/// PageModule entity Namespace for permissions, other scoped references
		/// </summary>
		public const string URN = "urn:nucleus:entities:module";
		
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Read-only Id of the page that this module belongs to.
		/// </summary>
		[System.Xml.Serialization.XmlIgnore]
		public Guid PageId { get; init; }

		/// <summary>
		/// Value indicating which pane of a layout the module is rendered in.
		/// </summary>
		/// <remarks>
		/// Refer to the documentation on Layouts for more information.
		/// </remarks>
		public string Pane { get; set; } = "ContentPane";

		/// <summary>
		/// Module Title.
		/// </summary>
		/// <remarks>
		/// Depending on the container used to render the module, the title may be displayed on-screen.
		/// </remarks>
		public string Title { get; set; }
		
		/// <summary>
		/// Specifies the ModuleDefinition used to generate output.
		/// </summary>
		public ModuleDefinition ModuleDefinition { get; set; }
				
		/// <summary>
		/// Name/Value pairs of settings for the module.
		/// </summary>
		/// <remarks>
		/// The names and available values of module settings are controlled by the developer of the module.
		/// </remarks>
		public List<ModuleSetting> ModuleSettings { get; set; } = new();

		/// <summary>
		/// List of module permissions.
		/// </summary>
		public List<Permission> Permissions { get; set; } = new();

		/// <summary>
		/// Specifies the selected container used to render the module.  May be null to use the site default.
		/// </summary>
		public ContainerDefinition ContainerDefinition { get; set; }

		/// <summary>
		/// Applies additional CSS classes to the module.
		/// </summary>
		public string Style { get; set; }

    /// <summary>
		/// Applies additional CSS classes to the module.  These are "automatic" classes which have been selected from built-in container styles.
		/// </summary>
		public string AutomaticClasses { get; set; }

    /// <summary>
    /// Applies additional CSS styles to the module.  These are "automatic" styles which have been selected from built-in container styles.
    /// </summary>
    /// <remarks>
    /// Styles are used to set css variables when the user has selected a custom value for a container style.
    /// </remarks>
    public string AutomaticStyles { get; set; }

    /// <summary>
    /// Module sort order.
    /// </summary>
    /// <remarks>
    /// Modules are rendered in order, within their specified panes.
    /// </remarks>
    public int SortOrder { get; set; }

		/// <summary>
		/// Inherit page permissions.
		/// </summary>
		/// <remarks>
		/// When set, this disables module permission checking.
		/// 
		/// </remarks>
		public Boolean InheritPagePermissions { get; set; } = true;
	}
}
