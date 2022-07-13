using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models.Serialization;

namespace Nucleus.XmlDocumentation.Models
{
	public class ApiClass
	{
		public string IdString { get; set; }
		public string AssemblyName { get; set; }
		public string Namespace { get; set; }
		public string FullName { get; set; }
		public string Name { get; set; }

		public ApiMember.MemberTypes Type { get; set; }
		public MixedContent Summary { get; set; }

		public MixedContent Remarks { get; set; }

		public string[] Examples { get; set; }

		public List<ApiMember> Constructors { get; } = new();
		public List<ApiMember> Properties { get; } = new();
		public List<ApiMember> Fields { get; } = new();
		public List<ApiMember> Methods { get; } = new();
		public List<ApiMember> Events { get; } = new();
		public SeeAlso[] SeeAlso { get; set; }
		public TypeParam[] TypeParams { get; set; }
		public string ControlId()
		{
			return this.FullName.Replace('.', '-');
		}

		public List<ApiMember> AllMembers
		{
			get
			{
				return this.Constructors.Concat(this.Properties).Concat(this.Events).Concat(this.Fields).Concat(this.Methods).ToList();
			}
		}
	}
}
