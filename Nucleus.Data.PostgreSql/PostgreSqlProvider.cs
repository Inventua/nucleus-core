using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions;
using Nucleus.Data.Common;

namespace Nucleus.Data.PostgreSql
{
	/// <summary>
	/// 
	/// </summary>
	public class PostgreSqlProvider : IDatabaseProvider
	{
		private const string DATABASE_PROVIDER_TYPE = "PostgreSql";

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
			return AddPostgreSql<TDataProvider>(services, options, schemaName);
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
		static private IServiceCollection AddPostgreSql<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{			
			services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.PostgreSql.PostgreSqlDataProviderMigration<TDataProvider>>();
			services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.PostgreSql.PostgreSqlDbContextConfigurator<TDataProvider>>();
			return services;
		}

		/// <summary>
		/// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
		/// the database provider implementing this interface.
		/// </summary>
		public Dictionary<string, string> GetDatabaseInformation(DatabaseConnectionOption options, string schemaName)
		{
			Dictionary<string, string> results = new();

			System.Data.Common.DbConnection connection = new Npgsql.NpgsqlConnection(options.ConnectionString);
			connection.Open();

			results.Add("Database", connection.Database);
			results.Add("Version", ExecuteScalar(connection, "SELECT server_version()"));
			results.Add("Size", ExecuteScalar(connection, "SELECT pg_database_size('@dbname')", new System.Data.Common.DbParameter[] { new Npgsql.NpgsqlParameter("@dbname", connection.Database) }));
			results.Add("Software", ExecuteScalar(connection, "SELECT version()"));

			connection.Close();

			return results;
		}

		private string ExecuteScalar(System.Data.Common.DbConnection connection, string sql)
		{
			return ExecuteScalar(connection, sql, new System.Data.Common.DbParameter[] { });
		}

		private string ExecuteScalar(System.Data.Common.DbConnection connection, string sql, System.Data.Common.DbParameter[] parameters)
		{
			object? result;

			System.Data.Common.DbCommand command = connection.CreateCommand();

			command.CommandText = sql;
			command.Parameters.AddRange(parameters);

			result = command.ExecuteScalar();

#pragma warning disable CS8603 // Possible null reference return.
			return result == null ? "" : result.ToString();
#pragma warning restore CS8603 // Possible null reference return.
		}



	}
}
