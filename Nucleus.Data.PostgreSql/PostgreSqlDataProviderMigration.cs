using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Nucleus.Data.PostgreSql
{
	/// <summary>
	/// PostgreSql implementation of the data migration provider.
	/// </summary>
	/// <typeparam name="TDataProvider">
	/// Type of the data provider implementation that this class handles migration for.
	/// </typeparam>
	/// <remarks>
	/// Inherits the entity framework implementation and adds a database type so that script resources can be located.
	/// </remarks>
	public class PostgreSqlDataProviderMigration<TDataProvider> : Nucleus.Data.EntityFramework.DataProviderMigration<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="eventDispatcher"></param>
		/// <param name="logger"></param>
		public PostgreSqlDataProviderMigration(TDataProvider provider, Nucleus.Abstractions.EventHandlers.IEventDispatcher eventDispatcher, ILogger<DataProviderMigration<TDataProvider>> logger) : base((provider as Nucleus.Data.EntityFramework.DataProvider)?.Context, new string[] { "Migrations", "SqlServer" }, eventDispatcher, logger) { }

		/// <summary>
		/// Checks whether the specified database object exists.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		override public Boolean DatabaseObjectExists(string name, DatabaseObjectTypes type)
		{
			int result;

			System.Data.Common.DbConnection connection = base.DbContext.Database.GetDbConnection();
			System.Data.Common.DbCommand command = connection.CreateCommand();
			
			switch (type)
			{
				case DatabaseObjectTypes.Table:
					command.CommandText = "SELECT COUNT(*) FROM pg_tables WHERE schemaname=@schemaname AND tablename=@objectName";
					break;

				case DatabaseObjectTypes.Index:
					command.CommandText = "SELECT COUNT(*) FROM pg_indexes WHERE schemaname=@schemaname AND indexname=@objectName";
					break;

				default:
					return false;
			}

			CreateParameter(command, "schemaname", base.DbContext.Database.GetDbConnection().Database);
			CreateParameter(command, "objectName", name);

			if (connection.State == System.Data.ConnectionState.Closed)
			{
				connection.Open();
			}

			//command.Transaction = System.Transactions.Transaction.Current;
			result = Convert.ToInt32(command.ExecuteScalar());

			return result > 0;
		}

		private System.Data.Common.DbParameter CreateParameter(System.Data.Common.DbCommand command, string name, string value)
		{
			System.Data.Common.DbParameter parameter = command.CreateParameter();
			parameter.ParameterName = name;
			parameter.Value = value;
			command.Parameters.Add(parameter);

			return parameter;
		}
	}
}
