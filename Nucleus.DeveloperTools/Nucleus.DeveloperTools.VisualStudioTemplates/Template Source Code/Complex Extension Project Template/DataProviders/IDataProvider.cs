using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using $nucleus.extension.namespace$.Models;
using Nucleus.Abstractions.Models;

namespace $nucleus.extension.namespace$.DataProviders
{
	public interface I$nucleus.extension.name$DataProvider : IDisposable
	{
		public Task<$nucleus.extension.model_class_name$> Get(Guid Id);
		public Task<IList<$nucleus.extension.model_class_name$>> List(PageModule pageModule);
		public Task Save(PageModule pageModule, $nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$);
		public Task Delete($nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$);

	}
}
