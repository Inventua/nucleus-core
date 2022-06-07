using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class RoleEditor
	{
		public Site Site { get; set; }	
		public Role Role { get; set; }
		public IEnumerable<RoleGroup> RoleGroups { get; set; } = new List<RoleGroup>();
		public Boolean IsAutoRole { get; set; }
		public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.User> Users { get; set; } = new() 
		{ 
			PageSize = 20, 
			PageSizes = new List<int> { 20, 50, 100, 250 } 
		};

	}
}
