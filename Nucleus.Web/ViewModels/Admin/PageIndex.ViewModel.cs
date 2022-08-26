using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class PageIndex
	{
		
		public PageMenu Pages { get; set; } 
		public Guid PageId { get; set; }

		public Boolean OpenPage { get; set; }

		public string SearchTerm { get; set; }
		public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.Page> SearchResults { get; set; } = new();
	}
}
