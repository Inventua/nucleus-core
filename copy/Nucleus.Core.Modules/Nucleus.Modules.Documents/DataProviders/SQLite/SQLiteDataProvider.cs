using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
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
using Nucleus.Modules.Documents.Models;

namespace Nucleus.Modules.Documents.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Nucleus.Core.DataProviders.Abstractions.SQLiteDataProvider, IDocumentsDataProvider
	{
		private const string SCRIPT_NAMESPACE = "Nucleus.Modules.Documents.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "Core.Documents";

		private IHttpContextAccessor HttpContextAccessor { get; }
		private EventDispatcher EventManager { get; }
		private ListManager ListManager { get; }
		private FileSystemManager FileSystemManager { get; }

		public SQLiteDataProvider(IHttpContextAccessor httpContextAccessor, EventDispatcher eventManager, SQLiteDataProviderOptions options, ListManager listManager, FileSystemManager fileSystemManager, ILogger<SQLiteDataProvider> logger) : base(options, logger)
		{
			this.HttpContextAccessor = httpContextAccessor;
			this.EventManager = eventManager;
			this.ListManager = listManager;
			this.FileSystemManager = fileSystemManager;
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

		public Document Get(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM Documents " +
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
					Document result =  ModelExtensions.Create<Document>(reader);
					if (result != null)
					{
						result.File = new() { Id = DataHelper.GetGUID(reader, "FileId") };
						result.Category = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "CategoryId"));
					}
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

		public IList<Document> List(PageModule pageModule)
		{
			IDataReader reader;
			List<Document> results = new ();

			string commandText =
				"SELECT * " +
				"FROM Documents " +
				"WHERE ModuleId = @ModuleId " +
				"ORDER BY SortOrder ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter($"@ModuleId", pageModule.Id)
				});

			try
			{
				while (reader.Read())
				{
					Document item = ModelExtensions.Create<Document>(reader);
					
					item.File = new() { Id = DataHelper.GetGUID(reader, "FileId") };	
					item.Category = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "CategoryId"));

					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void Save(PageModule pageModule, Document Document)
		{
			Document current = Get(Document.Id);

			if (current == null)
			{
				Document.Id = AddDocument(pageModule, Document);
				this.EventManager.RaiseEvent<Document, Create>(Document);
			}
			else
			{
				UpdateDocument(Document);
				this.EventManager.RaiseEvent<Document, Update>(Document);
			}
		}

		public void Delete(Document document)
		{
			try
			{
				ExecuteNonQuery("BEGIN TRANSACTION;", null);

				string commandTextPermissions =
					"DELETE " +
					"FROM Documents " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", document.Id)
						});

				ExecuteNonQuery("COMMIT;", null);

				this.EventManager.RaiseEvent<Document, Delete>(document);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Document {0}", document.Id);
				ExecuteNonQuery("ROLLBACK;", null);
				throw;
			}

		}



		private long GetTopDocumentSortOrder(Guid moduleId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM Documents " +
				"WHERE ModuleId = @ModuleId " +
				"ORDER BY SortOrder DESC " +
				"LIMIT 1; ";

			topSortOrder = this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", moduleId)
				});

			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		private Guid AddDocument(PageModule pageModule, Document document)
		{
			if (document.Id == Guid.Empty) document.Id = Guid.NewGuid();

			document.SortOrder = GetTopDocumentSortOrder(pageModule.Id) + 10;

			string commandText =
				"INSERT INTO Documents " +
				"(Id, ModuleId, Title, Description, CategoryId, SortOrder, FileId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ModuleId, @Title, @Description, @CategoryId, @SortOrder, @FileId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", document.Id),
					new SqliteParameter("@ModuleId", pageModule.Id),
					new SqliteParameter("@Title", document.Title),
					new SqliteParameter("@Description", document.Description),
					new SqliteParameter("@CategoryId", document.Category?.Id),
					new SqliteParameter("@SortOrder", document.SortOrder),
					new SqliteParameter("@FileId", document.File?.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return document.Id;
		}

		private void UpdateDocument(Document document)
		{
			// Note, Document Type is not included in the update.  Document types cannot be changed by users.
			string commandText =
				"UPDATE Documents " +
				"SET " +
				"  Title = @Title, " +
				"  Description = @Description, " +
				"  CategoryId = @CategoryId, " +
				"  SortOrder = @SortOrder, " +
				"  FileId = @FileId, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", document.Id),
					new SqliteParameter("@Title", document.Title),
					new SqliteParameter("@Description", document.Description),
					new SqliteParameter("@CategoryId", document.Category?.Id),
					new SqliteParameter("@SortOrder", document.SortOrder),
					new SqliteParameter("@FileId", document.File?.Id),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}


	}
}
