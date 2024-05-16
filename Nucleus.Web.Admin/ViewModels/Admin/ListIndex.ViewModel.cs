using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class ListIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<List> Lists { get; set; } = new() { PageSize = 20 };

	}
}
