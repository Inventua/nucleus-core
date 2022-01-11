using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Specifies configuration file options for a data provider.
	/// </summary>
	public class DatabaseConnectionOption
	{
		/// <summary>
		/// Unique identifier for the data provider.
		/// </summary>
		/// <remarks>
		/// This value is matched with the <seealso cref="DatabaseSchema.ConnectionKey"/> to create an instance of the correct data provider for a schema.
		/// </remarks>
		public string Key { get; private set; }

		/// <summary>
		/// Database type.
		/// </summary>
		/// <remarks>
		/// This value must match a value specified by the database provider.  The core database provider types are Sqlite and SqlServer.
		/// </remarks>
		public string Type { get; private set; }

		/// <summary>
		/// The connection string provides parameters needed to establish a connection to the database.  The format and contents
		/// of a connection string are specific to each database provider type.
		/// </summary>
		public string ConnectionString { get; private set; }
		
	}
}
