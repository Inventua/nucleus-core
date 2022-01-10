using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class See
	{
		[XmlAttribute(AttributeName = "cref")]
		public string CodeReference { get; set; }

		[XmlAttribute(AttributeName = "href")]
		public string Href { get; set; }

		[XmlAttribute(AttributeName = "langword")]
		public string Keyword { get; set; }

		[XmlText]
		public string LinkText { get; set; }

		[XmlIgnore]
		public System.Uri Uri { get; set; }
		
	}
}
