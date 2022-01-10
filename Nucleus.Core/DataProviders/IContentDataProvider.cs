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
	/// Provides create, read, update and delete functionality for the <see cref="Folder"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	internal interface IContentDataProvider : IDisposable //, DataProviderConfiguration<IContentDataProvider>
	{
		abstract Task<List<Content>> ListContent(PageModule pageModule);
		abstract Task<Content> GetContent(Guid id);
		abstract Task SaveContent(PageModule module, Content content);
		abstract Task DeleteContent(Content content);

	}
}
