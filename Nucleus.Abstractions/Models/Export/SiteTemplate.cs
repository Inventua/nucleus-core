using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.Abstractions.Models.Export
{
	/// <summary>
	/// Site export wrapper class.
	/// </summary>
	[XmlRoot(Namespace = NAMESPACE, ElementName = "site-template") ]	
	public class SiteTemplate
	{
		/// <summary>
		/// xml namespace for the site export file.
		/// </summary>
		/// <remarks>
		/// A site export file can be used as a template for creating a new site.
		/// </remarks>
		public const string NAMESPACE = "urn:nucleus:schemas:xml-site-template/v1";

    /// <summary>
    /// Template friendly name.
    /// </summary>
    [System.Xml.Serialization.XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }

    /// <summary>
    /// Template description.
    /// </summary>
    [System.Xml.Serialization.XmlElement(ElementName = "description")]
    public string Description { get; set; }

		/// <summary>
		/// Site information.
		/// </summary>
		public Models.Site Site { get; set; }

		/// <summary>
		/// Permission types.
		/// </summary>
		/// <remarks>
		/// Permission types are not site-specific, but can be included in a template.
		/// </remarks>
		public List<Models.PermissionType> PermissionTypes { get; set; }

		/// <summary>
		/// Scheduled Tasks.
		/// </summary>
		/// <remarks>
		/// Scheduled tasks types are not site-specific, but can be included in a template.  The import wizard does not check to ensure
		/// that the relevant extension for a scheduled task is installed, so only core scheduled tasks should be included in templates.
		/// </remarks>
		public List<Models.TaskScheduler.ScheduledTask> ScheduledTasks { get; set; }

		/// <summary>
		/// Site Pages, including modules, permissions.
		/// </summary>
		public List<Page> Pages { get; set; }

		/// <summary>
		/// Module content.
		/// </summary>
		public List<Content> Contents { get; set; }

		/// <summary>
		/// Lists and list values
		/// </summary>
		public List<Models.List> Lists { get; set; }

		/// <summary>
		/// Site Role groups
		/// </summary>
		public List<Models.RoleGroup> RoleGroups { get; set; }

		/// <summary>
		/// Site Roles
		/// </summary>
		public List<Models.Role> Roles { get; set; }

		/// <summary>
		/// Mail templates
		/// </summary>
		public List<Models.Mail.MailTemplate> MailTemplates { get; set; }
	}
}
