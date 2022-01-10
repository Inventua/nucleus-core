using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Nucleus.Core.DataProviders.Abstractions
{
	/// <summary>
	/// Data provider base class.  Provides common functionality for data providers.
	/// </summary>
	public abstract class DataProvider: IDisposable
	{
		private DbConnection Connection { get; set; }
		public Boolean IsPaused { get; protected set; }

		// Check that the database exists, create if necessary, check and update schema
		public abstract DbConnection CreateConnection();
		protected DbTransaction CurrentTransaction { get; set; }

		protected abstract void UpdateSchemaVersion(string schemaName, System.Version Version);
		//public abstract void CheckDatabaseSchema(string schemaName);
		protected abstract void CheckDatabaseSchema(string schemaName, IList<DatabaseSchemaScript> Scripts);

		public abstract string SchemaName { get; }
		public abstract IList<DatabaseSchemaScript> SchemaScripts { get; }

		/// <summary>
		/// Read Schema.SchemaVersion and compare to the latest version of embedded database scripts.  If a script with a later version exists,
		/// run the script to update the database schema.
		/// </summary>
		public void CheckDatabaseSchema(string schemaName)
		{
			CheckDatabaseSchema(schemaName, SchemaScripts);
		}

		public DbConnection Connect()
		{
			Exception objException = null;
			int intRetry = 0;

			if (this.IsPaused)
			{
				while (this.IsPaused)
				{
					Task.Delay(TimeSpan.FromSeconds(5));
					intRetry++;

					if (intRetry > 10)
					{
						throw new TimeoutException("Connection Timeout", null);
					}
				}
			}

			if (this.Connection == null)
			{
				lock (this.GetType())
				{
					try
					{
						if (this.Connection == null)
						{
							this.Connection = CreateConnection();
						}
					}
					catch (Exception ex)
					{
						objException = ex;
					}
				}
			}

			if (objException != null)
				throw objException;

			return this.Connection;
		}

		protected Boolean BeginTransaction()
		{
			if (this.CurrentTransaction == null)
			{
				this.CurrentTransaction = this.Connect().BeginTransaction();
				return true;
			}

			return false;
			//ExecuteNonQuery("BEGIN TRANSACTION;", null);
		}

		protected void CommitTransaction()
		{
			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.Commit();
				this.CurrentTransaction = null;
			}
			//ExecuteNonQuery("COMMIT;", null);
		}

		protected void RollbackTransaction()
		{
			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.Rollback();
				this.CurrentTransaction = null;
			}
			//ExecuteNonQuery("ROLLBACK;", null);
		}

		public void CloseConnection(Boolean Pause)
		{
			if (Pause)
			{
				this.IsPaused = true;
			}

			if (this.Connection != null && this.Connection.State == System.Data.ConnectionState.Open)
			{
				this.Connection.Close();
				this.Connection = null;
			}
		}

		public void ResumeConnection()
		{
			this.IsPaused = false;
		}

		/// <summary>
		/// Create a DatabaseSchemaScript object representing an embedded database script.
		/// </summary>
		/// <param name="resourceName">Relative name of an embedded database script within the scripts namespace.</param>
		/// <returns>A DatabaseSchemaScript object</returns>
		protected DatabaseSchemaScript BuildManifestSchemaScript(string scriptNamespace, string resourceName)
		{
			StreamReader reader = new(this.GetType().Assembly.GetManifestResourceStream(resourceName));
			string shortResourceName = resourceName.Replace(scriptNamespace, "", StringComparison.OrdinalIgnoreCase);
			return new DatabaseSchemaScript
				(
					shortResourceName,
					ParseVersion(shortResourceName),
					reader.ReadToEnd()
				);
		}

		/// <summary>
		/// Parse the filename of an embedded resource file and return its version number.
		/// </summary>
		/// <param name="resourceName">Relative name of an embedded database script within the inventua.massy.osacloudfiles.dataprovider.scripts namespace.</param>
		/// <returns>System.Version containing the embedded script file version.  The version is the file name without the file extension.</returns>
		private static System.Version ParseVersion(string resourceName)
		{
			System.Version result;

			if (System.Version.TryParse(System.IO.Path.GetFileNameWithoutExtension(resourceName), out result))
			{
				return result;
			}
			else
			{
				throw new ApplicationException($"Unexpected resource {resourceName} in dataprovider/scripts.");
			}
		}

		protected abstract DbCommand CreateDbCommand(string CommandText);

		public IDataReader ExecuteReader(string CommandText)
		{
			return ExecuteReader(CommandText, null);
		}

		public IDataReader ExecuteReader(string CommandText, params DbParameter[] Parameters) // SQLiteDataReader
		{
			DbCommand objCommand = CreateDbCommand(CommandText);
			IDataReader objReader = null;

			if (String.IsNullOrEmpty(CommandText))
				throw new ArgumentNullException(nameof(CommandText));

			objCommand.Connection = this.Connect();
			objCommand.Transaction = this.CurrentTransaction;

			PrepareParameters(objCommand, Parameters);

			objReader = objCommand.ExecuteReader();
			
			return objReader;
		}

		public int ExecuteNonQuery(string CommandText)
		{
			return ExecuteNonQuery(CommandText, null);
		}

		public int ExecuteNonQuery(string CommandText, params DbParameter[] Parameters)
		{
			DbCommand objCommand = CreateDbCommand(CommandText);

			if ((CommandText == null || CommandText.Length == 0))
				throw new ArgumentNullException(nameof(CommandText));

			objCommand.Connection = this.Connect();
			objCommand.Transaction = this.CurrentTransaction;

			PrepareParameters(objCommand, Parameters);

			try
			{
				return objCommand.ExecuteNonQuery();
			}
			finally
			{
				objCommand.Dispose();
			}
		}

		public object ExecuteScalar(string CommandText)
		{
			return ExecuteScalar(CommandText, null);
		}

		public object ExecuteScalar(string CommandText, params DbParameter[] Parameters)
		{
			DbCommand objCommand = CreateDbCommand(CommandText);
			object objResult;

			if (CommandText == null || CommandText.Length == 0)
				throw new ArgumentNullException(nameof(CommandText));

			objCommand.Connection = this.Connect();
			objCommand.Transaction = this.CurrentTransaction;

			PrepareParameters(objCommand, Parameters);

			try
			{
				objResult = objCommand.ExecuteScalar();
			}
			catch (Exception e)
			{
				// Catch SQLite exceptions in the form: SQLite Error 19: 'NOT NULL constraint failed: MailTemplates.Subject'. and change into a more 
				// friendly error.
				System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(e.Message, "'NOT NULL constraint failed: (\\w*).(\\w*)'");
				if (match.Success && match.Groups.Count > 2)
				{
					throw new ConstraintException($"{match.Groups[1].Value}.{match.Groups[2].Value}", $"The {match.Groups[2].Value} field is required.", e);
				}
				else
				{
					throw;
				}
			}
			finally
			{
				objCommand.Dispose();
			}

			return objResult;
		}

		private static void PrepareParameters(DbCommand Command, params DbParameter[] Parameters)
		{
			if (Parameters != null)
			{
				foreach (DbParameter objParameter in Parameters)
				{
					if (objParameter.Value == null)
						objParameter.Value = DBNull.Value;

					if (objParameter.Value is Guid && (Guid)objParameter.Value == Guid.Empty)
						objParameter.Value = DBNull.Value;

					if (objParameter.Value is DateTime && (DateTime)objParameter.Value == DateTime.MinValue)
						objParameter.Value = DBNull.Value;

					if (objParameter.Value is DateTimeOffset && (DateTimeOffset)objParameter.Value == DateTimeOffset.MinValue)
						objParameter.Value = DBNull.Value;

					Command.Parameters.Add(objParameter);
				}
			}
		}

		virtual public void Dispose()
		{
			this.CloseConnection(false);
			GC.SuppressFinalize(this);
		}
	}

}
