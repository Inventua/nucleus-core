using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TDataProvider">
	/// Type of the data provider implementation that this class handles migration for.
	/// </typeparam>
	/// <remarks>
	/// Database-specific data migration implementations inherit this class in order to provide data migrations for their database schema.
	/// </remarks>
	public abstract class DataProviderMigration<TDataProvider> : Nucleus.Data.Common.DataProviderMigration<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		/// <summary>
		/// Entity framework dbcontext for this instance.
		/// </summary>
		protected DbContext DbContext { get; }
		private ILogger<DataProviderMigration<TDataProvider>> Logger { get; }

		private Nucleus.Abstractions.EventHandlers.IEventDispatcher EventDispatcher { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="scriptFolderNames"></param>
		/// <param name="eventDispatcher"></param>
		/// <param name="logger"></param>
		public DataProviderMigration(DbContext context, string[] scriptFolderNames, Nucleus.Abstractions.EventHandlers.IEventDispatcher eventDispatcher, ILogger<DataProviderMigration<TDataProvider>> logger) : base(scriptFolderNames)
		{
			this.DbContext = context;
			this.EventDispatcher = eventDispatcher;
			this.Logger = logger;
		}

		/// <summary>
		/// Compare the value matching the SchemaName property with the versions of available scripts, and apply any schema updates 
		/// which have not already been applied.
		/// </summary>
		/// <remarks>
		/// The default schema name is the base namespace for the data provider class specified in the constructor.
		/// </remarks>
		public override void CheckDatabaseSchema()
		{
			IList<DatabaseSchemaScript> Scripts = this.SchemaScripts;
			System.Version CurrentDBVersion;
			System.Version LatestVersion = new(0, 0, 0, 0);

			foreach (DatabaseSchemaScript script in Scripts)
			{
				if (script.Version > LatestVersion)
				{
					LatestVersion = script.Version;
				}
			}

			// Get version from database
			CurrentDBVersion = GetSchemaVersion(this.SchemaName);

			if (LatestVersion > CurrentDBVersion)
			{
				// run scripts
				this.RunDatabaseScripts(this.SchemaName, CurrentDBVersion, Scripts);
			}
			else
			{
				Logger.LogTrace("Schema is up to date, no action taken.");
			}

		}


		/// <summary>
		/// Check whether the Schema table exists in a database-agnostic way by quering it.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Data provider implementations can override this method to check whether the table exists using a database-specific
		/// method (without relying on exceptions).
		/// </remarks>
		virtual protected Boolean SchemaTableExists()
		{
			return DatabaseObjectExists("Schema", DatabaseObjectTypes.Table);
			//try
			//{
			//	this.DbContext.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Schema");
			//	//_ = this.DbContext.Schema.Count();
			//	return true;
			//}
			//catch (Exception)
			//{
			//	return false;
			//}
		}

		/// <summary>
		/// Retrieve the version number of the latest schema update that has been applied for the specified schema name.  
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		/// <remarks>
		/// If no schema entry exists for the specified schema name, this function returns version 0.0.0.0.
		/// </remarks>
		public override System.Version GetSchemaVersion(string schemaName)
		{
			Version result = new(0, 0, 0, 0);

			if (this.SchemaTableExists())
			{
				Nucleus.Abstractions.Models.Internal.Schema schema = this.DbContext.Schema
					.Where(schema => schema.SchemaName == schemaName)
					.AsNoTracking()
					.FirstOrDefault();

				if (schema != null)
				{
					_ = System.Version.TryParse(schema.SchemaVersion, out result);
				}
			}

			return result;
		}

		/// <summary>
		/// Add or update a record in the schema table with the specified schema name and version.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="version"></param>
		/// <remarks>
		/// This function is called after a schema update has been applied.
		/// </remarks>
		private void UpdateSchemaVersion(string schemaName, Version version)
		{
			Nucleus.Abstractions.Models.Internal.Schema existing = this.DbContext.Schema
				.Where(schema => schema.SchemaName == schemaName)
				.AsNoTracking()
				.FirstOrDefault();

			Nucleus.Abstractions.Models.Internal.Schema schema = new() { SchemaName = schemaName, SchemaVersion = version.ToString() };

			this.DbContext.Attach(schema);
			this.DbContext.Entry(schema).State = existing == null ? EntityState.Added : EntityState.Modified;

			this.DbContext.SaveChanges<Nucleus.Abstractions.Models.Internal.Schema>();
		}

		/// <summary>
		/// Execute all scripts which have a version greater than the current schema version.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="currentSchemaVersion"></param>
		/// <param name="scripts"></param>
		public override void RunDatabaseScripts(string schemaName, System.Version currentSchemaVersion, IList<DatabaseSchemaScript> scripts)
		{
			foreach (DatabaseSchemaScript script in scripts
				.Where(script => currentSchemaVersion < script.Version)
				.OrderBy(script => script.Version))
			{
				// Run the script
				RunScript(schemaName, script);

				this.EventDispatcher?.RaiseEvent<MigrateEvent, Migrate>(new MigrateEvent(schemaName, script.FullName, currentSchemaVersion, script.Version));
			}
		}

		/// <summary>
		/// Execute the commands in the script file.  Commands are separated by "GO", followed by CRLF, LR or CR.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="script"></param>
		/// <returns></returns>
		/// <remarks>
		/// The use of either \r\n OR \n OR \r allows scripts created in various editors to be used successfully.  Using Environment.Newline is
		/// not appropriate because the file format is specified by the developer's text editor, not the execution environment.  Windows typically
		/// uses /r/n, Unix uses /n and Mac uses /r.
		/// </remarks>
		private bool RunScript(string schemaName, DatabaseSchemaScript script)
		{
			try
			{
				Logger.LogTrace("Running schema update script {0}.", script.FullName);

				// Execute the entire script and schema table update in a transaction, so that it succeeds or fails as an atomic block.
				this.DbContext.Database.BeginTransaction();

				if (script.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
				{

					foreach (string command in script.Content.Split(new string[] { "GO\r\n", "GO\n", "GO\r" }, StringSplitOptions.RemoveEmptyEntries))
					{
						ExecuteCommand(script, command);
					}
				}
				else if (script.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				{
					DataDefinition definition = Newtonsoft.Json.JsonConvert.DeserializeObject<DataDefinition>(script.Content, new Newtonsoft.Json.JsonConverter[] { new MigrationOperationConverter(this.DbContext.Database.ProviderName), new SystemTypeConverter() });

					if (definition.SchemaName != schemaName)
					{
						throw new InvalidOperationException($"The schema script {script.FullName} schema name {definition.SchemaName} does not match the expected schema name {schemaName}.");
					}

					if (!definition.Version.Equals(script.Version))
					{
						throw new InvalidOperationException($"The schema script {script.FullName} version {definition.Version} does not match the expected schema name {script.Version}.");
					}


					foreach (MigrationOperation operation in definition.Operations)
					{
						// Operations wrapped in a DatabaseProviderSpecificOperation can be returned as null if conditions are not met, skip nulls them
						if (operation != null)
						{
							ApplyCorrections(operation);

							if (operation.GetType() == typeof(DatabaseProviderSpecificOperation))
							{
								if ((operation as DatabaseProviderSpecificOperation).IsValidFor(this.DbContext.Database.ProviderName))
								{
									ExecuteCommand(script, (operation as DatabaseProviderSpecificOperation).Operation);
								}
							}
							else
							{
								ExecuteCommand(script, operation);
							}
						}
					}
				}
				else
				{
					throw new InvalidOperationException($"Script {script.FullName} is not recognized.  Migration script names must end in '.sql' or '.json'. ");
				}

				// Add/Update the version number that was executed 
				UpdateSchemaVersion(schemaName, script.Version);

				this.DbContext.Database.CommitTransaction();

			}
			catch (Exception)
			{
				this.DbContext.Database.RollbackTransaction();
				throw;
			}

			return true;
		}


		private void ApplyCorrections(MigrationOperation operation)
		{
			if (operation.GetType() == typeof(CreateTableOperation))
			{
				CreateTableOperation createTableOperation = operation as CreateTableOperation;

				foreach (AddColumnOperation column in createTableOperation.Columns)
				{
					if (column.Table is null)
					{
						column.Table = createTableOperation.Name;
					}

					ApplyCorrections(column);

					if (DbContext.Database.ProviderName.EndsWith("SqlServer", StringComparison.OrdinalIgnoreCase))
					{
						// For sql server, If the column is a not-null guid, named "id" or "{tablename}Id" or "{tablename}_Id" and is the only column in the primary key,
						// set default to newsequentialid() 
						if
						(
							createTableOperation.PrimaryKey != null &&
							column.ClrType == typeof(Guid) &&
							(column.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || column.Name.Equals($"{column.Table}Id", StringComparison.OrdinalIgnoreCase) || column.Name.Equals($"{column.Table}_Id", StringComparison.OrdinalIgnoreCase)) &&
							createTableOperation.PrimaryKey.Columns.Count() == 1 &&
							createTableOperation.PrimaryKey.Columns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
						{
							column.DefaultValueSql = "newsequentialid()";
						}
					}
				}

				// Allow table to be omitted from primary key declaration in createTable
				if (createTableOperation.PrimaryKey != null)
				{
					if (createTableOperation.PrimaryKey.Table is null)
					{
						createTableOperation.PrimaryKey.Table = createTableOperation.Name;
					}
				}

				// Allow table to be omitted from foreign key declaration in createTable
				foreach (AddForeignKeyOperation key in createTableOperation.ForeignKeys.ToList())
				{
					// Keys wrapped in a DatabaseProviderSpecificOperation can be returned as null if conditions are not met, remove them
					if (key is null)
					{
						createTableOperation.ForeignKeys.Remove(key);
					}
					else if (key.Table is null)
					{
						key.Table = createTableOperation.Name;
					}
				}
			}

			if (operation.GetType() == typeof(AddColumnOperation))
			{
				ApplyCorrections(operation as AddColumnOperation);
			}

			if (operation.GetType() == typeof(DatabaseProviderSpecificOperation))
			{
				ApplyCorrections((operation as DatabaseProviderSpecificOperation).Operation);
			}
		}

		/// <summary>
		/// Apply corrections for an AddColumnOperation, which could be standalone, or part of a create table operation.
		/// </summary>
		/// <param name="operation"></param>
		private void ApplyCorrections(AddColumnOperation operation)
		{
			// Default to unicode (nvarchar) types if not specified
			if (!operation.IsUnicode.HasValue)
			{
				operation.IsUnicode = true;
			}

			if (DbContext.Database.ProviderName.EndsWith("Sqlite", StringComparison.OrdinalIgnoreCase))
			{
				if (operation.ClrType == typeof(string) && operation.Collation == null)
				{
					// For Sqlite, make all string columns case-insensitive unless collation is already specified
					operation.Collation = "NOCASE";
				}
			}

			if (operation.ClrType == typeof(Boolean))
			{
				if (DbContext.Database.ProviderName.EndsWith("PostgreSQL", StringComparison.OrdinalIgnoreCase))
				{
					// For Postgres, booleans are "true" or "false" rather than "1" or "0"
					if (operation.DefaultValueSql == "1")
					{
						operation.DefaultValueSql = "true";
					}
					else
					{
						operation.DefaultValueSql = "false";
					}
				}
				else
				{
					// For everything else, booleans are "1" or "0" rather than "true" or "false"
					if (operation.DefaultValueSql == "true")
					{
						operation.DefaultValueSql = "1";
					}
					else
					{
						operation.DefaultValueSql = "0";
					}
				}
			}
		}

		private void ExecuteCommand(DatabaseSchemaScript script, string command)
		{
			try
			{
				// deal with last line of the file being "GO" with no cr/lf characters following
				if (command.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
				{
					command = command[0..^2];
				}
				this.DbContext.Database.ExecuteSqlRaw(command);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Migration script error [{script.FullName}]: {command}", ex);
			}
		}

		private void ExecuteCommand(DatabaseSchemaScript script, MigrationOperation operation)
		{
			if (operation is CreateTableOperation)
			{
				if (DatabaseObjectExists((operation as CreateTableOperation).Name, DatabaseObjectTypes.Table))
				{
					// table already exists
					this?.Logger.LogInformation("Table {0} already exists, create table operation skipped.", (operation as CreateTableOperation).Name);
					return;
				}
			}

			if (operation is CreateIndexOperation)
			{
				if (DatabaseObjectExists((operation as CreateIndexOperation).Name, DatabaseObjectTypes.Index))
				{
					// table already exists
					this?.Logger.LogInformation("Index {0} already exists, create index operation skipped.", (operation as CreateIndexOperation).Name);
					return;
				}
			}

			IMigrationsSqlGenerator sqlGenerator = this.DbContext.GetInfrastructure().GetService<IMigrationsSqlGenerator>();

			foreach (MigrationCommand migrationCommand in sqlGenerator.Generate(new List<MigrationOperation>() { operation }, null, MigrationsSqlGenerationOptions.NoTransactions))
			{
				try
				{
					IRelationalConnection connection = this.DbContext.GetInfrastructure().GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalConnection>();
					migrationCommand.ExecuteNonQuery(connection, null);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Migration script error [{script.FullName}]: {migrationCommand.CommandText}", ex);
				}
			}


		}

	}
}
