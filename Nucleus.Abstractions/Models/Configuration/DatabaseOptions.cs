using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents database configuration options.
	/// </summary>
	public class DatabaseOptions
	{
		/// <summary>
		/// Configuration file section path for database connections.
		/// </summary>
		public static string Section = "Nucleus:Database";

		/// <summary>
		/// List of configured database connections.
		/// </summary>
		public List<DatabaseConnectionOption> Connections { get; private set; }

		/// <summary>
		/// List of configured schemas.
		/// </summary>
		public List<DatabaseSchema> Schemas { get; private set; }

		/// <summary>
		/// Get the database connection options for the schema specifed by <paramref name="schemaName"/>.  If no matching 
		/// connection is found, return the database connection options for the "*" (default) schema.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public DatabaseConnectionOption GetDatabaseConnection(string schemaName)
		{
			// Get connection for the specified schema name.  
			DatabaseSchema schema = this.Schemas
				.Where(schema => schema.Name == schemaName)
				.FirstOrDefault();

			if (schema == null)
			{
				schema = this.Schemas
					.Where(schema => schema.Name == "*")
					.FirstOrDefault();
			}

			if (schema != null)
			{
				DatabaseConnectionOption connection = this.Connections
					.Where(connection => connection.Key.Equals(schema.ConnectionKey, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				return connection;
			}

			return null;
		}
	}
}
