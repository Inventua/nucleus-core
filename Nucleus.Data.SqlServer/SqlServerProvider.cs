using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Data.Common;

namespace Nucleus.Data.SqlServer
{
	/// <summary>
	/// 
	/// </summary>
	public class SqlServerProvider : IDatabaseProvider
	{
		private const string DATABASE_PROVIDER_TYPE = "SqlServer";

		/// <summary>
		/// Database provider type key.
		/// </summary>
		/// <remarks>
		/// This value is used to represent the database provider in the database configuration file 'Type' property.
		/// </remarks>
		/// <returns></returns>
		public string TypeKey()
		{
			return DATABASE_PROVIDER_TYPE;
		}

		/// <summary>
		/// Add SqlServer data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public IServiceCollection AddDataProvider<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			return AddSqlServer<TDataProvider>(services, options, schemaName);
		}

		/// <summary>
		/// Add SqlServer data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		static private IServiceCollection AddSqlServer<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.SqlServer.SqlServerDataProviderMigration<TDataProvider>>();
			services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.SqlServer.SqlServerDbContextConfigurator<TDataProvider>>();
			return services;
		}


		/// <summary>
		/// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
		/// the database provider implementing this interface.
		/// </summary>
		public Dictionary<string, string> GetDatabaseInformation(DatabaseConnectionOption options, string schemaName)
		{
			Dictionary<string, string> results = new();

			System.Data.Common.DbConnection connection = new Microsoft.Data.SqlClient.SqlConnection(options.ConnectionString);
			connection.Open();

			results.Add("Server", ExecuteScalar(connection, "SELECT SERVERPROPERTY('MachineName')"));
			results.Add("Database", connection.Database);
			results.Add("Version", $"{ExecuteScalar(connection, "SELECT SERVERPROPERTY('ProductVersion')")} [{ExecuteScalar(connection, "SELECT SERVERPROPERTY('ProductUpdateReference')")}]");
			results.Add("Edition", ExecuteScalar(connection, "SELECT SERVERPROPERTY('Edition')"));

			results.Add("Transport Protocol", ExecuteScalar(connection, "SELECT CONNECTIONPROPERTY('net_transport')"));

			results.Add("Size", ExecuteReader(connection, "sp_spaceused", "database_size").ToString());

			results.Add("Software", ExecuteScalar(connection, "SELECT @@VERSION"));

			connection.Close();

			return results;
		}

		private string ExecuteScalar(System.Data.Common.DbConnection connection, string sql)
		{
			string result;

			System.Data.Common.DbCommand command = connection.CreateCommand();

			command.CommandText = sql;
			result = Convert.ToString(command.ExecuteScalar());

			return result;
		}

		private object ExecuteReader(System.Data.Common.DbConnection connection, string sql, string columnName)
		{
			System.Data.Common.DbCommand command = connection.CreateCommand();

			command.CommandText = sql;
			System.Data.Common.DbDataReader reader = command.ExecuteReader();

			try
			{
				if (reader.Read())
				{
					return reader.GetValue(reader.GetOrdinal(columnName));
				}
				else
				{
					return "";
				}
			}
			finally
			{
				reader.Close();
			}
		}

	}
}
