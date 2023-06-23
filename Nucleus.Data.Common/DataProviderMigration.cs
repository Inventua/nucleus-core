using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Typed DataProviderMigration.  This class is used to add typed instances of DataProviderMigration to the dependency injection container.
	/// </summary>
	/// <typeparam name="TDataProvider">
	/// Data provider class.
	/// </typeparam>
	public abstract class DataProviderMigration<TDataProvider> : DataProviderMigration
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <remarks>
		/// This constructor sets the schema name, embedded script namespace and scripts assembly using conventions.  The schema name
		/// is derived from the specified <typeparamref name="TDataProvider"/> namespace: It is set to the part of the namespace before 
		/// ".DataProvider".  The script namespaces list is set to the specified <typeparamref name="TDataProvider"/> namespace with the specified
		/// by <paramref name="scriptFolderNames"/> appended, followed by /Scripts.  The scripts assembly is set to the assembly which contains
		/// the <typeparamref name="TDataProvider"/> class.
		/// </remarks>
		public DataProviderMigration(string[] scriptFolderNames)
		{
			this.SchemaScriptsNamespaces =
				scriptFolderNames.Select(scriptFolderName => $"{typeof(TDataProvider).Namespace}.{scriptFolderName}.Scripts.");

			this.SchemaScriptsAssembly = typeof(TDataProvider).Assembly;
		}

		/// <summary>
		/// Constructor which allows the caller to specify the source of schema files.
		/// </summary>	
		/// <param name="absoluteSchemaScriptsNamespaces">
		/// Fully-qualified namespace for embedded schema files, including the /Scripts folder.
		/// </param>
		/// <param name="schemaScriptsAssembly">
		/// Assembly to retrieve embedded schemas from.
		/// </param>
		public DataProviderMigration(string[] absoluteSchemaScriptsNamespaces, System.Reflection.Assembly schemaScriptsAssembly)
		{
			this.SchemaScriptsNamespaces = absoluteSchemaScriptsNamespaces;
			this.SchemaScriptsAssembly = schemaScriptsAssembly;
		}
	}

	/// <summary>
	/// Provides a migration implementation for the database provider specified by T.
	/// </summary>
	public abstract class DataProviderMigration
	{
		/// <summary>
		/// Database object type enum for use by the DatabaseObjectExists method.
		/// </summary>
		public enum DatabaseObjectTypes
		{
			/// <summary>
			/// Table
			/// </summary>
			Table,
			/// <summary>
			/// Index
			/// </summary>
			Index
		}

		/// <summary>
		/// Namespace of the embedded schema scripts.
		/// </summary>
		/// <remarks>
		/// The default value for SchemaScriptsNamespace is the namespace of the class which inherits this class, followed by
		/// the database type, or 'Migrations' for database-agnostic screens, with a ".Scripts." suffix.  Add your scripts to the
		/// DataProvider/database-type/scripts folder where database-type is 'Migrations', or the name of the data provider that the
		/// script is for (Sqlite, MySql, SqlServer or PostgreSql).
		/// Scripts must have a ".sql" extension or a ".json" extension.  This value is not case-sensitive.
		/// </remarks>
		public virtual IEnumerable<string> SchemaScriptsNamespaces { get; internal set; }

		/// <summary>
		/// Identifies the assembly which contains embedded database migration scripts.  The default value is the assembly which contains
		/// your DataProviderMigration implementation.
		/// </summary>
		public virtual System.Reflection.Assembly SchemaScriptsAssembly { get; internal set; }

		/// <summary>
		/// Retrieves a list of embedded data migration scripts.  
		/// </summary>
		public virtual IList<DatabaseSchemaScript> SchemaScripts
		{
			get
			{
				List<DatabaseSchemaScript> scripts = new();

				foreach (string schemaScriptsNamespace in this.SchemaScriptsNamespaces)
				{
					foreach (string script in this.SchemaScriptsAssembly.GetManifestResourceNames().Where
					(
						name =>
							(name == "*" || name.StartsWith(schemaScriptsNamespace, StringComparison.OrdinalIgnoreCase))
							&&
							(name.ToLower().EndsWith(".sql") || name.ToLower().EndsWith(".json"))
					))
					{
						if (Nucleus.Data.Common.DatabaseSchemaScript.CanParseVersion(schemaScriptsNamespace, script))
						{
							scripts.Add(Nucleus.Data.Common.DatabaseSchemaScript.BuildManifestSchemaScript(this.SchemaScriptsAssembly, schemaScriptsNamespace, script));
						}
					}
				}

				return scripts;
			}
		}

		/// <summary>
		/// Compare the stored schema version from the database with the versions of the available schema scripts retrieved by .SchemaScripts.  If
		/// any have not been applied, apply them in order (sorted by version, lowest-to-highest)
		/// </summary>
		public abstract void CheckDatabaseSchema();

		/// <summary>
		/// Retrieve the version number of the latest schema update that has been applied for the specified schema name.  
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		/// <remarks>
		/// If no schema entry exists for the specified schema name, this function returns version 0.0.0.0.
		/// </remarks>
		abstract public System.Version GetSchemaVersion(string schemaName);

		/// <summary>
		/// Execute all scripts which have a version greater than the current schema version.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="currentSchemaVersion"></param>
		/// <param name="scripts"></param>
		abstract public void RunDatabaseScripts(string schemaName, System.Version currentSchemaVersion, IList<DatabaseSchemaScript> scripts);

		/// <summary>
		/// Checks whether the specified database object exists.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		abstract public Boolean DatabaseObjectExists(string name, DatabaseObjectTypes type);
	}
}
