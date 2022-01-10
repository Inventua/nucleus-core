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
					command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name=@objectName";
					break;

				case DatabaseObjectTypes.Index:
					command.CommandText = "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema=DATABASE() AND index_name=@objectName";
					break;

				default:
					return false;
			}

			System.Data.Common.DbParameter parameter = command.CreateParameter();
			parameter.ParameterName = "objectName";
			parameter.Value = name;
			command.Parameters.Add(parameter);

			if (connection.State == System.Data.ConnectionState.Closed)
			{
				connection.Open();
			}

			//command.Transaction = System.Transactions.Transaction.Current;
			result = Convert.ToInt32(command.ExecuteScalar());

			return result > 0;
		}

	}
}
