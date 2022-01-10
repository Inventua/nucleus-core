//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.Common;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Nucleus.DataProviders.Abstractions
//{
//	/// <summary>
//	/// Data provider base class.  Provides common functionality for data providers.
//	/// </summary>
//	public abstract class DataProvider: IDisposable
//	{
//		private DbConnection _Connection { get; set; }
//		public Boolean IsPaused { get; protected set; }

//		// Check that the database exists, create if necessary, check and update schema
//		public abstract DbConnection CreateConnection();

//		protected abstract void UpdateSchemaVersion(System.Version Version);
//		protected abstract void CheckDatabaseSchema(IList<DatabaseSchemaScript> Scripts);

//		public DbConnection Connect()
//		{
//			Exception objException = null;
//			int intRetry = 0;

//			if (this.IsPaused)
//			{
//				while (this.IsPaused)
//				{
//					Task.Delay(TimeSpan.FromSeconds(5));
//					intRetry = intRetry + 1;

//					if (intRetry > 10)
//						throw new DataException("Connection Timeout", null);
//				}
//			}

//			if (this._Connection == null)
//			{
//				lock (this.GetType())
//				{
//					try
//					{
//						if (this._Connection == null)
//						{
//							this._Connection = CreateConnection();
//						}
//					}
//					catch (Exception ex)
//					{
//						objException = ex;
//					}
//				}
//			}

//			if (objException != null)
//				throw objException;

//			return this._Connection;
//		}

		

//		public void CloseConnection(Boolean Pause)
//		{
//			if (Pause)
//			{
//				this.IsPaused = true;
//			}

//			if (this._Connection != null && this._Connection.State == System.Data.ConnectionState.Open)
//			{
//				this._Connection.Close();
//				this._Connection = null;
//			}
//		}

//		public void ResumeConnection()
//		{
//			this.IsPaused = false;
//		}
		
//		protected void RunDatabaseScripts(System.Version CurrentSchemaVersion, IList<DatabaseSchemaScript> Scripts)
//		{			
//			foreach (DatabaseSchemaScript script in Scripts.OrderBy( (script) => script.Version ))
//			{
//				// check db version that needs updating (do not run previous scripts)
//				if (CurrentSchemaVersion < script.Version)
//				{  
//					// Run the script
//					RunScript(script);
//				}
//			}
//		}

//		protected bool RunScript(DatabaseSchemaScript Script)
//		{

//			try
//			{
//				foreach (var strSQLCommand in Script.Content.Trim().Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
//					ExecuteNonQuery(strSQLCommand, null);

//				//SqLiteHelper.SetCommandTimeout(-1);

//				// Add/Update the version number that was executed 
//				UpdateSchemaVersion(Script.Version);

//				//Microsoft.VisualBasic.Logging.LogInformation($"Updated database version to {Script.Version}");

//				CloseConnection(false);

//				//  Microsoft.VisualBasic.Logging.LogInformation($"Script execution complete.");
//			}
//			catch (Exception ex)
//			{
//				//Microsoft.VisualBasic.Logging.LogException(ex);
//				throw new DataException("Run script error [" + Script.Name + "]:", ex);
//			}
//			finally
//			{
//				// Close connection if not already closed
//				CloseConnection(false);
//			}

//			return true;
//		}



//		protected abstract DbCommand CreateDbCommand(string CommandText);

//		public IDataReader ExecuteReader(string CommandText)
//		{
//			return ExecuteReader(CommandText, null);
//		}

//		public IDataReader ExecuteReader(string CommandText, params DbParameter[] Parameters) // SQLiteDataReader
//		{
//			DbCommand objCommand = CreateDbCommand(CommandText);
//			IDataReader objReader = null;

//			if (String.IsNullOrEmpty(CommandText))
//				throw new ArgumentNullException("CommandText");

//			objCommand.Connection = this.Connect();

//			PrepareParameters(objCommand, Parameters);

//			objReader = objCommand.ExecuteReader();
			
//			return objReader;
//		}

//		public int ExecuteNonQuery(string CommandText)
//		{
//			return ExecuteNonQuery(CommandText, null);
//		}

//		public int ExecuteNonQuery(string CommandText, params DbParameter[] Parameters)
//		{
//			DbCommand objCommand = CreateDbCommand(CommandText);

//			if ((CommandText == null || CommandText.Length == 0))
//				throw new ArgumentNullException("CommandText");

//			objCommand.Connection = this.Connect();

//			PrepareParameters(objCommand, Parameters);

//			try
//			{
//				return objCommand.ExecuteNonQuery();
//			}
//			finally
//			{
//				objCommand.Dispose();
//			}
//		}

//		public object ExecuteScalar(string CommandText)
//		{
//			return ExecuteScalar(CommandText, null);
//		}

//		public object ExecuteScalar(string CommandText, params DbParameter[] Parameters)
//		{
//			DbCommand objCommand = CreateDbCommand(CommandText);
//			object objResult;

//			if (CommandText == null || CommandText.Length == 0)
//				throw new ArgumentNullException("CommandText");

//			objCommand.Connection = this.Connect();

//			PrepareParameters(objCommand, Parameters);

//			objResult = objCommand.ExecuteScalar();
//			objCommand.Dispose();

//			return objResult;
//		}

//		private void PrepareParameters(DbCommand Command, params DbParameter[] Parameters)
//		{
//			if (Parameters != null)
//			{
//				foreach (DbParameter objParameter in Parameters)
//				{
//					if (objParameter.Value == null)
//						objParameter.Value = DBNull.Value;
//					Command.Parameters.Add(objParameter);
//				}
//			}
//		}

//		virtual public void Dispose()
//		{
//			this.CloseConnection(false);
//		}
//	}

//}
