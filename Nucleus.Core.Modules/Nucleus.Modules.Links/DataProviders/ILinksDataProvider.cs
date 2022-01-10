using Nucleus.Abstractions.Models;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.DataProviders
{
	public interface ILinksDataProvider : IDisposable
	{
		public Task<Link> Get(Guid Id);
		public Task<List<Link>> List(PageModule pageModule);
		public Task Save(PageModule pageModule, Link links);
		public Task Delete(Link links);

	}
}
