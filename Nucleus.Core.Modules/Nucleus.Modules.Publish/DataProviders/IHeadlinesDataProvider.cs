using Nucleus.Abstractions.Models;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Modules.Publish.DataProviders
{
	public interface IHeadlinesDataProvider : IDisposable
	{
		public Task<FilterOptions> GetFilterOptions(PageModule pageModule);

		public Task SaveFilterOptions(PageModule pageModule, FilterOptions options);
		
	}
}
