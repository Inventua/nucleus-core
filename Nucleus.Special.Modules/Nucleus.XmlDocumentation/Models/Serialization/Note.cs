using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class Note
	{		
		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }

		[XmlText]
		public string Description { get; set; }
	}
}
