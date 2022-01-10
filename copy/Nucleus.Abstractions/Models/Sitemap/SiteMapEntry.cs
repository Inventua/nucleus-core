using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Sitemap
{

	public class SiteMapEntry
	{
		[System.Xml.Serialization.XmlElement(ElementName = "loc")]
		public string Url { get; set; }

		[System.Xml.Serialization.XmlElement(ElementName = "lastmod")]
		public DateTime LastModified { get; set; }
	}
}
