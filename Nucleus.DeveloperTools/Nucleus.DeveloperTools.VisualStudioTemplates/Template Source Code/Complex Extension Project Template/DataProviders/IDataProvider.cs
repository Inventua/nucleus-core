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
		public Task<$nucleus_extension_name$> Get(Guid Id);
		public Task<IList<$nucleus_extension_name$>> List(PageModule pageModule);
		public Task Save(PageModule pageModule, $nucleus_extension_name$ $nucleus_extension_name_lcase$);
		public Task Delete($nucleus_extension_name$ $nucleus_extension_name_lcase$);

	}
}
