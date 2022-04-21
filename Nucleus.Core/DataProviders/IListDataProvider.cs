using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="List"/> class.
	internal interface IListDataProvider : IDisposable
	{
		abstract Task<IEnumerable<List>> ListLists(Site site);
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<List>> ListLists(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task<List> GetList(Guid listId);
		abstract Task SaveList(Site site, List list);
		abstract Task DeleteList(List list);

		abstract Task<ListItem> GetListItem(Guid id);
	}
}
