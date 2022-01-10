using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using $nucleus_extension_namespace$.Models;
using Nucleus.Abstractions.Models;

namespace $nucleus_extension_namespace$.DataProviders
{
	public interface I$nucleus_extension_name$DataProvider : IDisposable, Nucleus.Core.DataProviders.Abstractions.IDataProvider
	{
		public $nucleus_extension_name$ Get(Guid Id);
		public IList<$nucleus_extension_name$> List(PageModule pageModule);
		public void Save(PageModule pageModule, $nucleus_extension_name$ $nucleus_extension_name_lcase$);
		public void Delete($nucleus_extension_name$ $nucleus_extension_name_lcase$);

	}
}
