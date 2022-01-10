using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	public class PageMenu
	{
		public Page Page { get; }
		public IEnumerable<PageMenu> Children { get; }

		public PageMenu(Page page, IEnumerable<PageMenu> children)
		{
			this.Page = page;
			this.Children = children;
		}

		public Boolean IsTopLevel()
		{
			return this.Page == null;
		}

	}
}
