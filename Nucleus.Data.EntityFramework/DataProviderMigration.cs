﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.Common;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using DocumentFormat.OpenXml.Math;
using static Nucleus.Data.Common.DataProviderSchemas;

namespace Nucleus.Data.EntityFramework
{
  /// <summary>
  /// Methods to handle schema migration operations.
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
      List<DatabaseSchemaScript> scripts = this.SchemaScripts.ToList();
      System.Version currentDBVersion;
      System.Version latestVersion = new(0, 0, 0, 0);

      if (scripts.Any())
      {
        // if there are any scripts for the database schema, include 00.00.00.json (embedded in this assembly) to create the "schema" table
        scripts.Insert(0, Nucleus.Data.Common.DatabaseSchemaScript.BuildManifestSchemaScript
        (
          typeof(Nucleus.Data.EntityFramework.DataProvider).Assembly,
          $"{typeof(Nucleus.Data.EntityFramework.DataProvider).Namespace}.Scripts.",
          $"{typeof(Nucleus.Data.EntityFramework.DataProvider).Namespace}.Scripts.00.00.00.json")
        );
      }

      foreach (DatabaseSchemaScript script in scripts)
      {
        if (script.Version > latestVersion)
        {
          latestVersion = script.Version;
        }
      }

      // Get version from database
      currentDBVersion = GetSchemaVersion(DataProviderSchemas.GetSchemaName(typeof(TDataProvider)).DataProviderSchemaName);

      if (currentDBVersion == null || latestVersion > currentDBVersion)
      {
        // run scripts
        this.RunDatabaseScripts(DataProviderSchemas.GetSchemaName(typeof(TDataProvider)).DataProviderSchemaName, currentDBVersion, scripts);
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
    }

    /// <summary>
    /// Retrieve the version number of the latest schema update that has been applied for the specified schema name.  
    /// </summary>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    /// <remarks>
    /// If no schema entry exists for the specified schema name, this function returns null.
    /// </remarks>
    public override System.Version GetSchemaVersion(string schemaName)
    {
      Version result = null; // = new(0, 0, 0, 0);

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
        .Where(script => currentSchemaVersion < script.Version || currentSchemaVersion == null)
        .OrderBy(script => script.Version))
      {
        RunScript(schemaName, script);
        this.EventDispatcher?.RaiseEvent<MigrateEventArgs, MigrateEvent>(new MigrateEventArgs(schemaName, script.FullName, currentSchemaVersion, script.Version));
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
      Logger.LogTrace("Running schema update script {0}.", script.FullName);

      if (script.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
      {
        ExecuteTransaction(true, () =>
        {
          foreach (string command in script.Content.Split(new string[] { "GO\r\n", "GO\n", "GO\r" }, StringSplitOptions.RemoveEmptyEntries))
          {
            ExecuteCommand(script, command);
          }
        });
      }
      else if (script.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
      {
        DataDefinition definition = Newtonsoft.Json.JsonConvert.DeserializeObject<DataDefinition>(script.Content, new Newtonsoft.Json.JsonConverter[] { new MigrationOperationConverter(this.DbContext.Database.ProviderName), new SystemTypeConverter() });
        if ((this.DbContext as Nucleus.Data.EntityFramework.DbContext).DbContextConfigurator.GetType().Assembly.GetName().Name == "Nucleus.Data.MySql")
        {
          // MySql automatically commits transactions when it executes most data definition language commands, so we can't use 
          // transactions.
          // https://dev.mysql.com/doc/refman/8.0/en/implicit-commit.html
          definition.UseTransaction = false;
        }

        if (definition.SchemaName != "*" && definition.SchemaName != schemaName)
        {
          throw new InvalidOperationException($"The schema script {script.FullName} schema name {definition.SchemaName} does not match the expected schema name {schemaName}.");
        }

        if (!definition.Version.Equals(script.Version))
        {
          throw new InvalidOperationException($"The schema script {script.FullName} version {definition.Version} does not match the expected schema version {script.Version}.");
        }

        ExecuteTransaction(definition.UseTransaction, () =>
        {
          foreach (MigrationOperation operation in definition.Operations)
          {
            // Operations wrapped in a DatabaseProviderSpecificOperation can be returned as null if conditions are not met
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
        });
      }
      else
      {
        throw new InvalidOperationException($"Script {script.FullName} is not recognized.  Migration script names must end in '.sql' or '.json'. ");
      }

      // Add/Update the version number that was executed 
      UpdateSchemaVersion(schemaName, script.Version);

      return true;
    }

    private void ExecuteTransaction(Boolean useTransaction, Action action)
    {
      if (useTransaction)
      {
        IExecutionStrategy strategy = this.DbContext.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {
          using IDbContextTransaction transaction = this.DbContext.Database.BeginTransaction();
          try
          {
            action.Invoke();

            transaction.Commit();
          }
          catch (Exception)
          {
            transaction.Rollback();
            throw;
          }
        });
      }
      else
      {
        action.Invoke();
      }
    }

    /// <summary>
    /// Set required properties on the <paramref name="migrationOperation"/> to appropriate default values, based on other values set 
    /// by the migration script and/or the database type.
    /// </summary>
    /// <param name="migrationOperation"></param>
		private void ApplyCorrections(MigrationOperation migrationOperation)
    {
      if (migrationOperation.GetType() == typeof(CreateTableOperation))
      {
        CreateTableOperation createTableOperation = migrationOperation as CreateTableOperation;

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

        // Allow table to be omitted from primary key declaration in createTable (set the primary key table name to the table name of 
        // the parent object).
        if (createTableOperation.PrimaryKey != null)
        {
          if (createTableOperation.PrimaryKey.Table is null)
          {
            createTableOperation.PrimaryKey.Table = createTableOperation.Name;
          }
        }

        // Allow table to be omitted from foreign key declaration in createTable (set the foreign key table name to the table name of 
        // the parent object).
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

      if (migrationOperation.GetType() == typeof(AddColumnOperation))
      {
        ApplyCorrections(migrationOperation as AddColumnOperation);
      }

      if (migrationOperation.GetType() == typeof(DatabaseProviderSpecificOperation))
      {
        ApplyCorrections((migrationOperation as DatabaseProviderSpecificOperation).Operation);
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
          if (operation.DefaultValueSql == "1" || (Boolean.TryParse(operation.DefaultValueSql, out Boolean value1) && value1 == true))
          {
            operation.DefaultValueSql = "true";
          }
          else if (operation.DefaultValueSql == "0" || (Boolean.TryParse(operation.DefaultValueSql, out Boolean value2) && value2 == false) || !operation.IsNullable)
          {
            // set default value to false if "0" or "false" is specified, or if nothing is specified and the column does not allow nulls
            operation.DefaultValueSql = "false";
          }
        }
        else
        {
          // For all of the other database providers, booleans are "1" or "0" rather than "true" or "false"
          if (operation.DefaultValueSql == "1" || (Boolean.TryParse(operation.DefaultValueSql, out Boolean value1) && value1 == true))
          {
            operation.DefaultValueSql = "1";
          }
          else if (operation.DefaultValueSql == "0" || (Boolean.TryParse(operation.DefaultValueSql, out Boolean value2) && value2 == false) || !operation.IsNullable)
          {
            // set default value to false if "0" or "false" is specified, or if nothing is specified and the column does not allow nulls
            operation.DefaultValueSql = "0";
          }
        }
      }
    }

    /// <summary>
    /// Execute the command specified as string in <paramref name="command"/>.
    /// </summary>
    /// <param name="script"></param>
    /// <param name="command"></param>
    /// <exception cref="InvalidOperationException"></exception>
		private void ExecuteCommand(DatabaseSchemaScript script, string command)
    {
      try
      {
        // deal with last line of the file being "GO" with no cr/lf characters following
        if (command.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
        {
          command = command[0..^2];
        }

        // Allow up to 4 minutes for each migration command, and reset back to the default command timeout after executing
        int? timeout = this.DbContext.Database.GetCommandTimeout();
        this.DbContext.Database.SetCommandTimeout(240);

        this.DbContext.Database.ExecuteSqlRaw(command);

        this.DbContext.Database.SetCommandTimeout(timeout);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Migration script error [{script.FullName}]: {command}", ex);
      }
    }

    /// <summary>
    /// Execute the command represented as a MigrationOperation object in <paramref name="operation"/>.
    /// </summary>
    /// <param name="script"></param>
    /// <param name="operation"></param>
    /// <exception cref="InvalidOperationException"></exception>
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
          // index already exists
          this?.Logger.LogInformation("Index {0} already exists, create index operation skipped.", (operation as CreateIndexOperation).Name);
          return;
        }
      }

      if (operation is DropIndexOperation)
      {
        if (!DatabaseObjectExists((operation as DropIndexOperation).Name, DatabaseObjectTypes.Index))
        {
          // index does not exist 
          this?.Logger.LogInformation("Index {0} does not exist, drop index operation skipped.", (operation as DropIndexOperation).Name);
          return;
        }
      }

      IMigrationsSqlGenerator sqlGenerator = this.DbContext.GetInfrastructure().GetService<IMigrationsSqlGenerator>();

      foreach (MigrationCommand migrationCommand in sqlGenerator.Generate(new List<MigrationOperation>() { operation }, null, MigrationsSqlGenerationOptions.NoTransactions))
      {
        try
        {
          IRelationalConnection connection = this.DbContext.GetInfrastructure().GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalConnection>();

          // Allow up to 4 minutes for migration commands, and reset back to the default command timeout after executing
          int? timeout = connection.CommandTimeout;
          connection.CommandTimeout = 240;

          migrationCommand.ExecuteNonQuery(connection, null);

          connection.CommandTimeout = timeout;
        }
        catch (Exception ex)
        {
          throw new InvalidOperationException($"Migration script error [{script.FullName}]: {migrationCommand.CommandText} {ex.Message}", ex);
        }
      }
    }
  }
}
