using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.XmlDocumentation.ViewModels
{
	public class MixedContent
	{
		public Page Page { get; set; }
		public Nucleus.XmlDocumentation.Models.Serialization.MixedContent Content { get; set; }

		public MixedContent(Page page, Nucleus.XmlDocumentation.Models.Serialization.MixedContent mixedContent)
		{
			this.Page = page;
			this.Content = mixedContent;
		}
	}
}
