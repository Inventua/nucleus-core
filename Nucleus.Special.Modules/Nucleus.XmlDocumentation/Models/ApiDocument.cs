using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.XmlDocumentation.Models
{
	public class ApiDocument
	{
		public const string URN = "urn:nucleus:entities:apidocument";

		public string AssemblyName { get; set; }
		public ApiNamespace Namespace { get; set; }
		public File SourceFile { get; set; }
		public List<ApiClass> Classes { get; set; }
		public DateTime LastModifiedDate { get; set; }

	}
}
