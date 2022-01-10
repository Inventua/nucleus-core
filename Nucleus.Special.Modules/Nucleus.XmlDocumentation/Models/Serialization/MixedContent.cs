using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	
	public class MixedContent
	{
		[System.Xml.Serialization.XmlElement("see", typeof(See))]
		[System.Xml.Serialization.XmlElement("seealso", typeof(SeeAlso))]
		[System.Xml.Serialization.XmlElement("paramref", typeof(ParamRef))]
		[System.Xml.Serialization.XmlElement("typeparamref", typeof(TypeParamRef))]
		[System.Xml.Serialization.XmlElement("para", typeof(Paragraph))]
		[System.Xml.Serialization.XmlElement("code", typeof(Code))]
		[System.Xml.Serialization.XmlElement("c", typeof(InlineCode))]
		[System.Xml.Serialization.XmlElement("value", typeof(Value))]
		[System.Xml.Serialization.XmlElement("note", typeof(Note))]
		[System.Xml.Serialization.XmlText(typeof(string))]
		public object[] Items { get; set; } 		
	}
}
