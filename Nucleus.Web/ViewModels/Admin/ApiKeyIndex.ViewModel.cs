using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class ApiKeyIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey> ApiKeys { get; set; } = new() { PageSize = 20 };

	}
}
