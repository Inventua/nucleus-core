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
	public interface IArticleDataProvider : IDisposable, Nucleus.Core.DataProviders.Abstractions.IDataProvider
	{
		public Article Get(Guid Id);
		public Article Find(PageModule module, string encodedTitle);
		public IList<Article> List(PageModule pageModule);
		public PagedResult<Article> List(PageModule module, PagingSettings settings);

		public void Save(PageModule pageModule, Article article);
		public void Delete(Article article);

	}
}
