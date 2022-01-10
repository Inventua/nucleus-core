using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	[XmlRoot(ElementName = "doc")]
	public class Documentation
	{
		[XmlElement(ElementName = "assembly")]
		public Assembly Assembly { get; set; }


		[XmlArray(ElementName = "members")]
		[XmlArrayItem(ElementName = "member")]
		public Member[] Members { get; set; }
	}
}
