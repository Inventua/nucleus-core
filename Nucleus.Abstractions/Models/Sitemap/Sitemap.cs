using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Sitemap
{
	/// <summary>
	/// Class used to generate an XML site map.
	/// </summary>
	[System.Xml.Serialization.XmlRoot(ElementName = "urlset", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
	public class Sitemap
	{
		/// <summary>
		/// List of <seealso cref="SiteMapEntry"/> objects, used to generate an XML site map.
		/// </summary>
		[System.Xml.Serialization.XmlElementAttribute("url", typeof(SiteMapEntry))]
		public List<SiteMapEntry> Items { get; set; } = new();
	}
}
