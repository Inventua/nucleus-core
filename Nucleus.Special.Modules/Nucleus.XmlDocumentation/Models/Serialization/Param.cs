﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.XmlDocumentation.Models.Serialization
{
	public class Param : MixedContent
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
				
		//public MixedContent Description { get; set; }

		[XmlIgnore]
		public string Type { get; set; }

		[XmlIgnore]
		public Boolean IsRef { get; set; }

		[XmlIgnore]
		public string Url { get; set; }
				
	}
}
