using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.XmlDocumentation.ViewModels
{
	public class Section
	{
		public string Caption { get; set; }
		public string CssClass { get; set; }
		public IList<Nucleus.XmlDocumentation.Models.ApiMember> Members { get; set; }
	}
}
