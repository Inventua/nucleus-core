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
	public interface IArticlesDataProvider : IDisposable
	{
		public Task<Article> Get(Guid Id);
		public Task<Guid?> Find(PageModule module, string encodedTitle);
		public Task<IList<Article>> List(PageModule pageModule);
		public Task<PagedResult<Article>> List(PageModule module, PagingSettings settings);
		public Task<PagedResult<Article>> List(PageModule module, PagingSettings settings, FilterOptions filters);

		public Task Save(PageModule pageModule, Article article);
		public Task Delete(Article article);

	}
}
