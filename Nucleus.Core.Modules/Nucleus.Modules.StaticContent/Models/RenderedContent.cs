using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.StaticContent.Models
{
	internal class RenderedContent
	{
		public string Content { get; }

		public RenderedContent(string content)
		{
			this.Content = content;
		}
	}
}
