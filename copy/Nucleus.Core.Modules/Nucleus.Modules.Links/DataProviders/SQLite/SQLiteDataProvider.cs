using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Nucleus.Modules.Links.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Nucleus.Core.DataProviders.Abstractions.SQLiteDataProvider, ILinksDataProvider
	{
		private const string SCRIPT_NAMESPACE = "Nucleus.Modules.Links.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "Nucleus.Modules.Links";

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

		public Link Get(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
					"SELECT * " +
					"FROM Links " +
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
					Link result = ModelExtensions.Create<Link>(reader);

					if (result != null)
					{
						result.Category = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "CategoryId"));

						switch (result.LinkType)
						{
							case LinkTypes.Url:
								result.LinkUrl = GetUrlLinkItem(result.Id);
								break;
							case LinkTypes.File:
								result.LinkFile = GetFileLinkItem(result.Id);								
								break;
							case LinkTypes.Page:
								result.LinkPage = GetPageLinkItem(result.Id);
								break;
						}
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

		private LinkUrl GetUrlLinkItem(Guid linkId)
		{
			IDataReader reader;
			
			if (linkId == Guid.Empty) return null;
						
			string commandText =
				"SELECT * " +
				"FROM LinkUrls " +
				"WHERE LinkId = @LinkId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId)
					});

			try
			{
				if (reader.Read())
				{
					LinkUrl result = ModelExtensions.Create<LinkUrl>(reader);
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

		private LinkFile GetFileLinkItem(Guid linkId)
		{
			IDataReader reader;

			if (linkId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM LinkFiles " +
				"WHERE LinkId = @LinkId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId)
					});

			try
			{
				if (reader.Read())
				{
					LinkFile result = new();
					result.File = new() { Id = DataHelper.GetGUID(reader, "FileId") };
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

		private LinkPage GetPageLinkItem(Guid linkId)
		{
			IDataReader reader;

			if (linkId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM LinkPages " +
				"WHERE LinkId = @LinkId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId)
					});

			try
			{
				if (reader.Read())
				{
					LinkPage result = new();
					result.Page = new() { Id = DataHelper.GetGUID(reader, "PageId") };
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

		public IList<Link> List(PageModule pageModule)
		{
			IDataReader reader;
			List<Link> results = new();

			string commandText =
					"SELECT * " +
					"FROM Links " +
					"WHERE ModuleId = @ModuleId " + 
					"ORDER BY SortOrder ";

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
					Link result = ModelExtensions.Create<Link>(reader);

					result.Category = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "CategoryId"));

					switch (result.LinkType)
					{
						case LinkTypes.Url:
							result.LinkUrl = GetUrlLinkItem(result.Id);
							break;
						case LinkTypes.File:
							result.LinkFile = GetFileLinkItem(result.Id);
							break;
						case LinkTypes.Page:
							result.LinkPage = GetPageLinkItem(result.Id);							
							break;
					}

					results.Add(result);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void Delete(Link link)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM Links " +
					"WHERE LinkId = @LinkId ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@LinkId", link.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Link, Delete>(link);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Link {0}", link.Id);
				RollbackTransaction();
				throw;
			}

		}

		public void Save(PageModule pageModule, Link link)
		{
			Link current = Get(link.Id);

			

			if (current == null)
			{
				link.Id = AddLink(pageModule, link);
				this.EventManager.RaiseEvent<Link, Create>(link);
			}
			else
			{
				UpdateLink(link);
				this.EventManager.RaiseEvent<Link, Update>(link);
			}

			
			switch (link.LinkType)
			{
				case LinkTypes.Url:
					SaveLinkItem(link.Id, link.LinkUrl);
					break;
				case LinkTypes.File:
					SaveLinkItem(link.Id, link.LinkFile);
					break;

				case LinkTypes.Page:
					SaveLinkItem(link.Id, link.LinkPage);
					break;				
			}
		}

		private Guid AddLink(PageModule pageModule, Link link)
		{
			if (link.Id == Guid.Empty) link.Id = Guid.NewGuid();

			link.SortOrder = GetTopLinkSortOrder(pageModule.Id) + 10;

			string commandText =
				"INSERT INTO Links " +
				"(Id, ModuleId, LinkType, Title, Description, SortOrder, CategoryId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ModuleId, @LinkType, @Title, @Description, @SortOrder, @CategoryId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", link.Id),
						new SqliteParameter("@ModuleId", pageModule.Id),
						new SqliteParameter("@LinkType", link.LinkType),
						new SqliteParameter("@Title", link.Title),
						new SqliteParameter("@Description", link.Description),
						new SqliteParameter("@SortOrder", link.SortOrder),
						new SqliteParameter("@CategoryId", link.Category?.Id),
						new SqliteParameter("@DateAdded", DateTime.UtcNow),
						new SqliteParameter("@AddedBy", CurrentUserId())
					});

			return link.Id;
		}

		private void UpdateLink(Link link)
		{
			string commandText =
					"UPDATE Links " +
					"SET " +
					"  Title = @Title, " +
					"  Description = @Description, " +
					"  SortOrder = @SortOrder, " + 
					"  LinkType = @LinkType, " +
					"  CategoryId = @CategoryId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", link.Id),
						new SqliteParameter("@Title", link.Title),
						new SqliteParameter("@Description", link.Description),
						new SqliteParameter("@SortOrder", link.SortOrder),
						new SqliteParameter("@LinkType", link.LinkType),
						new SqliteParameter("@CategoryId", link.Category?.Id),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		private long GetTopLinkSortOrder(Guid moduleId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM Links " +
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

		public void SaveLinkItem(Guid linkId, LinkUrl linkItem)
		{
			LinkUrl current;

			current = GetUrlLinkItem(linkId) as LinkUrl;

			if (current == null)
			{
				AddLinkItem(linkId, linkItem as LinkUrl);
			}
			else
			{
				UpdateLinkItem(linkId, linkItem as LinkUrl);
			}

		}


		private void AddLinkItem(Guid linkId, LinkUrl linkItem)
		{
			string commandText =
				"INSERT INTO LinkUrls " +
				"(LinkId, Url, DateAdded, AddedBy) " +
				"VALUES " +
				"(@LinkId, @Url, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@Url", linkItem.Url),
						new SqliteParameter("@DateAdded", DateTime.UtcNow),
						new SqliteParameter("@AddedBy", CurrentUserId())
					});

		}

		private void UpdateLinkItem(Guid linkId, LinkUrl linkItem)
		{
			string commandText =
					"UPDATE LinkUrls " +
					"SET " +
					"  Url = @Url, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE LinkId = @LinkId ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@Url", linkItem.Url),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		///

		public void SaveLinkItem(Guid linkId, LinkFile linkItem)
		{
			LinkFile current = GetFileLinkItem(linkId);

			if (current == null)
			{
				AddLinkItem(linkId, linkItem as LinkFile);
			}
			else
			{
				UpdateLinkItem(linkId, linkItem as LinkFile);
			}
		}


		private void AddLinkItem(Guid linkId, LinkFile linkItem)
		{			
			string commandText =
				"INSERT INTO LinkFiles " +
				"(LinkId, FileId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@LinkId, @FileId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@FileId", linkItem.File?.Id),
						new SqliteParameter("@DateAdded", DateTime.UtcNow),
						new SqliteParameter("@AddedBy", CurrentUserId())
					});

		}

		private void UpdateLinkItem(Guid linkId, LinkFile linkItem)
		{
			string commandText =
					"UPDATE LinkFiles " +
					"SET " +
					"  FileId = @FileId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE LinkId = @LinkId ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@FileId", linkItem.File?.Id),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		public void SaveLinkItem(Guid linkId, LinkPage linkItem)
		{
			LinkPage current = GetPageLinkItem(linkId);

			if (current == null)
			{
				AddLinkItem(linkId, linkItem as LinkPage);
			}
			else
			{
				UpdateLinkItem(linkId, linkItem as LinkPage);
			}

		}


		private void AddLinkItem(Guid linkId, LinkPage linkItem)
		{
			string commandText =
				"INSERT INTO LinkPages " +
				"(LinkId, PageId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@LinkId, @PageId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@PageId", linkItem.Page?.Id),
						new SqliteParameter("@DateAdded", DateTime.UtcNow),
						new SqliteParameter("@AddedBy", CurrentUserId())
					});

		}

		private void UpdateLinkItem(Guid linkId, LinkPage linkItem)
		{
			string commandText =
					"UPDATE LinkPages " +
					"SET " +
					"  PageId = @PageId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE LinkId = @LinkId ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@LinkId", linkId),
						new SqliteParameter("@PageId", linkItem.Page?.Id),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

	}
}
