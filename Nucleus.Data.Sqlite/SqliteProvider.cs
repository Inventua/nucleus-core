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

namespace Nucleus.Data.Sqlite
{
	/// <summary>
	/// 
	/// </summary>
	public class SqliteProvider : IDatabaseProvider
	{
		private const string DATABASE_PROVIDER_TYPE = "Sqlite";
		
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
		/// Add Sqlite data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses Sqlite.  This overload allows callers to specify their schema name instead 
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
			return AddSqlite<TDataProvider>(services, options, schemaName);
		}

		/// <summary>
		/// Add Sqlite data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses Sqlite.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		private IServiceCollection AddSqlite<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			// add Sqlite data provider objects to the services collection.
			
			services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.Sqlite.SqliteDataProviderMigration<TDataProvider>>();
			services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.Sqlite.SqliteDbContextConfigurator<TDataProvider>>();
			return services;			
		}

		/// <summary>
		/// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
		/// the database provider implementing this interface.
		/// </summary>
		public Dictionary<string, string> GetDatabaseInformation(DatabaseConnectionOption options, string schemaName)
		{
			Dictionary<string, string> results = new();

			System.Data.Common.DbConnection connection = new Microsoft.Data.Sqlite.SqliteConnection(options.ConnectionString);
			connection.Open();

			results.Add("Database", connection.Database);
			results.Add("Version", ExecuteScalar(connection, "SELECT sqlite_version()"));			
			results.Add("Size", new System.IO.FileInfo(connection.Database).Length.FormatFileSize());

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


	}
}
