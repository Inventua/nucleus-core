using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="Folder"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	internal interface IListDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract List<List> ListLists(Site site);
		abstract List GetList(Guid listId);
		abstract void SaveList(Site site, List list);
		abstract void DeleteList(List list);

		abstract ListItem GetListItem(Guid id);
	}
}
