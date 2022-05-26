using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using $nucleus_extension_namespace$.Models;
using Nucleus.Abstractions.Models;

namespace $nucleus_extension_namespace$.DataProviders
{
	public interface I$nucleus_extension_name$DataProvider : IDisposable
	{
		public Task<$nucleus_extension_modelname$> Get(Guid Id);
		public Task<IList<$nucleus_extension_modelname$>> List(PageModule pageModule);
		public Task Save(PageModule pageModule, $nucleus_extension_modelname$ $nucleus_extension_modelname_lcase$);
		public Task Delete($nucleus_extension_modelname$ $nucleus_extension_modelname_lcase$);

	}
}
