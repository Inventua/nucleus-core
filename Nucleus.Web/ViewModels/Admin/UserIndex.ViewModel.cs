using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{
	public class UserIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.User> Users { get; set; } = new() { PageSize = 20 };
		public string SearchTerm { get; set; }

		public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.User> SearchResults { get; set; } = new();
		

	}
}
