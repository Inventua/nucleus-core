﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Data.MySql
{

	/// <summary>
	/// 
	/// </summary>
	public class MySqlProvider : IDatabaseProvider
	{
		private const string DATABASE_PROVIDER_TYPE = "MySql";

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
		/// Add MySql data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
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
			return AddMySql<TDataProvider>(services, options, schemaName);
		}

		/// <summary>
		/// Add MySql data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		private IServiceCollection AddMySql<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			// add MySql data provider objects to the services collection.
			services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.MySql.MySqlDataProviderMigration<TDataProvider>>();
			services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.MySql.MySqlDbContextConfigurator<TDataProvider>>();
			return services;		
		}

    /// <summary>
    /// Test the database connection string.
    /// </summary>
    /// <param name="connectionString"></param>
    public void TestConnection(string connectionString)
    {
      System.Data.Common.DbConnection connection = new MySqlConnector.MySqlConnection(connectionString);
      connection.Open();

      System.Data.Common.DbCommand command = connection.CreateCommand();
      command.CommandText = "SELECT 1;";

      command.ExecuteNonQuery();

      connection.Close();
    }

    /// <summary>
    /// Return a list of databases.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public IEnumerable<string> ListDatabases(string connectionString)
    {
      List<string> results = new();

      System.Data.Common.DbConnection connection = new MySqlConnector.MySqlConnection(connectionString);
      connection.Open();

      System.Data.Common.DbCommand command = connection.CreateCommand();
      command.CommandText = "SELECT schema_name AS Name FROM INFORMATION_SCHEMA.schemata WHERE schema_name NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys');";

      System.Data.Common.DbDataReader reader = command.ExecuteReader();

      try
      {
        while (reader.Read())
        {
          string? name = reader.GetValue(reader.GetOrdinal("Name")).ToString();
          if (name != null)
          {
            results.Add(name);
          }
        }
      }
      finally
      {
        reader.Close();
      }

      connection.Close();

      return results;
    }

    /// <summary>
    /// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
    /// the database provider implementing this interface.
    /// </summary>
    public Dictionary<string, string> GetDatabaseInformation(DatabaseConnectionOption options, string schemaName)
		{
			Dictionary<string, string> results = new();

			System.Data.Common.DbConnection connection = new MySqlConnector.MySqlConnection(options.ConnectionString);
			connection.Open();

			results.Add("Server", ExecuteScalar(connection, "SELECT @@hostname;"));
			results.Add("Database", connection.Database);
			results.Add("Version", ExecuteScalar(connection, "SELECT VERSION();"));

      if (Convert.ToInt32(ExecuteScalar(connection, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='Schema'")) > 0)
      {
        string schemaVersion = ExecuteScalar(connection, $"SELECT `SchemaVersion` FROM `Schema` WHERE `SchemaName`=@schemaName;", new System.Data.Common.DbParameter[] { new MySqlConnector.MySqlParameter("@schemaName", schemaName) });
        if (String.IsNullOrEmpty(schemaVersion) && schemaName == "*")
        {
          schemaVersion = ExecuteScalar(connection, $"SELECT `SchemaVersion` FROM `Schema` WHERE `SchemaName`=@schemaName;", new System.Data.Common.DbParameter[] { new MySqlConnector.MySqlParameter("@schemaName", "Nucleus.Core") });
        }
        results.Add("Schema Version", schemaVersion);
      }

      results.Add("Size", 
				ExecuteScalar(connection, "SELECT ROUND(SUM(data_length + index_length) / 1024 / 1024, 1) AS 'database_size' FROM information_schema.tables WHERE table_schema = @dbname;", 
				new System.Data.Common.DbParameter[] { new MySqlConnector.MySqlParameter("@dbname", connection.Database) }).ToString() + "MB");

#pragma warning disable CS8604 // Possible null reference argument.
			results.Add("Software", ExecuteReader(connection, "SHOW VARIABLES where variable_name='version_comment';", "Value").ToString());
#pragma warning restore CS8604 // Possible null reference argument.

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
