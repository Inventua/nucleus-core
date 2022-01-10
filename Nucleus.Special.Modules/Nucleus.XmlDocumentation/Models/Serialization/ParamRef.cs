using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class ParamRef
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

	}
}
