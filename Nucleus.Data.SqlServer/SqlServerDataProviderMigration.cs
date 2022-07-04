using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Nucleus.Data.SqlServer
{
	/// <summary>
	/// Sql server implementation of the data migration provider.
	/// </summary>
	/// <typeparam name="TDataProvider">
	/// Type of the data provider implementation that this class handles migration for.
	/// </typeparam>
	/// <remarks>
	/// Inherits the entity framework implementation and adds a database type so that script resources can be located.
	/// </remarks>
	public class SqlServerDataProviderMigration<TDataProvider> : Nucleus.Data.EntityFramework.DataProviderMigration<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="eventDispatcher"></param>
		/// <param name="logger"></param>
		public SqlServerDataProviderMigration(TDataProvider provider, Nucleus.Abstractions.EventHandlers.IEventDispatcher eventDispatcher, ILogger<DataProviderMigration<TDataProvider>> logger) : base((provider as Nucleus.Data.EntityFramework.DataProvider).Context, new string[] { "Migrations", "SqlServer" }, eventDispatcher, logger) { }

		/// <summary>
		/// Checks whether the specified database object exists.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		override public Boolean DatabaseObjectExists(string name, DatabaseObjectTypes type)
		{
			int result;

			System.Data.Common.DbConnection connection = base.DbContext.Database.GetDbConnection();// new Microsoft.Data.SqlClient.SqlConnection(base.DbContext.Database.GetConnectionString());
			System.Data.Common.DbCommand command = connection.CreateCommand();
			System.Data.Common.DbParameter parameter = command.CreateParameter();

			switch (type)
			{
				case DatabaseObjectTypes.Table:
					command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name=@objectName";
					break;

				case DatabaseObjectTypes.Index:
					command.CommandText = "SELECT COUNT(*) FROM sys.indexes WHERE name=@objectName";
					break;

				default:
					return false;
			}

			parameter.ParameterName = "objectName";
			parameter.Value = name;
			command.Parameters.Add(parameter);

			if (connection.State == System.Data.ConnectionState.Closed)
			{
				connection.Open();
			}

			// Enlist in the current transaction.  
			if (base.DbContext.Database.CurrentTransaction != null)
			{
				command.Transaction = base.DbContext.Database.CurrentTransaction.GetDbTransaction();
			}

			result = Convert.ToInt32(command.ExecuteScalar());

			return result > 0;
		}

	}
}
