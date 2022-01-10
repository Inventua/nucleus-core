using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Represents configuration items for a database schema.
	/// </summary>
	public class DatabaseSchema
	{
		/// <summary>
		/// Schema name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Key for a <see cref="DatabaseConnectionOption"/>.
		/// </summary>
		public string ConnectionKey { get; private set; }
		
	}
}
