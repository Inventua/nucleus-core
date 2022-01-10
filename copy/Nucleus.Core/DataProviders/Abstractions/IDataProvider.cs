using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.DataProviders.Abstractions
{
	public interface IDataProvider
	{
		public string SchemaName { get; }
		public void CheckDatabaseSchema(string schemaName);

	}
}
