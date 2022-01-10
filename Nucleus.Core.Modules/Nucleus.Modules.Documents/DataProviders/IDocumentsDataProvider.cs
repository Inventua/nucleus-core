using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Documents.Models;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Documents.DataProviders
{
	public interface IDocumentsDataProvider : IDisposable//, Nucleus.Data.Common.IDataProvider
	{
		public Task<Document> Get(Guid Id);
		public Task<IList<Document>> List(PageModule pageModule);
		public Task Save(PageModule pageModule, Document document);
		public Task Delete(Document document);

	}
}
