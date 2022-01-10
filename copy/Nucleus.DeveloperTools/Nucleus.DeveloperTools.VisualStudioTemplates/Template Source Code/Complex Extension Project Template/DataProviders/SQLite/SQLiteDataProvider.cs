using System;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Microsoft.Data.Sqlite;
using Nucleus.Core.DataProviders;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Core.Authorization;
using Nucleus.Core;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Nucleus.Core.DataProviders.Abstractions.SQLiteDataProvider, I$nucleus_extension_name$DataProvider
	{
		private const string SCRIPT_NAMESPACE = "$nucleus_extension_namespace$.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "$nucleus_extension_namespace$.$nucleus_extension_name$";

		private IHttpContextAccessor HttpContextAccessor { get; }
		private EventDispatcher EventManager { get; }
		private ListManager ListManager { get; }

		public SQLiteDataProvider(IHttpContextAccessor httpContextAccessor, EventDispatcher eventManager, SQLiteDataProviderOptions options, ListManager listManager, ILogger<SQLiteDataProvider> logger) : base(options, logger)
		{
			this.HttpContextAccessor = httpContextAccessor;
			this.EventManager = eventManager;
			this.ListManager = listManager;
		}

		public override string SchemaName
		{
			get { return SCHEMA_NAME; }
		}

		public override IList<DatabaseSchemaScript> SchemaScripts
		{
			get
			{
				List<DatabaseSchemaScript> scripts = new List<DatabaseSchemaScript>();

				foreach (string script in this.GetType().Assembly.GetManifestResourceNames().Where
				(
					name => name.ToLower().EndsWith(".sql") && name.StartsWith(SCRIPT_NAMESPACE, StringComparison.OrdinalIgnoreCase)
				))
				{
					scripts.Add(BuildManifestSchemaScript(SCRIPT_NAMESPACE, script));
				}

				return scripts;
			}
		}

		private Guid CurrentUserId()
		{
			System.Security.Claims.ClaimsPrincipal user = this.HttpContextAccessor.HttpContext.User;
			return user.GetUserId();
		}

		#region "    Database schema checking and updates    "
		/// <summary>
		/// Read Schema.SchemaVersion and compare to the latest version of embedded database scripts.  If a script with a later version exists,
		/// run the script to update the database schema.
		/// </summary>
		public void CheckDatabaseSchema()
		{
			List<DatabaseSchemaScript> scripts = new List<DatabaseSchemaScript>();

			IEnumerable<string> scriptResourceNames = this.GetType().Assembly.GetManifestResourceNames().Where
			(
				name => name.ToLower().EndsWith(".sql") && name.StartsWith(SCRIPT_NAMESPACE, StringComparison.OrdinalIgnoreCase)
			);

			foreach (string scriptName in scriptResourceNames)
			{
				Logger.LogTrace("Adding script {0}.", scriptName);
				scripts.Add(BuildManifestSchemaScript(SCRIPT_NAMESPACE, scriptName));
			}

			base.CheckDatabaseSchema(SCHEMA_NAME, scripts);
		}

		#endregion

		public $nucleus_extension_name$ Get(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM $nucleus_extension_name$ " +
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
					$nucleus_extension_name$ result =  ModelExtensions.Create<$nucleus_extension_name$>(reader);					
					return result;
				}
				else
				{
					return null;
				}
			}
			finally
			{
				reader.Close();
			}
		}

		public IList<$nucleus_extension_name$> List(PageModule pageModule)
		{
			IDataReader reader;
			List<$nucleus_extension_name$> results = new ();

			string commandText =
				"SELECT * " +
				"FROM $nucleus_extension_name$ " +
				"WHERE ModuleId = @ModuleId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", pageModule.Id)
				});

			try
			{
				while (reader.Read())
				{
					$nucleus_extension_name$ item = ModelExtensions.Create<$nucleus_extension_name$>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void Save(PageModule pageModule, $nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			$nucleus_extension_name$ current = Get($nucleus_extension_name_lcase$.Id);

			if (current == null)
			{
				$nucleus_extension_name_lcase$.Id = Add$nucleus_extension_name$(pageModule, $nucleus_extension_name_lcase$);
				this.EventManager.RaiseEvent<$nucleus_extension_name$, Create>($nucleus_extension_name_lcase$);
			}
			else
			{
				Update$nucleus_extension_name$($nucleus_extension_name_lcase$);
				this.EventManager.RaiseEvent<$nucleus_extension_name$, Update>($nucleus_extension_name_lcase$);
			}
		}

		public void Delete($nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM $nucleus_extension_name$ " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", $nucleus_extension_name_lcase$.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<$nucleus_extension_name$, Delete>($nucleus_extension_name_lcase$);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting $nucleus_extension_name$ {0}", $nucleus_extension_name_lcase$.Id);
				RollbackTransaction();
				throw;
			}

		}

		private Guid Add$nucleus_extension_name$(PageModule pageModule, $nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			if ($nucleus_extension_name_lcase$.Id == Guid.Empty) $nucleus_extension_name_lcase$.Id = Guid.NewGuid();
		
		  string commandText =
				"INSERT INTO $nucleus_extension_name$ " +
				"(Id, ModuleId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ModuleId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", $nucleus_extension_name_lcase$.Id),
					new SqliteParameter("@ModuleId", pageModule.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return $nucleus_extension_name_lcase$.Id;
		}

		private void Update$nucleus_extension_name$($nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			string commandText =
				"UPDATE $nucleus_extension_name$ " +
				"SET " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", $nucleus_extension_name_lcase$.Id),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}


	}
}
