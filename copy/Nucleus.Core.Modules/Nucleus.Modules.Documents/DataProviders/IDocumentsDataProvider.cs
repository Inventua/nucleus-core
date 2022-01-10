using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Documents.Models;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Documents.DataProviders
{
	public interface IDocumentsDataProvider : IDisposable, Nucleus.Core.DataProviders.Abstractions.IDataProvider
	{
		public Document Get(Guid Id);
		public IList<Document> List(PageModule pageModule);
		public void Save(PageModule pageModule, Document document);
		public void Delete(Document document);

	}
}
