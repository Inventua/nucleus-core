using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.XmlDocumentation.ViewModels
{
	public class ApiClass
	{
		public Page Page { get; set; }
		public Nucleus.XmlDocumentation.Models.ApiClass Content { get; set; }

		public ApiClass(Page page, Nucleus.XmlDocumentation.Models.ApiClass apiClass)
		{
			this.Page = page;
			this.Content = apiClass;
		}
	}
}
