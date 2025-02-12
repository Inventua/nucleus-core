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
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;

namespace Nucleus.Data.Sqlite
{
	/// <summary>
	/// Sqlite database provider.
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
			return AddSqlite<TDataProvider>(services);
		}

		/// <summary>
		/// Add Sqlite data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses Sqlite.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <returns></returns>
		private static IServiceCollection AddSqlite<TDataProvider>(IServiceCollection services)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			// add Sqlite data provider objects to the services collection.
			
			services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.Sqlite.SqliteDataProviderMigration<TDataProvider>>();
			services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.Sqlite.SqliteDbContextConfigurator<TDataProvider>>();
			return services;			
		}

    /// <summary>
    /// Test the database connection.
    /// </summary>
    /// <param name="connectionString"></param>
    public void TestConnection(string connectionString)
    {
      System.Data.Common.DbConnection connection = new Microsoft.Data.Sqlite.SqliteConnection(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(connectionString));

      connection.Open();

      System.Data.Common.DbCommand command = connection.CreateCommand();
      command.CommandText = "SELECT 1";

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
      // Sqlite doesn't have the concept of databases
      return new string[] { };
    }

    /// <summary>
    /// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
    /// the database provider implementing this interface.
    /// </summary>
    public Dictionary<string, string> GetDatabaseInformation(DatabaseConnectionOption options, string schemaName)
		{
			Dictionary<string, string> results = new();

      System.Data.Common.DbConnection connection = new Microsoft.Data.Sqlite.SqliteConnection(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(options.ConnectionString));
      
      connection.Open();

			// For Sqlite, we show the filename (with no path or extension) as the database name, because the Sqlite "database" is always called "main".  We
			// exclude the path in order to not show potentially sensitive information, and we exclude the file extension because it doesn't add anything to
			// the display, and the filename presents better on-screen without the extension (which is pretty much always ".db" anyway).
			results.Add("Database", System.IO.Path.GetFileNameWithoutExtension(connection.DataSource));
			results.Add("Version", ExecuteScalar(connection, "SELECT sqlite_version()"));			
			results.Add("Size", new System.IO.FileInfo(connection.DataSource).Length.FormatFileSize());

      if (Convert.ToInt32(ExecuteScalar(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Schema'")) > 0)
      {
        string schemaVersion = ExecuteScalar(connection, $"SELECT [SchemaVersion] FROM [Schema] WHERE [SchemaName]=@schemaName;", new System.Data.Common.DbParameter[] { new Microsoft.Data.Sqlite.SqliteParameter("@schemaName", schemaName) });
        if (String.IsNullOrEmpty(schemaVersion) && schemaName == "*")
        {
          schemaVersion = ExecuteScalar(connection, $"SELECT [SchemaVersion] FROM [Schema] WHERE [SchemaName]=@schemaName;", new System.Data.Common.DbParameter[] { new Microsoft.Data.Sqlite.SqliteParameter("@schemaName", "Nucleus.Core") });
        }
        results.Add("Schema Version", schemaVersion);
      }
      else
      {
        results.Add("Schema Version", "-");
      }

      connection.Close();
     
			return results;
		}

    // Future use
    //public void BackupDatabase(DatabaseConnectionOption options, string targetFilename)
    //{
    //  Microsoft.Data.Sqlite.SqliteConnection connection = new(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(options.ConnectionString));

    //  connection.Open();

    //  using SqliteConnection backup = new ($"Data Source={targetFilename}");
    //  {
    //    connection.BackupDatabase(backup);
    //  }

    //  connection.Close();
    //}

		private static string ExecuteScalar(System.Data.Common.DbConnection connection, string sql)
		{
			string result;

			System.Data.Common.DbCommand command = connection.CreateCommand();

			command.CommandText = sql;
			result = Convert.ToString(command.ExecuteScalar());

			return result;
		}

    private string ExecuteScalar(System.Data.Common.DbConnection connection, string sql, System.Data.Common.DbParameter[] parameters)
    {
      object result;

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
