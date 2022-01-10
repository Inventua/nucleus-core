using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class PageIndex
	{
		public IEnumerable<Page> SearchResults { get; set; }
		public PageMenu Pages { get; set; } 
		public Guid PageId { get; set; }

		public string SearchTerm { get; set; }
	}
}
