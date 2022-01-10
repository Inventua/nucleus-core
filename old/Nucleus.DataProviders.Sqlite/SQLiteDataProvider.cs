using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Linq;
using Nucleus.Abstractions.DataProviders;

namespace Nucleus.DataProviders.Sqlite
{
	/// <summary>
	/// SQLite-specific common implementation of DataProvider methods.
	/// </summary>
	abstract public class SQLiteDataProvider : DataProvider
	{
		
		private SQLiteDataProviderOptions Options { get; }
		protected ILogger<SQLiteDataProvider> Logger { get; }

		/// <summary>
		///  Create an SQLiteDataProvider instance by using the SQLiteDataProviderFactory
		/// </summary>
		/// <param name="LatestVersion"></param>
		/// <param name="Options"></param>
		/// <param name="Logger"></param>
		protected SQLiteDataProvider(SQLiteDataProviderOptions Options, ILogger<SQLiteDataProvider> Logger)
		{
			this.Options = Options;
			this.Logger = Logger;
		}

		protected override DbCommand CreateDbCommand(string CommandText)
		{
			return new Microsoft.Data.Sqlite.SqliteCommand(CommandText);
		}

		public override DbConnection CreateConnection()
		{
			// Note: SQLite database files are created automatically when opened, if the file does not already exist
			DbConnection connection = new SqliteConnection(BuildConnectionString());
			connection.Open();

			return connection;
		}

		private string BuildConnectionString()
		{
			SqliteConnectionStringBuilder Builder = new SqliteConnectionStringBuilder { DataSource = this.Options.FileName };
			return Builder.ConnectionString;
		}

		// Check for latest version of db scripts and run them as required
		protected override void CheckDatabaseSchema(string schemaName, IList<DatabaseSchemaScript> Scripts)
		{
			System.Version CurrentDBVersion;
			System.Version LatestVersion = new System.Version(0, 0, 0, 0);

			foreach (DatabaseSchemaScript script in Scripts)
			{
				if (script.Version > LatestVersion)
				{
					LatestVersion = script.Version;
				}
			}

			lock (this.GetType())
			{
				// Get version from database
				CurrentDBVersion = GetSchemaVersion(schemaName);

				if (LatestVersion > CurrentDBVersion)
				{
					// run scripts
					RunDatabaseScripts(schemaName, CurrentDBVersion, Scripts);
				}
				else
				{
					Logger.LogTrace("Schema is up to date, no action taken.");
				}
			}
		}

		protected Boolean SchemaTableExists()
		{
			string commandString = "SELECT name FROM sqlite_master WHERE type='table' AND name='Schema'";
			return ExecuteScalar(commandString) != null;
		}

		protected System.Version GetSchemaVersion(string schemaName)
		{
			System.Version result = new System.Version(0, 0, 0, 0);
			string commandString = "SELECT SchemaVersion FROM Schema WHERE SchemaName=@SchemaName";
			string value;

			if (this.SchemaTableExists())
			{
				value = (string)ExecuteScalar(
					commandString,
					new SqliteParameter[]
					{
						new SqliteParameter("@SchemaName", schemaName)
					});
								
				if (value != null)
				{
					result = System.Version.Parse(value);
				}				
			}

			return result;
		}

		protected override void UpdateSchemaVersion(string schemaName, Version Version)
		{
			Version previousSchemaVersion = GetSchemaVersion(schemaName);
			string commandString;

			if (previousSchemaVersion == new System.Version(0, 0, 0, 0))
			{
				commandString = "INSERT INTO Schema (SchemaName, SchemaVersion) VALUES (@SchemaName, @Version)";
			}
			else
			{
				commandString = "UPDATE Schema SET SchemaVersion = @Version WHERE SchemaName=@SchemaName";
			}

			ExecuteNonQuery(commandString, new SqliteParameter[]
			{
				new SqliteParameter ("@SchemaName", schemaName),
				new SqliteParameter ("@Version", Version.ToString())
			});
		}

		protected void RunDatabaseScripts(string schemaName, System.Version CurrentSchemaVersion, IList<DatabaseSchemaScript> Scripts)
		{
			foreach (DatabaseSchemaScript script in Scripts.OrderBy((script) => script.Version))
			{
				// check db version that needs updating (do not run previous scripts)
				if (CurrentSchemaVersion < script.Version)
				{
					// Run the script
					RunScript(schemaName, script);
				}
			}
		}

		protected bool RunScript(string schemaName, DatabaseSchemaScript script)
		{			
			try
			{
				Logger.LogTrace("Running schema update script {0}.", script.Name);

				this.Connect();

				ExecuteNonQuery("BEGIN TRANSACTION;", null);

				foreach (var strSQLCommand in script.Content.Trim().Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
				{
					ExecuteNonQuery(strSQLCommand, null);
				}

				// Add/Update the version number that was executed 
				UpdateSchemaVersion(schemaName, script.Version);

				ExecuteNonQuery("COMMIT;", null);

				CloseConnection(false);
			}
			catch (Exception ex)
			{
				ExecuteNonQuery("ROLLBACK;", null);
				Logger.LogError(ex, "Running schema update script {0}.", script.Name);
				throw new GeneralException("Run script error [" + script.Name + "]:", ex);
			}
			finally
			{
				// Close connection if not already closed
				CloseConnection(false);
			}

			return true;
		}

		#region "    Generic methods    "

		protected T GetItem<T>(string tableName, Guid id) where T : new()
		{
			IDataReader reader;

			if (id == Guid.Empty) return default(T);

			string commandText =
				"SELECT * " +
				$"FROM {tableName} " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", id)
					});

			try
			{
				if (reader.Read())
				{
					return ModelExtensions.Create<T>(reader);
				}
				else
				{
					return default(T);
				}
			}
			finally
			{
				reader.Close();
			}
		}

		protected List<T> ListItems<T>(string tableName) where T : new()
		{
			IDataReader reader;
			List<T> results = new List<T>();

			string commandText =
				"SELECT * " +
				$"FROM {tableName} ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[] { });

			try
			{
				while (reader.Read())
				{
					T item = ModelExtensions.Create<T>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		protected List<T> ListItems<T>(Guid id, string idColumn, string tableName) where T : new()
		{
			IDataReader reader;
			List<T> results = new();

			string commandText =
				"SELECT * " +
				$"FROM {tableName} " +
				$"WHERE {idColumn} = @{idColumn} ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter($"@{idColumn}", id)
				});

			try
			{
				while (reader.Read())
				{
					T item = ModelExtensions.Create<T>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		#endregion
	}
}
