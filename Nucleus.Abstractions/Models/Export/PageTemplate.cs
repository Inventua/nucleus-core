using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.Abstractions.Models.Export
{
	/// <summary>
	/// Page export wrapper class.
	/// </summary>
	[XmlRoot(Namespace = NAMESPACE, ElementName = "page-template") ]	
	public class PageTemplate
	{
		/// <summary>
		/// xml namespace for the site export file.
		/// </summary>
		/// <remarks>
		/// A site export file can be used as a template for creating a new site.
		/// </remarks>
		public const string NAMESPACE = "urn:nucleus:schemas:xml-page-template/v1";

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
		/// Page data, including modules, permissions.
		/// </summary>
		public Page Page { get; set; }

		/// <summary>
		/// Module content.
		/// </summary>
		public List<Content> Contents { get; set; }

	}
}
