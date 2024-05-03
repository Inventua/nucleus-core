using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class RoleIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<Role> Roles { get; set; } = new() { PageSize = 20 };

	}
}
