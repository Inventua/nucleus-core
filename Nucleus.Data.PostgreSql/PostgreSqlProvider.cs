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
    /// Test the database connection string.
    /// </summary>
    /// <param name="connectionString"></param>
    public void TestConnection(string connectionString)
    {
      System.Data.Common.DbConnection connection = new Npgsql.NpgsqlConnection(connectionString);
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

      System.Data.Common.DbConnection connection = new Npgsql.NpgsqlConnection(connectionString);
      connection.Open();

      System.Data.Common.DbCommand command = connection.CreateCommand();
      command.CommandText = "SELECT datname AS Name FROM pg_database WHERE datName NOT IN ('postgres', 'template0', 'template1')";

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

			System.Data.Common.DbConnection connection = new Npgsql.NpgsqlConnection(options.ConnectionString);
			connection.Open();

			results.Add("Database", connection.Database);
			results.Add("Version", ExecuteScalar(connection, "SHOW server_version"));
			results.Add("Size", Convert.ToInt64(ExecuteScalar(connection, "SELECT pg_database_size(@dbname)", new System.Data.Common.DbParameter[] { new Npgsql.NpgsqlParameter("@dbname", connection.Database) })).FormatFileSize());
			results.Add("Software", ExecuteScalar(connection, "SELECT version()"));

      if (Convert.ToInt32(ExecuteScalar(connection, "SELECT COUNT(*) FROM pg_tables WHERE tablename='Schema'"))>0)
      {
        string schemaVersion = ExecuteScalar(connection, $"SELECT SchemaVersion FROM Schema WHERE SchemaName=@schemaName;", new System.Data.Common.DbParameter[] { new Npgsql.NpgsqlParameter("@schemaName", schemaName) });
        if (String.IsNullOrEmpty(schemaVersion) && schemaName == "*")
        {
          schemaVersion = ExecuteScalar(connection, $"SELECT SchemaVersion FROM Schema WHERE SchemaName=@schemaName;", new System.Data.Common.DbParameter[] { new Npgsql.NpgsqlParameter("@schemaName", "Nucleus.Core") });
        }
        results.Add("Schema Version", schemaVersion);
      }

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


    private int ExecuteScalarInt(System.Data.Common.DbConnection connection, string sql)
    {
      return ExecuteScalarInt(connection, sql, new System.Data.Common.DbParameter[] { });
    }

    private int ExecuteScalarInt(System.Data.Common.DbConnection connection, string sql, System.Data.Common.DbParameter[] parameters)
    {
      object? result;

      System.Data.Common.DbCommand command = connection.CreateCommand();

      command.CommandText = sql;
      command.Parameters.AddRange(parameters);

      result = command.ExecuteScalar();

#pragma warning disable CS8603 // Possible null reference return.
      return result == null ? 0 : Convert.ToInt32(result);
#pragma warning restore CS8603 // Possible null reference return.
    }



  }
}
