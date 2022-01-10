using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class Exception
	{
		[XmlAttribute(AttributeName = "cref")]
		public string CodeReference { get; set; }

		[XmlText]
		public string Description { get; set; }

		[XmlIgnore]
		public System.Uri Uri { get; set; }
	}
}
