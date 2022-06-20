using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// An exception which is thrown when Nucleus is unable to connect to a database.
	/// </summary>
	public class ConnectionException : System.Data.Common.DbException
	{
		public System.Data.Common.DbConnection Connection { get; }

		/// <summary>
		/// Initialize a new instance of the ConnectionException class.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="databaseConnectionOption"></param>
		/// <param name="innerException"></param>
		/// <remarks>
		/// The exception's message is derived from the values in the specified database connection option.
		/// </remarks>
		public ConnectionException(System.Data.Common.DbConnection connection, Nucleus.Abstractions.Models.Configuration.DatabaseConnectionOption databaseConnectionOption, Exception innerException)
			: base(GetMessage(databaseConnectionOption), innerException)
		{
			this.Connection = connection;
		}

		private static string GetMessage(Nucleus.Abstractions.Models.Configuration.DatabaseConnectionOption databaseConnectionOption)
		{
			string message = $"An error occurred while connecting to the database (configuration key: '{databaseConnectionOption?.Key}', provider: '{databaseConnectionOption?.Type}').";

			return message;
		}
	}
}
