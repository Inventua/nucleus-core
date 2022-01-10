using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class Member
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		[XmlElement(ElementName = "summary")]
		public MixedContent Summary { get; set; }

		[XmlElement(ElementName = "returns")]
		public MixedContent Returns { get; set; }

		[XmlElement(ElementName = "remarks")]
		public MixedContent Remarks { get; set; }

		[XmlElement(ElementName = "example")]
		public string[] Examples { get; set; }

		[XmlElement(ElementName = "param")]
		public Param[] Params { get; set; }

		[XmlElement(ElementName = "value")]
		public Value[] Values { get; set; }

		[XmlElement(ElementName = "exception")]
		public Exception[] Exceptions { get; set; }

		[XmlElement(ElementName = "event")]
		public Event[] Events { get; set; }
				
		[XmlElement(ElementName = "seealso")]
		public SeeAlso[] SeeAlso { get; set; }

		[XmlElement(ElementName = "typeparam")]
		public TypeParam[] TypeParams { get; set; }

	}
}
