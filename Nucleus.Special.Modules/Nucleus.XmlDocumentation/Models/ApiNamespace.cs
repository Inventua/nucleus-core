using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models.Serialization;

namespace Nucleus.XmlDocumentation.Models
{
	public class ApiNamespace
	{
		public string Name { get; }
		public MixedContent Summary { get; set; }

		public MixedContent Remarks { get; set; }

		public string[] Examples { get; set; }

		public ApiNamespace(string name)
		{
			this.Name = name;
		}

		public string ControlId ()
		{
			return this.Name.Replace('.', '-');
		}
	}
}
