using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models.Serialization;

namespace Nucleus.XmlDocumentation.Models
{
	public class ApiDocument
	{
		public const string URN = "urn:nucleus:entities:apidocument";

		public string AssemblyName { get; set; }
		public ApiNamespace Namespace { get; set; }
		public string SourceFileName { get; set; }
		public List<ApiClass> Classes { get; set; }
		public DateTime LastModifiedDate { get; set; }

	}
}
