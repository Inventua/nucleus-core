using System;
using Microsoft.EntityFrameworkCore;
using Nucleus.Data.Common;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using System.Linq;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Base class used by entity-framework data providers.
	/// </summary>
	/// <remarks>
	/// Nucleus core and module data provider which use entity framework classes inherit this class, which contains an implementation of
	/// the schema migration functions.  Data provider implementations which register a related DataProviderMigration class must inherit 
	/// this class.
	/// </remarks>
	public abstract class DataProvider : Nucleus.Data.Common.DataProvider, IDisposable
	{
		/// <summary>
		/// Entity framework DbContext.
		/// </summary>
		public Nucleus.Data.EntityFramework.DbContext Context { get; }

		/// <summary>
		/// Logger used to log messages.
		/// </summary>
		protected ILogger<DataProvider> Logger { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="logger"></param>
		public DataProvider(DbContext context, ILogger<DataProvider> logger)
		{
			this.Context = context;
			this.Logger = logger;
		}

		/// <summary>
		/// Attempts a connection to the database.  Throws a database provider specific exception if not successful.
		/// </summary>
		public override void CheckConnection()
		{
			try
			{
				this.Context.Database.OpenConnection();
			}
			catch(Exception e)
			{
				Logger?.LogError("An error occurred while connecting to the database.", e);
				throw;
			}
		}

		///// <summary>
		///// Check whether the Schema table exists in a database-agnostic way by querying it.
		///// </summary>
		///// <returns></returns>
		///// <remarks>
		///// Data provider implementations can override this method to check whether the table exists using a database-specific
		///// method (without relying on exceptions).
		///// </remarks>
		//virtual protected Boolean SchemaTableExists()
		//{
		//	try
		//	{
		//		_ = this.Context.Schema.Count();
		//		return true;
		//	}
		//	catch (Exception)
		//	{
		//		return false;
		//	}
		//}

		///// <summary>
		///// Retrieve the version number of the latest schema update that has been applied for the specified schema name.  
		///// </summary>
		///// <param name="schemaName"></param>
		///// <returns></returns>
		///// <remarks>
		///// If no schema entry exists for the specified schema name, this function returns version 0.0.0.0.
		///// </remarks>
		//public override System.Version GetSchemaVersion(string schemaName)
		//{
		//	Version result = new(0, 0, 0, 0);

		//	if (this.SchemaTableExists())
		//	{
		//		Nucleus.Abstractions.Models.Internal.Schema schema = this.Context.Schema
		//			.Where(schema => schema.SchemaName == schemaName)
		//			.AsNoTracking()
		//			.FirstOrDefault();

		//		if (schema != null)
		//		{
		//			_ = System.Version.TryParse(schema.SchemaVersion, out result);
		//		}
		//	}

		//	return result;
		//}

		///// <summary>
		///// Add or update a record in the schema table with the specified schema name and version.
		///// </summary>
		///// <param name="schemaName"></param>
		///// <param name="version"></param>
		///// <remarks>
		///// This function is called after a schema update has been applied.
		///// </remarks>
		//private void UpdateSchemaVersion(string schemaName, Version version)
		//{
		//	Version previousSchemaVersion = GetSchemaVersion(schemaName);

		//	Nucleus.Abstractions.Models.Internal.Schema schema = new() { SchemaName = schemaName, SchemaVersion = version.ToString() };
		//	this.Context.Attach(schema);

		//	if (previousSchemaVersion == new System.Version(0, 0, 0, 0))
		//	{
		//		this.Context.Entry(schema).State = Microsoft.EntityFrameworkCore.EntityState.Added;
		//	}
		//	else
		//	{
		//		this.Context.Entry(schema).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
		//	}

		//	this.Context.SaveChanges<Nucleus.Abstractions.Models.Internal.Schema>();
		//}

		///// <summary>
		///// Execute all scripts which have a version greater than the current schema version.
		///// </summary>
		///// <param name="schemaName"></param>
		///// <param name="currentSchemaVersion"></param>
		///// <param name="scripts"></param>
		//public override void RunDatabaseScripts(string schemaName, System.Version currentSchemaVersion, IList<DatabaseSchemaScript> scripts)
		//{
		//	foreach (DatabaseSchemaScript script in scripts
		//		.Where(script => currentSchemaVersion < script.Version)
		//		.OrderBy(script => script.Version))
		//	{
		//		// Run the script
		//		RunScript(schemaName, script);
		//	}
		//}

		///// <summary>
		///// Execute the commands in the script file.  Commands are separated by "GO", followed by CRLF, LR or CR.
		///// </summary>
		///// <param name="schemaName"></param>
		///// <param name="script"></param>
		///// <returns></returns>
		///// <remarks>
		///// The use of either \r\n OR \n OR \r allows scripts created in various editors to be used successfully.  Using Environment.Newline is
		///// not appropriate because the file format is specified by the developer's text editor, not the execution environment.  Windows typically
		///// uses /r/n, Unix uses /n and Mac uses /r.
		///// </remarks>
		//private bool RunScript(string schemaName, DatabaseSchemaScript script)
		//{
		//	try
		//	{
		//		Logger.LogTrace("Running schema update script {0}.", script.Name);

		//		// Execute the entire script and schema table update in a transaction, so that it succeeds or fails as an atomic block.
		//		this.Context.Database.BeginTransaction();

		//		if (script.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
		//		{
		//			foreach (string command in script.Content.Split(new string[] { "GO\r\n", "GO\n", "GO\r" }, StringSplitOptions.RemoveEmptyEntries))
		//			{
		//				ExecuteCommand(command);
		//			}
		//		}
		//		else if (script.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
		//		{
		//		 	DataDefinition.DataDefinition definition = Newtonsoft.Json.JsonConvert.DeserializeObject<DataDefinition.DataDefinition>(script.Content, new Newtonsoft.Json.JsonConverter[] { new DataDefinition.MigrationOperationConverter() });

		//			IReadOnlyList<Microsoft.EntityFrameworkCore.Migrations.MigrationCommand> commands = this.RelationalDatabaseCreatorDependencies.MigrationsSqlGenerator.Generate(definition.Operations.ToList(), null, Microsoft.EntityFrameworkCore.Migrations.MigrationsSqlGenerationOptions.Script | Microsoft.EntityFrameworkCore.Migrations.MigrationsSqlGenerationOptions.NoTransactions);

		//			this.RelationalDatabaseCreatorDependencies.MigrationCommandExecutor.ExecuteNonQuery(commands, this.RelationalDatabaseCreatorDependencies.Connection);
				
					
					
		//		}
		//		else
		//		{
		//			throw new InvalidOperationException($"Script {script.Name} is not recognized.  Migration script names must end in '.sql' or '.json'. ");
		//		}

		//		// Add/Update the version number that was executed 
		//		UpdateSchemaVersion(schemaName, script.Version);

		//		this.Context.Database.CommitTransaction();
		//	}
		//	catch (Exception ex)
		//	{
		//		this.Context.Database.RollbackTransaction();
		//		//Logger.LogError(ex, "Running schema update script {0}.", script.Name);
		//		throw new InvalidOperationException($"Run script error [{script.Name}]: {script.Content}", ex);
		//	}

		//	return true;
		//}

		//private void ExecuteCommand(string command)
		//{
		//	// deal with last line of the file being "GO" with no cr/lf characters following
		//	if (command.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
		//	{
		//		command = command[0..^2];
		//	}
		//	this.Context.Database.ExecuteSqlRaw(command);
		//}

	}

}
