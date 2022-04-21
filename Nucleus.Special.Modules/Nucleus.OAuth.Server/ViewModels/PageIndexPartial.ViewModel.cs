using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.OAuth.Server.ViewModels
{	
	public class PageIndexPartial
	{
		public PageMenu Pages { get; set; } 
		public Page FromPage { get; set; }

	}
}
