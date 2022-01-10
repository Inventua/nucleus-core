using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Sitemap
{
	/// <summary>
	/// Represents an entry in a sitemap XML file, used by search crawlers.
	/// </summary>
	public class SiteMapEntry
	{
		/// <summary>
		/// Url for the entry.
		/// </summary>
		[System.Xml.Serialization.XmlElement(ElementName = "loc")]
		public string Url { get; set; }

		///// <summary>
		///// Last modified date for the entry.
		///// </summary>
		//[System.Xml.Serialization.XmlElement(ElementName = "lastmod")]
		//public DateTime LastModified { get; set; }
	}
}
