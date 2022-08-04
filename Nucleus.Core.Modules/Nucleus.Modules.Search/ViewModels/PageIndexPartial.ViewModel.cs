using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Search.ViewModels
{	
	public class PageIndexPartial
	{
		public PageMenu Pages { get; set; } 
		public Page FromPage { get; set; }

	}
}
