using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Nucleus.Data.Sqlite
{
	/// <summary>
	/// Sqlite implementation of the data migration provider.
	/// </summary>
	/// <typeparam name="TDataProvider">
	/// Type of the data provider implementation that this class handles migration for.
	/// </typeparam>
	/// <remarks>
	/// Inherits the entity framework implementation and adds a database type so that script resources can be located.
	/// </remarks>
	public class SqliteDataProviderMigration<TDataProvider> : Nucleus.Data.EntityFramework.DataProviderMigration<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="eventDispatcher"></param>
		/// <param name="logger"></param>
		public SqliteDataProviderMigration(TDataProvider provider, Nucleus.Abstractions.EventHandlers.IEventDispatcher eventDispatcher, ILogger<DataProviderMigration<TDataProvider>> logger) : base((provider as Nucleus.Data.EntityFramework.DataProvider).Context, new string[] { "Migrations", "Sqlite" }, eventDispatcher, logger) {	}

		/// <summary>
		/// Checks whether the specified database object exists.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		override public Boolean DatabaseObjectExists(string name, DatabaseObjectTypes type)
		{
			int result;

			System.Data.Common.DbConnection connection = base.DbContext.Database.GetDbConnection();//new Microsoft.Data.Sqlite.SqliteConnection(base.DbContext.Database.GetConnectionString());
			System.Data.Common.DbCommand command = connection.CreateCommand();
			System.Data.Common.DbParameter parameter = command.CreateParameter();

			switch (type)
			{
				case DatabaseObjectTypes.Table:
					command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName";
					break;

				case DatabaseObjectTypes.Index:
					command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name=@tableName";
					break;

				default:
					return false;
			}

			parameter.ParameterName = "tableName";
			parameter.Value = name;
			command.Parameters.Add(parameter);
			
			if (connection.State == System.Data.ConnectionState.Closed)
			{
				connection.Open();
			}

			result = Convert.ToInt32(command.ExecuteScalar());
			
			return result > 0;
		}

	}
}
