using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class Returns : MixedContent
	{
		[System.Xml.Serialization.XmlAttribute("type")]
		public string Type { get; set; }
	}
}
