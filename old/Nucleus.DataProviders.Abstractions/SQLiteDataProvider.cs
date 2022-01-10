//using System;
//using System.Collections.Generic;
//using System.Data.Common;
//using System.Text;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.Data.Sqlite;
//using System.Data;

//namespace Nucleus.DataProviders.Abstractions
//{
//	/// <summary>
//	/// SQLite-specific common implementation of DataProvider methods.
//	/// </summary>
//	abstract public class SQLiteDataProvider : DataProvider
//	{
//		private SQLiteDataProviderOptions Options { get; }
//		private ILogger<SQLiteDataProvider> Logger { get; }

//		/// <summary>
//		///  Create an SQLiteDataProvider instance by using the SQLiteDataProviderFactory
//		/// </summary>
//		/// <param name="LatestVersion"></param>
//		/// <param name="Options"></param>
//		/// <param name="Logger"></param>
//		protected SQLiteDataProvider(SQLiteDataProviderOptions Options, ILogger<SQLiteDataProvider> Logger)
//		{
//			this.Options = Options;
//			this.Logger = Logger;
//		}

//		protected override DbCommand CreateDbCommand(string CommandText)
//		{
//			return new Microsoft.Data.Sqlite.SqliteCommand(CommandText);
//		}

//		public override DbConnection CreateConnection()
//		{
//			// Note: SQLite database files are created automatically when opened, if the file does not already exist
//			DbConnection connection = new SqliteConnection(BuildConnectionString());
//			connection.Open();

//			return connection;
//		}

//		string BuildConnectionString()
//		{
//			SqliteConnectionStringBuilder Builder = new SqliteConnectionStringBuilder { DataSource = this.Options.FileName };
//			return Builder.ConnectionString;
//		}

//		// Check for latest version of db scripts and run them as required
//		protected override void CheckDatabaseSchema(IList<DatabaseSchemaScript> Scripts)
//		{
//			System.Version CurrentDBVersion;
//			System.Version LatestVersion = new System.Version(0, 0, 0, 0);

//			foreach (DatabaseSchemaScript script in Scripts)
//			{
//				if (script.Version > LatestVersion)
//				{
//					LatestVersion = script.Version;
//				}
//			}

//			lock (this.GetType())
//			{
//				// Get version from database
//				CurrentDBVersion = GetSchemaVersion();

//				if (CurrentDBVersion == new System.Version(0, 0, 0, 0))
//				{
//					// the database has just been created and is empty, run the script to create the Schema table
//					RunScript(
//						new DatabaseSchemaScript("00.00.01.sql", 
//						new System.Version(0, 0, 1, 0), 
//						"CREATE TABLE [Schema] ([SchemaVersion] TEXT NULL); " +
//						"GO" + Environment.NewLine + 
//						"INSERT INTO [Schema] (SchemaVersion) VALUES ('0.0.1.0');"));

//					CurrentDBVersion = GetSchemaVersion();
//				}

//				if (LatestVersion > CurrentDBVersion)
//				{
//					// run scripts
//					RunDatabaseScripts(CurrentDBVersion, Scripts);
//				}
//			}
//		}

//		protected Boolean SchemaTableExists()
//		{
//			string commandString = "SELECT name FROM sqlite_master WHERE type='table' AND name='Schema'";
//			return ExecuteScalar(commandString) != null;
//		}

//		protected System.Version GetSchemaVersion()
//		{
//			System.Version result = new System.Version(0, 0, 0, 0);
//			string commandString = "SELECT SchemaVersion FROM Schema";
//			string value;

//			if (this.SchemaTableExists())
//			{
//				value = (string)ExecuteScalar(commandString);
//				result = System.Version.Parse(value);				
//			}

//			return result;
//		}

//		protected override void UpdateSchemaVersion(Version Version)
//		{
//			string commandString = "UPDATE Schema SET SchemaVersion = @Version";

//			ExecuteNonQuery(commandString, new SqliteParameter[]
//			{
//				new SqliteParameter ("@Version", Version.ToString())
//			});
//		}
//	}
//}
