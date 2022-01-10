using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Core.DataProviders.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

using System.Data;
using Microsoft.Data.Sqlite;
using Nucleus.Core.DataProviders;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Core.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Abstractions.SQLiteDataProvider, ILayoutDataProvider, IUserDataProvider, IPermissionsDataProvider, ISessionDataProvider, IMailDataProvider, IScheduledTaskDataProvider, IFileSystemDataProvider, IListDataProvider
	{
		private const string SCRIPT_NAMESPACE = "Nucleus.Core.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "Nucleus.Core";

		private IHttpContextAccessor HttpContextAccessor { get; }
		private EventDispatcher EventManager { get; }

		public SQLiteDataProvider(IHttpContextAccessor httpContextAccessor, EventDispatcher eventManager, Abstractions.SQLiteDataProviderOptions options, ILogger<SQLiteDataProvider> logger) : base(options, logger)
		{
			this.HttpContextAccessor = httpContextAccessor;
			this.EventManager = eventManager;
		}

		public override string SchemaName
		{
			get { return SCHEMA_NAME; }
		}

		public override IList<DatabaseSchemaScript> SchemaScripts
		{
			get
			{
				List<DatabaseSchemaScript> scripts = new();

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





		#region "    Site methods    "

		public Site GetSite(Guid siteId)
		{
			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM Sites " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", siteId)
					});

			try
			{
				if (reader.Read())
				{
					Site result = ModelExtensions.Create<Site>(reader);
					result.DefaultLayout = GetLayoutDefinition(DataHelper.GetGUID(reader, "DefaultLayoutDefinitionId"));
					result.RegisteredUsersRole = GetRole(DataHelper.GetGUID(reader, "RegisteredUsersRoleId"));
					result.AdministratorsRole = GetRole(DataHelper.GetGUID(reader, "AdministratorsRoleId"));
					result.AnonymousUsersRole = GetRole(DataHelper.GetGUID(reader, "AnonymousUsersRoleId"));
					result.AllUsersRole = GetRole(DataHelper.GetGUID(reader, "AllUsersRoleId"));
					result.UserProfileProperties = ListSiteUserProfileProperties(result.Id);
					result.SiteSettings = ListSiteSettings(result.Id);
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


		public Dictionary<string, string> ListSiteSettings(Guid siteId)
		{
			IDataReader reader;
			Dictionary<string, string> results = new();

			string commandText =
				"SELECT * " +
				"FROM SiteSettings " +
				"WHERE SiteId = @SiteId";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(DataHelper.GetString(reader, "SettingName"), DataHelper.GetString(reader, "SettingValue"));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void SaveSiteSettings(Guid siteId, Dictionary<string, string> siteSettings)
		{
			Site current = GetSite(siteId);

			if (current != null)
			{
				foreach (KeyValuePair<string, string> setting in siteSettings)
				{
					if (current.SiteSettings.ContainsKey(setting.Key))
					{
						UpdateSiteSetting(siteId, setting);
					}
					else
					{
						AddSiteSetting(siteId, setting);
					}
				}

				this.EventManager.RaiseEvent<Site, Update>(current);
			}
		}

		/// <summary>
		/// Create a new SiteSetting record
		/// </summary>
		/// <param name="SiteSetting"></param>
		private void AddSiteSetting(Guid siteId, KeyValuePair<string, string> setting)
		{
			string commandText =
				"INSERT INTO SiteSettings " +
				"(SiteId, SettingName, SettingValue, DateAdded, AddedBy) " +
				"VALUES " +
				"(@SiteId, @SettingName, @SettingValue, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@SettingName", setting.Key),
					new SqliteParameter("@SettingValue", setting.Value),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		/// <summary>
		/// Update an existing SiteSetting record
		/// </summary>
		/// <param name="SiteSetting"></param>
		private void UpdateSiteSetting(Guid siteId, KeyValuePair<string, string> setting)
		{
			string commandText =
				"UPDATE SiteSettings " +
				"SET " +
				"  SettingValue = @SettingValue, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE SiteId = @SiteId " +
				"AND SettingName = @SettingName ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@SettingName", setting.Key),
					new SqliteParameter("@SettingValue", setting.Value),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		public void DeleteSite(Site site)
		{
			try
			{
				BeginTransaction();

				string commandTextPreDelete =
					"UPDATE Sites " +
					"SET AdministratorsRoleID = null, RegisteredUsersRoleId = null, AnonymousUsersRole = null, AllUsersRoleId = null " +
					"WHERE Id = @Id ";

				string commandText =
					"DELETE " +
					"FROM Sites " +
					"WHERE Id = @Id ";

				string commandTextSettings =
						"DELETE " +
						"FROM SiteSettings " +
						"WHERE SiteId = @SiteId ";

				this.ExecuteNonQuery(
					commandTextSettings,
						new SqliteParameter[]
						{
						new SqliteParameter("@SiteId", site.Id)
						});

				this.ExecuteNonQuery(
					commandTextPreDelete,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", site.Id)
						});

				foreach (Page page in ListPages(site.Id))
				{
					DeletePage(page);
				}

				foreach (User user in ListUsers(site))
				{
					DeleteUser(user);
				}

				foreach (Role role in ListRoles(site))
				{
					DeleteRole(role);
				}

				foreach (RoleGroup group in ListRoleGroups(site))
				{
					DeleteRoleGroup(group);
				}

				foreach (SiteAlias alias in ListSiteAliases(site.Id))
				{
					DeleteSiteAlias(alias.Id);
				}

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", site.Id)
						});

				CommitTransaction();
				this.EventManager.RaiseEvent<Site, Delete>(site);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting site {0}", site.Id);
				RollbackTransaction();
				throw;
			}
		}

		public Guid DetectSite(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase)
		{
			string result;
			string siteAlias = requestUri.Value + pathBase;

			string commandText =
				"SELECT Id " +
				"FROM Sites " +
				"WHERE Id IN (SELECT SiteId FROM SiteAlias WHERE Alias = @SiteAlias) " +
				"LIMIT 1 ";

			result = (string)this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@SiteAlias", siteAlias)
					});

			if (result == null)
			{
				return Guid.Empty;
			}
			else
			{
				return Guid.Parse(result.ToString());
			}
		}

		public List<Site> ListSites()
		{
			return ListItems<Site>("Sites");
		}

		public IEnumerable<UserProfileProperty> ListSiteUserProfileProperties(Guid siteId)
		{
			IDataReader reader;
			List<UserProfileProperty> results = new();

			if (siteId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM UserProfileProperties " +
				"WHERE SiteId = @SiteId " +
				"ORDER BY SortOrder ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<UserProfileProperty>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void SaveSite(Site site)
		{
			Site current = GetSite(site.Id);

			if (current == null)
			{
				site.Id = AddSite(site);
				this.EventManager.RaiseEvent<Site, Create>(site);
			}
			else
			{
				UpdateSite(site);
				this.EventManager.RaiseEvent<Site, Update>(site);
			}

			//foreach (SiteAlias alias in site.Aliases)
			//{
			//	SaveSiteAlias(site.Id, alias);
			//}

			//SaveSiteUserProfileProperties(site.UserProfileProperties);

			SaveSiteSettings(site.Id, site.SiteSettings);
		}


		private Guid AddSite(Site site)
		{
			if (site.Id == Guid.Empty) site.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Sites " +
				"(Id, Name, SiteGroupId, DefaultLayoutDefinitionId, HomeDirectory, AdministratorsRoleId, RegisteredUsersRoleId, AnonymousUsersRoleId, AllUsersRoleId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @Name, @SiteGroupId, @DefaultLayoutDefinitionId, @HomeDirectory, @AdministratorsRoleId, @RegisteredUsersRoleId, @AnonymousUsersRoleId, @AllUsersRoleId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", site.Id),
					new SqliteParameter("@Name", site.Name),
					new SqliteParameter("@SiteGroupId", site.SiteGroupId),
					new SqliteParameter("@DefaultLayoutDefinitionId", site.DefaultLayout.Id),
					new SqliteParameter("@HomeDirectory", site.HomeDirectory),
					new SqliteParameter("@AdministratorsRoleId", site.AdministratorsRole.Id),
					new SqliteParameter("@RegisteredUsersRoleId", site.RegisteredUsersRole.Id),
					new SqliteParameter("@AnonymousUsersRoleId", site.AnonymousUsersRole.Id),
					new SqliteParameter("@AllUsersRoleId", site.AllUsersRole.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return site.Id;
		}

		private void UpdateSite(Site site)
		{
			// Note, Site Type is not included in the update.  Site types cannot be changed by users.
			string commandText =
				"UPDATE Sites " +
				"SET " +
				"  Name = @Name, " +
				"  SiteGroupId = @SiteGroupId, " +
				"  DefaultLayoutDefinitionId = @DefaultLayoutDefinitionId, " +
				"  HomeDirectory = @HomeDirectory, " +
				"  AdministratorsRoleId = @AdministratorsRoleId, " +
				"  RegisteredUsersRoleId = @RegisteredUsersRoleId, " +
				"  AnonymousUsersRoleId = @AnonymousUsersRoleId, " +
				"  AllUsersRoleId = @AllUsersRoleId, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", site.Id),
					new SqliteParameter("@Name", site.Name),
					new SqliteParameter("@SiteGroupId", site.SiteGroupId),
					new SqliteParameter("@DefaultLayoutDefinitionId", site.DefaultLayout.Id),
					new SqliteParameter("@HomeDirectory", site.HomeDirectory),
					new SqliteParameter("@AdministratorsRoleId", site.AdministratorsRole.Id),
					new SqliteParameter("@RegisteredUsersRoleId", site.RegisteredUsersRole.Id),
					new SqliteParameter("@AnonymousUsersRoleId", site.AnonymousUsersRole.Id),
					new SqliteParameter("@AllUsersRoleId", site.AllUsersRole.Id),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public List<SiteAlias> ListSiteAliases(Guid siteId)
		{
			IDataReader reader;
			List<SiteAlias> results = new();
			SiteAlias result;

			string commandText =
				"SELECT * " +
				"FROM SiteAlias " +
				"WHERE SiteId = @SiteId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId)
				});

			try
			{
				while (reader.Read())
				{
					result = ModelExtensions.Create<SiteAlias>(reader);

					results.Add(result);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public SiteAlias GetSiteAlias(Guid id)
		{
			return GetItem<SiteAlias>("SiteAlias", id);
		}

		public void SaveSiteAlias(Guid siteId, SiteAlias alias)
		{
			SiteAlias current = GetSiteAlias(alias.Id);

			if (current == null)
			{
				alias.Id = AddSiteAlias(siteId, alias);
			}
			else
			{
				UpdateSiteAlias(alias);
			}
		}

		private Guid AddSiteAlias(Guid siteId, SiteAlias siteAlias)
		{
			if (siteAlias.Id == Guid.Empty) siteAlias.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO SiteAlias " +
				"(Id, SiteId, Alias, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @Alias, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", siteAlias.Id),
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@Alias", siteAlias.Alias),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return siteAlias.Id;
		}

		private void UpdateSiteAlias(SiteAlias siteAlias)
		{
			// Note, SiteAlias Type is not included in the update.  SiteAlias types cannot be changed by users.
			string commandText =
				"UPDATE SiteAlias " +
				"SET " +
				"  Alias = @Alias, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", siteAlias.Id),
					new SqliteParameter("@Alias", siteAlias.Alias),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public void DeleteSiteAlias(Guid id)
		{
			string commandText =
				"DELETE FROM SiteAlias " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", id)
				});
		}

		#endregion

		#region "    Page methods    "

		public Page GetPage(Guid pageId)
		{
			Page result;

			IDataReader reader;

			if (pageId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM Pages " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", pageId)
					});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<Page>(reader);

					if (result != null)
					{
						result.Layout = GetLayoutDefinition(DataHelper.GetGUID(reader, "LayoutDefinitionId"));
						result.Modules = ListPageModules(result.Id);
						result.Routes = ListPageRoutes(result);
						result.Permissions = ListPagePermissions(result.Id);

						result.IsFirst = GetFirstPageSortOrder(result.ParentId) == result.SortOrder;
						result.IsLast = GetLastPageSortOrder(result.ParentId) == result.SortOrder;
					}
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


			return result;
		}

		public Guid GetPageSiteId(Page page)
		{
			string result;

			string commandText =
				"SELECT SiteId " +
				"FROM Pages " +
				"WHERE Id = @PageId ";

			result = (string)this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@PageId", page.Id)
				});

			if (String.IsNullOrEmpty(result))
			{
				return Guid.Empty;
			}
			else
			{
				return Guid.Parse(result);
			}
		}

		public Guid GetPageModulePageId(PageModule site)
		{
			string result;

			string commandText =
				"SELECT PageId " +
				"FROM PageModules " +
				"WHERE Id = @ModuleId ";

			result = (string)this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", site.Id)
				});

			if (String.IsNullOrEmpty(result))
			{
				return Guid.Empty;
			}
			else
			{
				return Guid.Parse(result);
			}
			//return GetPage(pageId);
		}

		public Guid FindPage(Site site, string path)
		{
			string result;

			if (String.IsNullOrEmpty(path)) return Guid.Empty;

			string commandText =
				"SELECT Pages.Id " +
				"FROM Pages, PageRoutes " +
				"WHERE PageRoutes.Path = @Path " +
				"AND Pages.SiteId = @SiteId " +
				"AND PageRoutes.PageId = Pages.Id " +
				"LIMIT 1; ";

			result = (string)this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Path", path)
				});

			if (result == null)
			{
				return Guid.Empty;
			}
			else
			{
				return Guid.Parse(result);
			}
		}

		public List<Page> ListPages(Guid siteId)
		{
			return ListItems<Page>(siteId, "SiteId", "Pages");
		}

		public List<Page> ListPages(Guid siteId, Guid parentId)
		{
			IDataReader reader;
			List<Page> results = new();

			string commandText;

			if (parentId == Guid.Empty)
			{
				commandText =
					"SELECT * " +
					"FROM Pages " +
					"WHERE SiteId = @SiteId " +
					"AND ParentId IS NULL " +
					"ORDER BY SortOrder ";

				reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId)
				});
			}
			else
			{
				commandText =
					"SELECT * " +
					"FROM Pages " +
					"WHERE SiteId = @SiteId " +
					"AND ParentId = @ParentId " +
					"ORDER BY SortOrder ";

				reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@ParentId", parentId)
				});
			}

			try
			{
				while (reader.Read())
				{
					Page page = ModelExtensions.Create<Page>(reader);
					page.Routes = ListPageRoutes(page);
					page.Permissions = ListPagePermissions(page.Id);
					page.Layout = GetLayoutDefinition(DataHelper.GetGUID(reader, "LayoutDefinitionId"));

					page.IsFirst = GetFirstPageSortOrder(page.ParentId) == page.SortOrder;
					page.IsLast = GetLastPageSortOrder(page.ParentId) == page.SortOrder;

					results.Add(page);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;

		}

		public List<Page> SearchPages(Guid siteId, string searchTerm)
		{
			IDataReader reader;
			List<Page> results = new();

			string commandText =
				"SELECT * " +
				"FROM Pages " +
				"WHERE SiteId = @SiteId " +
				"AND (Name LIKE @SearchTerm " +
				"OR Title LIKE @SearchTerm " +
				"OR Description  LIKE @SearchTerm " + "" +
				"OR Keywords LIKE @SearchTerm) ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@SearchTerm", $"%{searchTerm}%"),
				});

			try
			{
				while (reader.Read())
				{
					Page item = ModelExtensions.Create<Page>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public List<PageRoute> ListPageRoutes(Page page)
		{
			IDataReader reader;
			List<PageRoute> results = new();

			string commandText =
				"SELECT * " +
				"FROM PageRoutes " +
				"WHERE PageId = @PageId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@PageId", page.Id)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<PageRoute>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void SavePage(Site site, Page page)
		{
			Page current = GetPage(page.Id);

			if (current == null)
			{
				page.Id = AddPage(site, page);
				this.EventManager.RaiseEvent<Page, Create>(page);
			}
			else
			{
				if (current.ParentId != page.ParentId)
				{
					// the user has moved this page to a different parent, reset sort order to prevent collisions
					page.SortOrder = GetLastPageSortOrder(page.ParentId);
				}

				UpdatePage(page);
				this.EventManager.RaiseEvent<Page, Update>(page);
			}

			SavePageRoutes(site, page, current?.Routes);
			SavePagePermissions(page.Id, page.Permissions, current?.Permissions);
		}

		private long GetFirstPageSortOrder(Guid? parentId)
		{
			object topSortOrder;

			string commandText;

			if (!parentId.HasValue || parentId == Guid.Empty)
			{
				commandText =
					"SELECT SortOrder " +
					"FROM Pages " +
					"WHERE ParentId IS NULL " +
					"ORDER BY SortOrder " +
					"LIMIT 1; ";

				topSortOrder = this.ExecuteScalar(
				commandText,
				Array.Empty<SqliteParameter>());
			}
			else
			{
				commandText =
					"SELECT SortOrder " +
					"FROM Pages " +
					"WHERE ParentId = @ParentId " +
					"ORDER BY SortOrder " +
					"LIMIT 1; ";

				topSortOrder = this.ExecuteScalar(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ParentId", parentId)
					});
			}


			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		private long GetLastPageSortOrder(Guid? parentId)
		{
			object topSortOrder;

			string commandText;

			if (!parentId.HasValue || parentId == Guid.Empty)
			{
				commandText =
					"SELECT SortOrder " +
					"FROM Pages " +
					"WHERE ParentId IS NULL " +
					"ORDER BY SortOrder DESC " +
					"LIMIT 1; ";

				topSortOrder = this.ExecuteScalar(
				commandText,
				Array.Empty<SqliteParameter>());
			}
			else
			{
				commandText =
					"SELECT SortOrder " +
					"FROM Pages " +
					"WHERE ParentId = @ParentId " +
					"ORDER BY SortOrder DESC " +
					"LIMIT 1; ";

				topSortOrder = this.ExecuteScalar(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ParentId", parentId)
					});
			}


			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		/// <summary>
		/// Create a new page record
		/// </summary>
		/// <param name="page"></param>
		private Guid AddPage(Site site, Page page)
		{
			if (page.Id == Guid.Empty) page.Id = Guid.NewGuid();

			page.SortOrder = GetFirstPageSortOrder(page.ParentId) + 10;

			string commandText =
				"INSERT INTO Pages " +
				"(Id, ParentId, SiteId, Name, Title, Description, Keywords, DefaultPageRouteId, Disabled, ShowInMenu, DisableInMenu, LayoutDefinitionId, SortOrder, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ParentId, @SiteId, @Name, @Title, @Description, @Keywords, @DefaultPageRouteId, @Disabled, @ShowInMenu, @DisableInMenu, @LayoutDefinitionId, @SortOrder, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", page.Id),
					new SqliteParameter("@ParentId", page.ParentId),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Name", page.Name),
					new SqliteParameter("@Title", page.Title),
					new SqliteParameter("@Description", page.Description),
					new SqliteParameter("@Keywords", page.Keywords),
					new SqliteParameter("@DefaultPageRouteId", page.DefaultPageRouteId),
					new SqliteParameter("@Disabled", page.Disabled),
					new SqliteParameter("@ShowInMenu", page.ShowInMenu),
					new SqliteParameter("@DisableInMenu", page.DisableInMenu),
					new SqliteParameter("@LayoutDefinitionId", page.Layout.Id),
					new SqliteParameter("@SortOrder", page.SortOrder),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return page.Id;
		}

		/// <summary>
		/// Update an existing page record
		/// </summary>
		/// <param name="page"></param>
		private void UpdatePage(Page page)
		{
			string commandText =
				"UPDATE Pages " +
				"SET " +
				"  ParentId = @ParentId, " +
				"  Name = @Name, " +
				"  Title = @Title, " +
				"  Name = @Name, " +
				"  Description = @Description, " +
				"  Keywords = @Keywords, " +
				"  DefaultPageRouteId = @DefaultPageRouteId, " +
				"  Disabled = @Disabled, " +
				"  ShowInMenu = @ShowInMenu, " +
				"  DisableInMenu = @DisableInMenu, " +
				"  LayoutDefinitionId = @LayoutDefinitionId, " +
				"  SortOrder = @SortOrder, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", page.Id),
					new SqliteParameter("@ParentId", page.ParentId),
					new SqliteParameter("@Name", page.Name),
					new SqliteParameter("@Title", page.Title),
					new SqliteParameter("@Description", page.Description),
					new SqliteParameter("@Keywords", page.Keywords),
					new SqliteParameter("@DefaultPageRouteId", page.DefaultPageRouteId),
					new SqliteParameter("@Disabled", page.Disabled),
					new SqliteParameter("@ShowInMenu", page.ShowInMenu),
					new SqliteParameter("@DisableInMenu", page.DisableInMenu),
					new SqliteParameter("@LayoutDefinitionId", page.Layout==null ? DBNull.Value : page.Layout.Id),
					new SqliteParameter("@SortOrder", page.SortOrder),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		void SavePageRoutes(Site site, Page page, List<PageRoute> originalUrls)
		{

			if (originalUrls != null)
			{
				foreach (PageRoute originalPageRoute in originalUrls)
				{
					Boolean found = false;

					foreach (PageRoute newPageRoute in page.Routes)
					{
						if (newPageRoute.Id == originalPageRoute.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						DeletePageRoute(originalPageRoute);
					}
				}
			}

			foreach (PageRoute PageRoute in page.Routes)
			{
				Boolean found = false;

				if (originalUrls != null)
				{
					foreach (PageRoute originalPageRoute in originalUrls)
					{
						if (PageRoute.Id == originalPageRoute.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					UpdatePageRoute(page, PageRoute);
				}
				else
				{
					AddPageRoute(site, page, PageRoute);
				}
			}
		}

		private void AddPageRoute(Site site, Page page, PageRoute PageRoute)
		{
			string commandText =
				"INSERT INTO PageRoutes " +
				"(Id, SiteId, PageId, Path, Type, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @PageId, @Path, @Type, @DateAdded, @AddedBy); " +
				"SELECT @Id;";

			if (PageRoute.Id == Guid.Empty)
			{
				PageRoute.Id = Guid.NewGuid();
			}

			if (!PageRoute.Path.StartsWith('/'))
			{
				PageRoute.Path = $"/{PageRoute.Path}";
			}

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", PageRoute.Id),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@PageId", page.Id),
					new SqliteParameter("@Path", PageRoute.Path),
					new SqliteParameter("@Type", PageRoute.Type),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		private void UpdatePageRoute(Page page, PageRoute PageRoute)
		{
			string commandText =
				"UPDATE PageRoutes " +
				"SET " +
				"  PageId = @PageId, " +
				"  Path = @Path, " +
				"  Type = @Type, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id;";

			if (!PageRoute.Path.StartsWith('/'))
			{
				PageRoute.Path = $"/{PageRoute.Path}";
			}

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", PageRoute.Id),
					new SqliteParameter("@PageId", page.Id),
					new SqliteParameter("@Path", PageRoute.Path),
					new SqliteParameter("@Type", PageRoute.Type),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		/// <summary>
		/// Delete a page Url record
		/// </summary>
		/// <param name="page"></param>
		public void DeletePageRoute(PageRoute pageRoute)
		{
			string commandText =
				"DELETE FROM PageRoutes " +
				"WHERE Id = @Id";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", pageRoute.Id)
				});
		}

		public void DeletePageRoutes(Page page)
		{
			string commandText =
				"DELETE FROM PageRoutes " +
				"WHERE PageId = @PageId";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@PageId", page.Id)
				});
		}

		public void DeletePageModules(Page page)
		{
			string commandTextPermissions =
				"DELETE " +
				"FROM Permissions " +
				"WHERE RelatedId IN " +
				"(" +
				"SELECT ID FROM PageModules " +
				"WHERE PageId = @PageId " +
				") ";

			this.ExecuteNonQuery(
				commandTextPermissions,
					new SqliteParameter[]
					{
						new SqliteParameter("@PageId", page.Id)
					});

			foreach (PageModule module in ListPageModules(page.Id))
			{
				DeletePageModule(module);
			}
			//string commandText =
			//	"DELETE FROM PageModules " +
			//	"WHERE PageId = @PageId";

			//this.ExecuteNonQuery(
			//	commandText,
			//	new SqliteParameter[]
			//	{
			//		new SqliteParameter("@PageId", page.Id)
			//	});
		}

		/// <summary>
		/// Delete a page record
		/// </summary>
		/// <param name="page"></param>
		public void DeletePage(Page page)
		{
			Boolean transactionStarted = false;

			try
			{
				transactionStarted = BeginTransaction();

				string commandText =
					"DELETE FROM Pages " +
					"WHERE Id = @Id";

				DeletePageRoutes(page);
				DeletePageModules(page);

				string commandTextPermissions =
					"DELETE " +
					"FROM Permissions " +
					"WHERE RelatedId = @PageId ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
						new SqliteParameter("@PageId", page.Id)
						});

				this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", page.Id)
					});

				if (transactionStarted)
				{

					CommitTransaction();
				}
				this.EventManager.RaiseEvent<Page, Delete>(page);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting page {0).", page.Id);
				if (transactionStarted)
				{

					RollbackTransaction();
				}
				throw;
			}

		}
		#endregion

		#region "    Module methods    "
		public PageModule GetPageModule(Guid moduleId)
		{
			IDataReader reader;
			PageModule result;

			if (moduleId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM PageModules " +
				"WHERE Id = @ModuleId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", moduleId)
				});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<PageModule>(reader);
					result.ModuleDefinition = GetModuleDefinition(DataHelper.GetGUID(reader, "ModuleDefinitionId"));
					result.Container = GetContainerDefinition(DataHelper.GetGUID(reader, "ContainerDefinitionId"));
					result.ModuleSettings = ListPageModuleSettings(result.Id);
					result.Permissions = ListPageModulePermissions(result.Id);
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

		public ModuleSettingsList ListPageModuleSettings(Guid moduleId)
		{
			IDataReader reader;
			ModuleSettingsList results = new();

			string commandText =
				"SELECT * " +
				"FROM PageModuleSettings " +
				"WHERE ModuleId = @ModuleId";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", moduleId)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(DataHelper.GetString(reader, "SettingName"), DataHelper.GetString(reader, "SettingValue"));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public List<PageModule> ListPageModules(Guid pageId)
		{
			IDataReader reader;
			List<PageModule> results = new();
			PageModule result;

			string commandText =
				"SELECT * " +
				"FROM PageModules, ModuleDefinitions " +
				"WHERE PageId = @PageId " +
				"AND PageModules.ModuleDefinitionId = ModuleDefinitions.Id " +
				"ORDER BY Pane, SortOrder ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@PageId", pageId)
				});

			try
			{
				while (reader.Read())
				{
					result = ModelExtensions.Create<PageModule>(reader);
					result.ModuleDefinition = GetModuleDefinition(DataHelper.GetGUID(reader, "ModuleDefinitionId"));
					result.ModuleSettings = ListPageModuleSettings(result.Id);
					result.Permissions = ListPageModulePermissions(result.Id);
					results.Add(result);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void DeletePageModule(PageModule module)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
						"DELETE " +
						"FROM Permissions " +
						"WHERE RelatedId = @ModuleId ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
						new SqliteParameter("@ModuleId", module.Id)
						});

				string commandTextSettings =
						"DELETE " +
						"FROM PageModuleSettings " +
						"WHERE ModuleId = @ModuleId ";

				this.ExecuteNonQuery(
					commandTextSettings,
						new SqliteParameter[]
						{
						new SqliteParameter("@ModuleId", module.Id)
						});

				string commandText =
					"DELETE FROM PageModules " +
					"WHERE Id = @Id; ";

				this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", module.Id)
					});

				CommitTransaction();

				this.EventManager.RaiseEvent<PageModule, Delete>(module);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting page module {0}", module.Id);
				RollbackTransaction();
				throw;
			}
		}


		public void SavePageModule(Guid pageId, PageModule module)
		{
			PageModule current = GetPageModule(module.Id);

			if (current == null)
			{
				module.Id = AddPageModule(pageId, module);
				this.EventManager.RaiseEvent<PageModule, Create>(module);
			}
			else
			{
				UpdatePageModule(pageId, module);
				this.EventManager.RaiseEvent<PageModule, Update>(module);
			}

			SavePageModuleSettings(module.Id, module.ModuleSettings);
		}

		private long GetTopPageModuleSortOrder(Guid pageId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM PageModules " +
				"WHERE PageId = @PageId " +
				"ORDER BY SortOrder DESC " +
				"LIMIT 1; ";

			topSortOrder = this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@PageId", pageId)
				});

			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		/// <summary>
		/// Create a new site record
		/// </summary>
		/// <param name="module"></param>
		private Guid AddPageModule(Guid pageId, PageModule module)
		{
			if (module.Id == Guid.Empty) module.Id = Guid.NewGuid();

			module.SortOrder = GetTopPageModuleSortOrder(pageId) + 10;

			string commandText =
				"INSERT INTO PageModules " +
				"(Id, PageId, Pane, ContainerDefinitionId, Title, ModuleDefinitionId, SortOrder, InheritPagePermissions, Style, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @PageId, @Pane, @ContainerDefinitionId, @Title, @ModuleDefinitionId, @SortOrder, @InheritPagePermissions, @Style, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", module.Id),
					new SqliteParameter("@PageId", pageId),
					new SqliteParameter("@Title", module.Title),
					new SqliteParameter("@Pane", module.Pane),
					new SqliteParameter("@ContainerDefinitionId", module.Container.Id),
					new SqliteParameter("@ModuleDefinitionId", module.ModuleDefinition.Id),
					new SqliteParameter("@SortOrder", module.SortOrder),
					new SqliteParameter("@InheritPagePermissions", module.InheritPagePermissions),
					new SqliteParameter("@Style", module.Style),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return module.Id;
		}

		/// <summary>
		/// Update an existing site record
		/// </summary>
		/// <param name="module"></param>
		private void UpdatePageModule(Guid pageId, PageModule module)
		{
			string commandText =
				"UPDATE PageModules " +
				"SET " +
				"  PageId = @PageId, " +
				"  Title = @Title, " +
				"  Pane = @Pane, " +
				"  ContainerDefinitionId = @ContainerDefinitionId, " +
				"  SortOrder = @SortOrder, " +
				"  InheritPagePermissions = @InheritPagePermissions, " +
				"  Style = @Style, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", module.Id),
					new SqliteParameter("@PageId", pageId),
					new SqliteParameter("@Title", module.Title),
					new SqliteParameter("@Pane", module.Pane),
					new SqliteParameter("@ContainerDefinitionId", module.Container?.Id),
					new SqliteParameter("@SortOrder", module.SortOrder),
					new SqliteParameter("@InheritPagePermissions", module.InheritPagePermissions),
					new SqliteParameter("@Style", module.Style),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		#endregion

		#region "    Module Settings    "
		public void SavePageModuleSettings(Guid moduleId, Dictionary<string, string> moduleSettings)
		{
			PageModule current = GetPageModule(moduleId);

			if (current != null)
			{
				foreach (KeyValuePair<string, string> setting in moduleSettings)
				{
					if (current.ModuleSettings.ContainsKey(setting.Key))
					{
						UpdatePageModuleSetting(moduleId, setting);
					}
					else
					{
						AddPageModuleSetting(moduleId, setting);
					}
				}

				this.EventManager.RaiseEvent<PageModule, Update>(current);
			}
		}

		/// <summary>
		/// Create a new PageModuleSetting record
		/// </summary>
		/// <param name="PageModuleSetting"></param>
		private void AddPageModuleSetting(Guid moduleId, KeyValuePair<string, string> setting)
		{
			string commandText =
				"INSERT INTO PageModuleSettings " +
				"(ModuleId, SettingName, SettingValue, DateAdded, AddedBy) " +
				"VALUES " +
				"(@ModuleId, @SettingName, @SettingValue, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", moduleId),
					new SqliteParameter("@SettingName", setting.Key),
					new SqliteParameter("@SettingValue", setting.Value),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		/// <summary>
		/// Update an existing PageModuleSetting record
		/// </summary>
		/// <param name="PageModuleSetting"></param>
		private void UpdatePageModuleSetting(Guid moduleId, KeyValuePair<string, string> setting)
		{
			string commandText =
				"UPDATE PageModuleSettings " +
				"SET " +
				"  SettingValue = @SettingValue, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE ModuleId = @ModuleId " +
				"AND SettingName = @SettingName ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", moduleId),
					new SqliteParameter("@SettingName", setting.Key),
					new SqliteParameter("@SettingValue", setting.Value),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		#endregion

		#region "    Module Definitions    "
		public List<ModuleDefinition> ListModuleDefinitions()
		{
			return ListItems<ModuleDefinition>("ModuleDefinitions");
		}

		public ModuleDefinition GetModuleDefinition(Guid moduleId)
		{
			return GetItem<ModuleDefinition>("ModuleDefinitions", moduleId);
		}

		public void SaveModuleDefinition(ModuleDefinition moduleDefinition)
		{
			ModuleDefinition current = GetModuleDefinition(moduleDefinition.Id);

			if (current == null)
			{
				moduleDefinition.Id = AddModuleDefinition(moduleDefinition);
				this.EventManager.RaiseEvent<ModuleDefinition, Create>(moduleDefinition);
			}
			else
			{
				UpdateModuleDefinition(moduleDefinition);
				this.EventManager.RaiseEvent<ModuleDefinition, Update>(moduleDefinition);
			}
		}

		/// <summary>
		/// Delete the specified module definition, and all module instances.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public void DeleteModuleDefinition(ModuleDefinition moduleDefinition)
		{
			BeginTransaction();

			try
			{
				string commandTextPermissions =
					"DELETE " +
					"FROM Permissions " +
					"WHERE RelatedId IN " +
					"( " +
					"SELECT Id FROM PageModules " +
					"WHERE ModuleDefinitionId = @ModuleDefinitionId " +
					"); ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@ModuleDefinitionId", moduleDefinition.Id)
						});

				string commandTextDeleteModuleSettings =
					"DELETE FROM PageModuleSettings " +
					"WHERE ModuleId IN " +
					"( " +
					"SELECT Id FROM PageModules " +
					"WHERE ModuleDefinitionId = @ModuleDefinitionId " +
					"); ";

				this.ExecuteNonQuery(
					commandTextDeleteModuleSettings,
						new SqliteParameter[]
						{
							new SqliteParameter("@ModuleDefinitionId", moduleDefinition.Id)
						});

				string commandTextDeleteModules =
					"DELETE FROM PageModules " +
					"WHERE ModuleDefinitionId = @ModuleDefinitionId; ";

				this.ExecuteNonQuery(
					commandTextDeleteModules,
						new SqliteParameter[]
						{
							new SqliteParameter("@ModuleDefinitionId", moduleDefinition.Id)
						});

				string commandText =
					"DELETE " +
					"FROM ModuleDefinitions " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", moduleDefinition.Id)
						});

				CommitTransaction();
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
		}

		/// <summary>
		/// Create a new moduleDefinition record
		/// </summary>
		/// <param name="moduleDefinition"></param>
		private Guid AddModuleDefinition(ModuleDefinition moduleDefinition)
		{
			if (moduleDefinition.Id == Guid.Empty) moduleDefinition.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ModuleDefinitions " +
				"(Id, FriendlyName, ClassTypeName, ViewAction, EditAction, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @FriendlyName, @ClassTypeName, @ViewAction, @EditAction, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", moduleDefinition.Id),
					new SqliteParameter("@FriendlyName", moduleDefinition.FriendlyName),
					new SqliteParameter("@ClassTypeName", moduleDefinition.ClassTypeName),
					new SqliteParameter("@ViewAction", moduleDefinition.ViewAction),
					new SqliteParameter("@EditAction", moduleDefinition.EditAction),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return moduleDefinition.Id;
		}

		/// <summary>
		/// Update an existing moduleDefinition record
		/// </summary>
		/// <param name="moduleDefinition"></param>
		private void UpdateModuleDefinition(ModuleDefinition moduleDefinition)
		{
			string commandText =
				"UPDATE ModuleDefinitions " +
				"SET " +
				"  FriendlyName = @FriendlyName, " +
				"  ClassTypeName = @ClassTypeName, " +
				"  ViewAction = @ViewAction, " +
				"  EditAction = @EditAction, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", moduleDefinition.Id),
					new SqliteParameter("@FriendlyName", moduleDefinition.FriendlyName),
					new SqliteParameter("@ClassTypeName", moduleDefinition.ClassTypeName),
					new SqliteParameter("@ViewAction", moduleDefinition.ViewAction),
					new SqliteParameter("@EditAction", moduleDefinition.EditAction),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		#endregion

		#region "    Users    "

		public List<User> ListSystemAdministrators()
		{
			IDataReader reader;
			List<User> results = new();

			string commandText =
				"SELECT * " +
				"FROM Users " +
				"WHERE SiteId IS NULL " +
				"AND IsSystemAdministrator = 1 ";

			reader = this.ExecuteReader(
				commandText,
				Array.Empty<SqliteParameter>());

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<User>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void SaveSystemAdministrator(User user)
		{
			User current = GetUser(user.Id);

			if (current == null)
			{
				user.Id = AddSystemAdministrator(user);
			}
			else
			{
				UpdateSystemAdministrator(user);
			}

			if (user.Secrets != null)
			{
				SaveUserSecrets(user);
			}

			if (user.Profile != null)
			{
				SaveUserProfile(user);
			}
		}

		private Guid AddSystemAdministrator(User user)
		{
			if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Users " +
				"(Id, UserName, SiteId, IsSystemAdministrator, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @UserName, @SiteId, @IsSystemAdministrator, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", user.Id),
					new SqliteParameter("@UserName", user.UserName),
					new SqliteParameter("@SiteId", null),
					new SqliteParameter("@IsSystemAdministrator", true),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return user.Id;
		}

		/// <summary>
		/// Update an existing user record
		/// </summary>
		/// <param name="user"></param>
		private void UpdateSystemAdministrator(User user)
		{
			string commandText =
				"UPDATE Users " +
				"SET " +
				"  UserName = @UserName, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id " +
				"AND IsSystemAdministrator = 1 ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", user.Id),
					new SqliteParameter("@UserName", user.UserName),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public List<User> ListUsers(Site site)
		{
			return ListItems<User>(site.Id, "SiteId", "Users");
		}

		public List<User> SearchUsers(Site site, string searchTerm)
		{
			IDataReader reader;
			List<User> results = new();

			string commandText =
				"SELECT * " +
				"FROM Users " +
				"WHERE SiteId = @SiteId " +
				"AND UserName LIKE @SearchTerm";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@SearchTerm", $"%{searchTerm}%"),
				});

			try
			{
				while (reader.Read())
				{
					User item = ModelExtensions.Create<User>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public User GetUserByName(Site site, string userName)
		{
			string result;

			if (String.IsNullOrEmpty(userName)) return null;

			string commandText =
				"SELECT Id " +
				"FROM Users " +
				"WHERE UserName = @UserName " +
				"AND SiteId = @SiteId; ";

			result = (string)this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserName", userName),
					new SqliteParameter("@SiteId", site.Id),
				});

			if (result == null)
			{
				return null;
			}
			else
			{
				return GetUser(Guid.Parse(result));
			}
		}

		public User GetUserByEmail(Site site, string email)
		{
			IDataReader reader = null;
			Guid result = Guid.Empty;

			if (String.IsNullOrEmpty(email)) return null;

			string commandText =
				"SELECT Id " +
				"FROM Users " +
				"WHERE Id IN " +
				"( " +
				"  SELECT UserId " +
				"  FROM UserProfileValues " + "" +
				"  WHERE Value = @Email " +
				"  AND UserProfilePropertyId IN " +
				"  ( " +
				"     SELECT Id " +
				"     FROM UserProfileProperties " +
				"     WHERE TypeUri = @EmailTypeUri " +
				"  ) " +
				") " +
				"AND SiteId = @SiteId; ";

			try
			{
				reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Email", email),
					new SqliteParameter("@EmailTypeUri", ClaimTypes.Email),					
					new SqliteParameter("@SiteId", site.Id),
				});

				if (reader.Read())
				{
					result = DataHelper.GetGUID(reader, "Id");
				}

				if (reader.Read())
				{
					throw new InvalidOperationException("The supplied email address matches more than one account.");
				}

				if (result == Guid.Empty)
				{
					return null;
				}
				else
				{
					return GetUser(result);
				}
			}
			finally
			{
				reader?.Close();
			}

		}

		public User GetSystemAdministrator(string userName)
		{
			IDataReader reader;
			User result;

			if (String.IsNullOrEmpty(userName)) return null;

			string commandText =
				"SELECT * " +
				"FROM Users " +
				"WHERE UserName = @UserName " +
				"AND SiteId IS NULL " +
				"AND IsSystemAdministrator = 1; ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserName", userName)
				});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<User>(reader);
					result.Secrets = GetUserSecrets(result.Id);
					result.Profile = ListUserProfileValues(result.Id);

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

		public User GetUser(Guid userId)
		{
			IDataReader reader;
			User result;

			string commandText =
				"SELECT * " +
				"FROM Users " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", userId)
				});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<User>(reader);
					result.Secrets = GetUserSecrets(result.Id);
					result.Roles = ListUserRoles(result.Id);
					result.Profile = ListUserProfileValues(result.Id);
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

		private List<UserProfileValue> ListUserProfileValues(Guid userId)
		{
			IDataReader reader;
			List<UserProfileValue> results = new();
			UserProfileValue result;

			if (userId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM UserProfileValues " +
				"WHERE UserId = @UserId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId)
				});

			try
			{
				while (reader.Read())
				{
					result = ModelExtensions.Create<UserProfileValue>(reader);
					result.UserProfileProperty = GetUserProfileProperty(DataHelper.GetGUID(reader, "UserProfilePropertyId"));
					results.Add(result);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void SaveUser(Site site, User user)
		{
			User current = GetUser(user.Id);

			if (current == null)
			{
				user.Id = AddUser(site, user);
				this.EventManager.RaiseEvent<User, Create>(user);
			}
			else
			{
				UpdateUser(user);
				this.EventManager.RaiseEvent<User, Update>(user);
			}

			if (user.Secrets != null)
			{
				SaveUserSecrets(user);
			}

			if (user.Roles != null)
			{
				SaveUserRoles(user, current?.Roles);
			}

			if (user.Profile != null)
			{
				SaveUserProfile(user);
			}
		}

		/// <summary>
		/// Create a new user record
		/// </summary>
		/// <param name="user"></param>
		private Guid AddUser(Site site, User user)
		{
			if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Users " +
				"(Id, UserName, SiteId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @UserName, @SiteId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", user.Id),
					new SqliteParameter("@UserName", user.UserName),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return user.Id;
		}

		/// <summary>
		/// Update an existing user record
		/// </summary>
		/// <param name="user"></param>
		private void UpdateUser(User user)
		{
			string commandText =
				"UPDATE Users " +
				"SET " +
				"  UserName = @UserName, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", user.Id),
					new SqliteParameter("@UserName", user.UserName),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}

		public void DeleteUser(User user)
		{
			Boolean transactionStarted = false;

			try
			{
				transactionStarted = BeginTransaction();
				//BeginTransaction();

				string commandText =
				"DELETE " +
				"FROM Users " +
				"WHERE Id = @Id ";

				string commandTextUserRoles =
					"DELETE " +
					"FROM UserRoles " +
					"WHERE UserId = @Id ";

				string commandTextUserSecrets =
					"DELETE " +
					"FROM UserSecrets " +
					"WHERE UserId = @Id ";

				string commandTextUserProfile =
					"DELETE " +
					"FROM UserProfileValues " +
					"WHERE UserId = @Id ";

				string commandTextUserSession =
					"DELETE " +
					"FROM UserSessions " +
					"WHERE UserId = @Id ";

				this.ExecuteNonQuery(
					commandTextUserRoles,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", user.Id)
						});

				this.ExecuteNonQuery(
					commandTextUserSecrets,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", user.Id)
						});

				this.ExecuteNonQuery(
					commandTextUserProfile,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", user.Id)
						});

				this.ExecuteNonQuery(
					commandTextUserSession,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", user.Id)
						});

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", user.Id)
						});

				if (transactionStarted)
				{

					CommitTransaction();
				}
				this.EventManager.RaiseEvent<User, Delete>(user);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting page user {0}", user.Id);

				if (transactionStarted)
				{
					RollbackTransaction();
					//RollbackTransaction();
				}

				throw;
			}
		}

		public void SaveUserSecrets(User user)
		{
			UserSecrets current = GetUserSecrets(user.Id);

			if (current == null)
			{
				AddUserSecrets(user);
			}
			else
			{
				UpdateUserSecrets(user);
			}
		}


		/// <summary>
		/// Create a new userSecrets record
		/// </summary>
		/// <param name="userSecrets"></param>
		private void AddUserSecrets(User user)
		{
			string commandText =
				"INSERT INTO UserSecrets " +
				"(UserId, PasswordHash, PasswordHashAlgorithm, PasswordResetToken, PasswordResetTokenExpiryDate, PasswordQuestion, PasswordAnswer, LastLoginDate, LastPasswordChangedDate, LastLockoutDate, IsLockedOut, FailedPasswordAttemptCount, FailedPasswordWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerWindowStart, Salt, DateAdded, AddedBy) " +
				"VALUES " +
				"(@UserId, @PasswordHash, @PasswordHashAlgorithm, @PasswordResetToken, @PasswordResetTokenExpiryDate, @PasswordQuestion, @PasswordAnswer, @LastLoginDate, @LastPasswordChangedDate, @LastLockoutDate, @IsLockedOut, @FailedPasswordAttemptCount, @FailedPasswordWindowStart, @FailedPasswordAnswerAttemptCount, @FailedPasswordAnswerWindowStart, @Salt, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", user.Id),
					new SqliteParameter("@PasswordHash", user.Secrets.PasswordHash),
					new SqliteParameter("@PasswordHashAlgorithm", user.Secrets.PasswordHashAlgorithm),
					new SqliteParameter("@PasswordResetToken", user.Secrets.PasswordResetToken),
					new SqliteParameter("@PasswordResetTokenExpiryDate", user.Secrets.PasswordResetTokenExpiryDate),
					new SqliteParameter("@PasswordQuestion", user.Secrets.PasswordQuestion),
					new SqliteParameter("@PasswordAnswer", user.Secrets.PasswordAnswer),
					new SqliteParameter("@LastLoginDate", user.Secrets.LastLoginDate),
					new SqliteParameter("@LastPasswordChangedDate", user.Secrets.LastPasswordChangedDate),
					new SqliteParameter("@LastLockoutDate", user.Secrets.LastLockoutDate),
					new SqliteParameter("@IsLockedOut", user.Secrets.IsLockedOut),
					new SqliteParameter("@FailedPasswordAttemptCount", user.Secrets.FailedPasswordAttemptCount),
					new SqliteParameter("@FailedPasswordWindowStart", user.Secrets.FailedPasswordWindowStart),
					new SqliteParameter("@FailedPasswordAnswerAttemptCount", user.Secrets.FailedPasswordAnswerAttemptCount),
					new SqliteParameter("@FailedPasswordAnswerWindowStart", user.Secrets.FailedPasswordAnswerWindowStart),
					new SqliteParameter("@Salt", user.Secrets.Salt),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		/// <summary>
		/// Update an existing userSecrets record
		/// </summary>
		/// <param name="userSecrets"></param>
		private void UpdateUserSecrets(User user)
		{
			string commandText =
				"UPDATE UserSecrets " +
				"SET " +
				"  UserId=@UserId, " +
				"  PasswordHash=@PasswordHash, " +
				"  PasswordHashAlgorithm=PasswordHashAlgorithm, " +
				"  PasswordResetToken=@PasswordResetToken, " +
				"  PasswordResetTokenExpiryDate=@PasswordResetTokenExpiryDate, " +
				"  PasswordQuestion=@PasswordQuestion, " +
				"  PasswordAnswer=@PasswordAnswer, " +
				"  LastLoginDate=@LastLoginDate, " +
				"  LastPasswordChangedDate=@LastPasswordChangedDate, " +
				"  LastLockoutDate=@LastLockoutDate, " +
				"  IsLockedOut=@IsLockedOut, " +
				"  FailedPasswordAttemptCount=@FailedPasswordAttemptCount, " +
				"  FailedPasswordWindowStart=@FailedPasswordWindowStart, " +
				"  FailedPasswordAnswerAttemptCount=@FailedPasswordAnswerAttemptCount, " +
				"  FailedPasswordAnswerWindowStart=@FailedPasswordAnswerWindowStart, " +
				"  Salt=@Salt, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE UserId = @UserId ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", user.Id),
					new SqliteParameter("@PasswordHash", user.Secrets.PasswordHash),
					new SqliteParameter("@PasswordHashAlgorithm", user.Secrets.PasswordHashAlgorithm),
					new SqliteParameter("@PasswordResetToken", user.Secrets.PasswordResetToken),
					new SqliteParameter("@PasswordResetTokenExpiryDate", user.Secrets.PasswordResetTokenExpiryDate),
					new SqliteParameter("@PasswordQuestion", user.Secrets.PasswordQuestion),
					new SqliteParameter("@PasswordAnswer", user.Secrets.PasswordAnswer),
					new SqliteParameter("@LastLoginDate", user.Secrets.LastLoginDate),
					new SqliteParameter("@LastPasswordChangedDate", user.Secrets.LastPasswordChangedDate),
					new SqliteParameter("@LastLockoutDate", user.Secrets.LastLockoutDate),
					new SqliteParameter("@IsLockedOut", user.Secrets.IsLockedOut),
					new SqliteParameter("@FailedPasswordAttemptCount", user.Secrets.FailedPasswordAttemptCount),
					new SqliteParameter("@FailedPasswordWindowStart", user.Secrets.FailedPasswordWindowStart),
					new SqliteParameter("@FailedPasswordAnswerAttemptCount", user.Secrets.FailedPasswordAnswerAttemptCount),
					new SqliteParameter("@FailedPasswordAnswerWindowStart", user.Secrets.FailedPasswordAnswerWindowStart),
					new SqliteParameter("@Salt", user.Secrets.Salt),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}

		public UserSecrets GetUserSecrets(Guid userId)
		{
			IDataReader reader;
			UserSecrets userSecrets;

			if (userId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM UserSecrets " +
				"WHERE UserId = @UserId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId)
				});

			try
			{
				if (reader.Read())
				{
					userSecrets = ModelExtensions.Create<UserSecrets>(reader);

					return userSecrets;
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

		// --
		public void SaveUserProfile(User user)
		{
			foreach (UserProfileValue value in user.Profile)
			{
				UserProfileValue current = GetUserProfileValue(user.Id, value.UserProfileProperty.Id);

				if (current == null)
				{
					AddUserProfile(user.Id, value);
				}
				else
				{
					UpdateUserProfile(user.Id, value);
				}
			}
		}


		/// <summary>
		/// Create a new userProfile record
		/// </summary>
		/// <param name="userProfile"></param>
		private void AddUserProfile(Guid userId, UserProfileValue value)
		{
			string commandText =
				"INSERT INTO UserProfileValues " +
				"(UserId, UserProfilePropertyId, Value, DateAdded, AddedBy) " +
				"VALUES " +
				"(@UserId, @UserProfilePropertyId, @Value, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId),
					new SqliteParameter("@UserProfilePropertyId", value.UserProfileProperty.Id),
					new SqliteParameter("@Value", value.Value),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		/// <summary>
		/// Update an existing userProfile record
		/// </summary>
		/// <param name="userProfile"></param>
		private void UpdateUserProfile(Guid userId, UserProfileValue value)
		{
			string commandText =
				"UPDATE UserProfileValues " +
				"SET " +
				"  Value=@Value, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE UserId = @UserId AND UserProfilePropertyId = @UserProfilePropertyId";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId),
					new SqliteParameter("@UserProfilePropertyId", value.UserProfileProperty.Id),
					new SqliteParameter("@Value", value.Value),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}

		public UserProfileValue GetUserProfileValue(Guid userId, Guid propertyId)
		{
			IDataReader reader;
			UserProfileValue userProfile;

			if (userId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM UserProfileValues " +
				"WHERE UserId = @UserId " +
				"AND UserProfilePropertyId = @PropertyId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId),
					new SqliteParameter("@PropertyId", propertyId)
				});

			try
			{
				if (reader.Read())
				{
					userProfile = ModelExtensions.Create<UserProfileValue>(reader);

					return userProfile;
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

		void SaveUserRoles(User user, List<Role> originalRoles)
		{
			if (user.Roles != null)
			{
				if (originalRoles != null)
				{
					foreach (Role originalRole in originalRoles)
					{
						Boolean found = false;

						foreach (Role newpageRole in user.Roles)
						{
							if (newpageRole.Id == originalRole.Id)
							{
								found = true;
								break;
							}
						}

						if (!found)
						{
							DeleteUserRole(user.Id, originalRole.Id);
						}
					}
				}

				foreach (Role role in user.Roles)
				{
					Boolean found = false;

					if (originalRoles != null)
					{
						foreach (Role originalRole in originalRoles)
						{
							if (role.Id == originalRole.Id)
							{
								found = true;
								break;
							}
						}
					}

					if (found)
					{
						// The UserRole table only has UserId and RoleId, updates are not required						
					}
					else
					{
						AddUserRole(user.Id, role.Id);
					}
				}
			}
		}


		public void DeleteUserRole(Guid userId, Guid roleId)
		{
			string commandText =
				"DELETE FROM UserRoles " +
				"WHERE UserId = @UserId " +
				"AND RoleId = @RoleId; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId),
					new SqliteParameter("@RoleId", roleId),
				});
		}

		/// <summary>
		/// Create a new userRoles record
		/// </summary>
		/// <param name="userRoles"></param>
		private void AddUserRole(Guid userId, Guid roleId)
		{
			string commandText =
				"INSERT INTO UserRoles " +
				"(UserId, RoleId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@UserId, @RoleId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId),
					new SqliteParameter("@RoleId", roleId),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});
		}

		public List<Role> ListUserRoles(Guid userId)
		{
			IDataReader reader;
			List<Role> results = new();

			if (userId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM Roles " +
				"WHERE Roles.Id IN (SELECT RoleId FROM UserRoles WHERE UserId = @UserId) ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@UserId", userId)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<Role>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}
		#endregion

		#region "    Role Groups    "

		public List<RoleGroup> ListRoleGroups(Site site)
		{
			return ListItems<RoleGroup>(site.Id, "SiteId", "RoleGroups");
		}

		public RoleGroup GetRoleGroup(Guid roleGroupId)
		{
			return GetItem<RoleGroup>("RoleGroups", roleGroupId);
		}

		public void SaveRoleGroup(Site site, RoleGroup roleGroup)
		{
			RoleGroup current = GetRoleGroup(roleGroup.Id);

			if (current == null)
			{
				roleGroup.Id = AddRoleGroup(site, roleGroup);
				this.EventManager.RaiseEvent<RoleGroup, Create>(roleGroup);
			}
			else
			{
				UpdateRoleGroup(roleGroup);
				this.EventManager.RaiseEvent<RoleGroup, Update>(roleGroup);
			}
		}

		public void DeleteRoleGroup(RoleGroup roleGroup)
		{
			Boolean transactionStarted = false;

			try
			{
				transactionStarted = BeginTransaction();
				//BeginTransaction();

				string commandTextRoles =
					"UPDATE Roles " +
					"SET RoleGroupId = NULL " +
					"WHERE RoleGroupId = @Id ";

				this.ExecuteNonQuery(
					commandTextRoles,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", roleGroup.Id)
						});

				string commandText =
					"DELETE " +
					"FROM RoleGroups " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", roleGroup.Id)
						});

				if (transactionStarted)
				{
					CommitTransaction();

				}
				this.EventManager.RaiseEvent<RoleGroup, Delete>(roleGroup);

			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting page role group {0}", roleGroup.Id);
				if (transactionStarted)
				{
					RollbackTransaction();
					//RollbackTransaction();
				}
				throw;
			}
		}


		private Guid AddRoleGroup(Site site, RoleGroup roleGroup)
		{
			if (roleGroup.Id == Guid.Empty) roleGroup.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO RoleGroups " +
				"(Id, Name, SiteId, Description, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @Name, @SiteId, @Description, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", roleGroup.Id),
					new SqliteParameter("@Name", roleGroup.Name),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Description", roleGroup.Description),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return roleGroup.Id;
		}

		private void UpdateRoleGroup(RoleGroup roleGroup)
		{
			string commandText =
				"UPDATE RoleGroups " +
				"SET " +
				"  Name = @Name, " +
				"  Description = @Description, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", roleGroup.Id),
					new SqliteParameter("@Name", roleGroup.Name),
					new SqliteParameter("@Description", roleGroup.Description),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}
		#endregion

		#region "    Roles    "

		public List<Role> ListRoles(Site site)
		{
			return ListItems<Role>(site.Id, "SiteId", "Roles");
		}

		public List<Role> ListRoleGroupRoles(Guid roleGroupId)
		{
			IDataReader reader;
			List<Role> results = new();

			string commandText =
				"SELECT * " +
				"FROM Roles " +
				"WHERE RoleGroupId = @RoleGroupId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@RoleGroupId", roleGroupId)
				});

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<Role>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public Role GetRole(Guid roleId)
		{
			return GetItem<Role>("Roles", roleId);
		}

		public void SaveRole(Site site, Role role)
		{
			Role current = GetRole(role.Id);

			if (current == null)
			{
				role.Id = AddRole(site, role);
				this.EventManager.RaiseEvent<Role, Create>(role);
			}
			else
			{
				UpdateRole(role);
				this.EventManager.RaiseEvent<Role, Update>(role);
			}
		}

		public void DeleteRole(Role role)
		{
			Boolean transactionStarted = false;

			try
			{
				transactionStarted = BeginTransaction();
				//BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM Permissions " +
					"WHERE RoleId = @RoleId ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
						new SqliteParameter("@RoleId", role.Id)
						});

				string commandText =
					"DELETE " +
					"FROM Roles " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", role.Id)
						});

				if (transactionStarted)
				{
					CommitTransaction();

				}
				this.EventManager.RaiseEvent<Role, Delete>(role);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting role {0}", role.Id);

				if (transactionStarted)
				{
					RollbackTransaction();
					//RollbackTransaction();
				}
				throw;
			}

		}

		private Guid AddRole(Site site, Role role)
		{
			if (role.Id == Guid.Empty) role.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Roles " +
				"(Id, RoleGroupId, SiteId, Name, Description, Type, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @RoleGroupId, @SiteId, @Name, @Description, @Type, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", role.Id),
					new SqliteParameter("@RoleGroupId", role.RoleGroupId),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Name", role.Name),
					new SqliteParameter("@Description", role.Description),
					new SqliteParameter("@Type", role.Type),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return role.Id;
		}

		private void UpdateRole(Role role)
		{
			// Note, Role Type is not included in the update.  Role types cannot be changed by users.
			string commandText =
				"UPDATE Roles " +
				"SET " +
				"  RoleGroupId = @RoleGroupId, " +
				"  Name = @Name, " +
				"  Description = @Description, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", role.Id),
					new SqliteParameter("@RoleGroupId", role.RoleGroupId),
					new SqliteParameter("@Name", role.Name),
					new SqliteParameter("@Description", role.Description),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}


		#endregion

		#region "    Permissions    "
		public List<PermissionType> ListPagePermissionTypes()
		{
			return ListPermissionTypes(PermissionType.PermissionScopeNamespaces.Page);
		}

		public List<PermissionType> ListModulePermissionTypes()
		{
			return ListPermissionTypes(PermissionType.PermissionScopeNamespaces.Module);
		}

		public List<PermissionType> ListFolderPermissionTypes()
		{
			return ListPermissionTypes(PermissionType.PermissionScopeNamespaces.Folder);
		}

		private PermissionType GetPermissionType(Guid permissionTypeId)
		{
			return GetItem<PermissionType>("PermissionTypes", permissionTypeId);
		}

		public List<PermissionType> ListPermissionTypes(string scopeNamespace)
		{
			IDataReader reader;
			List<PermissionType> results = new();

			string commandText =
				"SELECT * " +
				"FROM PermissionTypes " +
				"WHERE Scope LIKE @ScopeNamespace ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ScopeNamespace", scopeNamespace + "%")
				});

			try
			{
				while (reader.Read())
				{
					results.Add(ModelExtensions.Create<PermissionType>(reader));
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public List<Permission> ListPermissions(Guid Id, string permissionNameSpace)
		{
			IDataReader reader;
			List<Permission> results = new();

			string commandText =
				"SELECT * " +
				"FROM Permissions " +
				"WHERE RelatedId = @RelatedId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@RelatedId", Id)
				});

			try
			{
				while (reader.Read())
				{
					Permission permission = ModelExtensions.Create<Permission>(reader);
					permission.Role = GetRole(DataHelper.GetGUID(reader, "RoleId"));
					permission.PermissionType = GetPermissionType(DataHelper.GetGUID(reader, "PermissionTypeId"));
					results.Add(permission);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public List<Permission> ListPagePermissions(Guid pageId)
		{
			return ListPermissions(pageId, PermissionType.PermissionScopeNamespaces.Page);
		}

		public List<Permission> ListPageModulePermissions(Guid moduleId)
		{
			return ListPermissions(moduleId, PermissionType.PermissionScopeNamespaces.Module);
		}

		public List<Permission> ListFolderPermissions(Guid folderId)
		{
			return ListPermissions(folderId, PermissionType.PermissionScopeNamespaces.Folder);
		}

		public void SavePagePermissions(Guid pageId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions)
		{
			SavePermissions(pageId, permissions, originalPermissions);
		}

		public void SavePermissions(Guid relatedId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions)
		{
			if (permissions == null) return;

			if (originalPermissions != null)
			{
				foreach (Permission originalPermission in originalPermissions)
				{
					Boolean found = false;

					foreach (Permission newPermission in permissions)
					{
						if (newPermission.Id == originalPermission.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						DeletePermission(originalPermission.Id);
					}
				}
			}

			foreach (Permission permission in permissions)
			{
				Boolean found = false;

				if (originalPermissions != null)
				{
					foreach (Permission originalPermission in originalPermissions)
					{
						if (permission.Id == originalPermission.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					UpdatePermission(permission);
				}
				else
				{
					AddPermission(relatedId, permission);
				}
			}
		}

		public void SaveFolderPermissions(Guid folderId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions)
		{
			SavePermissions(folderId, permissions, originalPermissions);
		}

		public void SavePageModulePermissions(Guid moduleId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions)
		{
			SavePermissions(moduleId, permissions, originalPermissions);
			//if (permissions == null) return;

			//foreach (Permission permission in permissions)
			//{
			//	SavePermission(moduleId, permission);
			//}

		}

		private Permission GetPermission(Guid relatedId, Guid permissionTypeId, Guid roleId)
		{
			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM Permissions " +
				"WHERE RelatedId = @RelatedId " +
				"AND PermissionTypeId = @PermissionTypeId " +
				"AND RoleId = @RoleId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
					{
					new SqliteParameter("@RelatedId", relatedId),
					new SqliteParameter("@PermissionTypeId", permissionTypeId),
					new SqliteParameter("@RoleId", roleId)
				});

			try
			{
				if (reader.Read())
				{
					return ModelExtensions.Create<Permission>(reader);
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

		private Permission GetPermission(Guid permissionId)
		{
			return GetItem<Permission>("Permissions", permissionId);
		}

		private void SavePermission(Guid relatedId, Permission permission)
		{
			Permission current = GetPermission(permission.Id);

			if (current == null)
			{
				current = GetPermission(relatedId, permission.PermissionType.Id, permission.Role.Id);
			}

			if (current == null)
			{
				permission.Id = AddPermission(relatedId, permission);
			}
			else
			{
				UpdatePermission(permission);
			}
		}

		private Guid AddPermission(Guid relatedId, Permission permission)
		{
			if (permission.Id == Guid.Empty) permission.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Permissions " +
				"(Id, RelatedId, PermissionTypeId, RoleId, AllowAccess, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @RelatedId, @PermissionTypeId, @RoleId, @AllowAccess, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", permission.Id),
					new SqliteParameter("@RelatedId", relatedId),
					new SqliteParameter("@PermissionTypeId", permission.PermissionType.Id),
					new SqliteParameter("@RoleId", permission.Role.Id),
					new SqliteParameter("@AllowAccess", permission.AllowAccess),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return permission.Id;
		}

		/// <summary>
		/// Update an existing permission record
		/// </summary>
		/// <param name="permission"></param>
		private void UpdatePermission(Permission permission)
		{
			string commandText =
				"UPDATE Permissions " +
				"SET " +
				"  AllowAccess = @AllowAccess, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", permission.Id),
					new SqliteParameter("@AllowAccess", permission.AllowAccess),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}


		public void DeletePermission(Guid Id)
		{
			string commandText =
				"DELETE FROM Permissions " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", Id)
				});
		}

		#endregion

		#region "    Session    "


		public UserSession GetUserSession(Guid userSessionId)
		{
			return GetItem<UserSession>("UserSessions", userSessionId);
		}

		public void DeleteUserSession(UserSession userSession)
		{
			string commandText =
				"DELETE " +
				"FROM UserSessions " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", userSession.Id)
					});
		}

		public void SaveUserSession(UserSession userSession)
		{
			UserSession current = GetUserSession(userSession.Id);

			if (current == null)
			{
				userSession.Id = AddUserSession(userSession);
			}
			else
			{
				UpdateUserSession(userSession);
			}
		}

		private Guid AddUserSession(UserSession userSession)
		{
			if (userSession.Id == Guid.Empty) userSession.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO UserSessions " +
				"(Id, IssuedDate, IsPersistent, SlidingExpiry, ExpiryDate, UserId, SiteId, RemoteIpAddress) " +
				"VALUES " +
				"(@Id, @IssuedDate, @IsPersistent, @SlidingExpiry, @ExpiryDate, @UserId, @SiteId, @RemoteIpAddress); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", userSession.Id),
					new SqliteParameter("@IssuedDate", userSession.IssuedDate),
					new SqliteParameter("@IsPersistent", userSession.IsPersistent),
					new SqliteParameter("@SlidingExpiry", userSession.SlidingExpiry),
					new SqliteParameter("@ExpiryDate", userSession.ExpiryDate),
					new SqliteParameter("@UserId", userSession.UserId),
					new SqliteParameter("@SiteId", userSession.SiteId),
					new SqliteParameter("@RemoteIpAddress", userSession.RemoteIpAddress.ToString())
				});

			return userSession.Id;
		}

		private void UpdateUserSession(UserSession userSession)
		{
			// For user sessions, the only field which can be updated is the expiry date.
			string commandText =
				"UPDATE UserSessions " +
				"SET " +
				"  ExpiryDate = @ExpiryDate " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", userSession.Id),
					new SqliteParameter("@ExpiryDate", userSession.ExpiryDate)
				});
		}

		#endregion

		#region "    Site User Profile Properties    "

		public UserProfileProperty GetUserProfileProperty(Guid id)
		{
			return this.GetItem<UserProfileProperty>("UserProfileProperties", id);
		}

		public void SaveUserProfileProperty(Guid siteId, UserProfileProperty property)
		{
			UserProfileProperty current = GetUserProfileProperty(property.Id);

			if (current == null)
			{
				property.Id = AddUserProfileProperty(siteId, property);
			}
			else
			{
				UpdateUserProfileProperty(property);
			}
		}

		private long GetTopProfilePropertySortOrder(Guid siteId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM UserProfileProperties " +
				"WHERE SiteId = @SiteId " +
				"ORDER BY SortOrder DESC " +
				"LIMIT 1; ";

			topSortOrder = this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@SiteId", siteId)
				});

			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		private Guid AddUserProfileProperty(Guid siteId, UserProfileProperty property)
		{
			if (property.Id == Guid.Empty) property.Id = Guid.NewGuid();

			property.SortOrder = GetTopProfilePropertySortOrder(siteId) + 10;

			string commandText =
				"INSERT INTO UserProfileProperties " +
				"(Id, SiteId, Name, TypeUri, HelpText, SortOrder, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @Name, @TypeUri, @HelpText, @SortOrder, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", property.Id),
					new SqliteParameter("@SiteId", siteId),
					new SqliteParameter("@Name", property.Name),
					new SqliteParameter("@TypeUri", property.TypeUri),
					new SqliteParameter("@SortOrder", property.SortOrder),
					new SqliteParameter("@HelpText", property.HelpText),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return property.Id;
		}

		private void UpdateUserProfileProperty(UserProfileProperty property)
		{
			// Site Id is never updated for user profile properties
			string commandText =
				"UPDATE UserProfileProperties " +
				"SET " +
				"  Name = @Name, " +
				"  TypeUri = @TypeUri, " +
				"  HelpText = @HelpText, " +
				"  SortOrder = @SortOrder, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", property.Id),
					new SqliteParameter("@Name", property.Name),
					new SqliteParameter("@TypeUri", property.TypeUri),
					new SqliteParameter("@HelpText", property.HelpText),
					new SqliteParameter("@SortOrder", property.SortOrder),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public void DeleteUserProfileProperty(Guid id)
		{
			string commandText =
				"DELETE FROM UserProfileProperties " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", id)
				});
		}
		#endregion

		#region "    Mail Templates    "
		public IEnumerable<MailTemplate> ListMailTemplates(Site site)
		{
			return ListItems<MailTemplate>(site.Id, "SiteId", "MailTemplates");
		}

		public MailTemplate GetMailTemplate(Guid templateId)
		{
			return GetItem<MailTemplate>("MailTemplates", templateId);
		}

		public void SaveMailTemplate(Site site, MailTemplate mailTemplate)
		{
			MailTemplate current = GetMailTemplate(mailTemplate.Id);

			if (current == null)
			{
				mailTemplate.Id = AddMailTemplate(site, mailTemplate);
				this.EventManager.RaiseEvent<MailTemplate, Create>(mailTemplate);
			}
			else
			{
				UpdateMailTemplate(mailTemplate);
				this.EventManager.RaiseEvent<MailTemplate, Update>(mailTemplate);
			}
		}

		public void DeleteMailTemplate(MailTemplate mailTemplate)
		{

			string commandText =
				"DELETE " +
				"FROM MailTemplates " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", mailTemplate.Id)
					});

			this.EventManager.RaiseEvent<MailTemplate, Delete>(mailTemplate);
		}

		private Guid AddMailTemplate(Site site, MailTemplate mailTemplate)
		{
			if (mailTemplate.Id == Guid.Empty) mailTemplate.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO MailTemplates " +
				"(Id, SiteId, Name, Subject, Body, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @Name, @Subject, @Body, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", mailTemplate.Id),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Name", mailTemplate.Name),
					new SqliteParameter("@Subject", mailTemplate.Subject),
					new SqliteParameter("@Body", mailTemplate.Body),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return mailTemplate.Id;
		}

		private void UpdateMailTemplate(MailTemplate mailTemplate)
		{
			// Note, MailTemplate Type is not included in the update.  MailTemplate types cannot be changed by users.
			string commandText =
				"UPDATE MailTemplates " +
				"SET " +
				"  Name = @Name, " +
				"  Subject = @Subject, " +
				"  Body = @Body, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", mailTemplate.Id),
					new SqliteParameter("@Name", mailTemplate.Name),
					new SqliteParameter("@Subject", mailTemplate.Subject),
					new SqliteParameter("@Body", mailTemplate.Body),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		#endregion


		#region "    Layout Definitions    "
		public List<LayoutDefinition> ListLayoutDefinitions()
		{
			return ListItems<LayoutDefinition>("LayoutDefinitions");
		}

		public LayoutDefinition GetLayoutDefinition(Guid moduleId)
		{
			return GetItem<LayoutDefinition>("LayoutDefinitions", moduleId);
		}

		public void SaveLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			LayoutDefinition current = GetLayoutDefinition(layoutDefinition.Id);

			if (current == null)
			{
				layoutDefinition.Id = AddLayoutDefinition(layoutDefinition);
				this.EventManager.RaiseEvent<LayoutDefinition, Create>(layoutDefinition);
			}
			else
			{
				UpdateLayoutDefinition(layoutDefinition);
				this.EventManager.RaiseEvent<LayoutDefinition, Update>(layoutDefinition);
			}
		}

		public void DeleteLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			BeginTransaction();

			try
			{
				string commandTextResetPages =
					"UPDATE Pages " +
					"SET LayoutDefinitionId = NULL" +
					"WHERE LayoutDefinitionId = @LayoutDefinitionId; ";

				this.ExecuteNonQuery(
					commandTextResetPages,
						new SqliteParameter[]
						{
							new SqliteParameter("@LayoutDefinitionId", layoutDefinition.Id)
						});

				string commandText =
					"DELETE " +
					"FROM LayoutDefinitions " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", layoutDefinition.Id)
						});

				CommitTransaction();
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
		}

		/// <summary>
		/// Create a new layoutDefinition record
		/// </summary>
		/// <param name="layoutDefinition"></param>
		private Guid AddLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			if (layoutDefinition.Id == Guid.Empty) layoutDefinition.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO LayoutDefinitions " +
				"(Id, FriendlyName, RelativePath, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @FriendlyName, @RelativePath, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", layoutDefinition.Id),
					new SqliteParameter("@FriendlyName", layoutDefinition.FriendlyName),
					new SqliteParameter("@RelativePath", layoutDefinition.RelativePath),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return layoutDefinition.Id;
		}

		/// <summary>
		/// Update an existing layoutDefinition record
		/// </summary>
		/// <param name="layoutDefinition"></param>
		private void UpdateLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			string commandText =
				"UPDATE LayoutDefinitions " +
				"SET " +
				"  FriendlyName = @FriendlyName, " +
				"  RelativePath = @RelativePath, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", layoutDefinition.Id),
					new SqliteParameter("@FriendlyName", layoutDefinition.FriendlyName),
					new SqliteParameter("@RelativePath", layoutDefinition.RelativePath),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		#endregion

		#region "    Container Definitions    "
		public List<ContainerDefinition> ListContainerDefinitions()
		{
			return ListItems<ContainerDefinition>("ContainerDefinitions");
		}

		public ContainerDefinition GetContainerDefinition(Guid moduleId)
		{
			return GetItem<ContainerDefinition>("ContainerDefinitions", moduleId);
		}

		public void SaveContainerDefinition(ContainerDefinition containerDefinition)
		{
			ContainerDefinition current = GetContainerDefinition(containerDefinition.Id);

			if (current == null)
			{
				containerDefinition.Id = AddContainerDefinition(containerDefinition);
				this.EventManager.RaiseEvent<ContainerDefinition, Create>(containerDefinition);
			}
			else
			{
				UpdateContainerDefinition(containerDefinition);
				this.EventManager.RaiseEvent<ContainerDefinition, Update>(containerDefinition);
			}
		}

		public void DeleteContainerDefinition(ContainerDefinition containerDefinition)
		{
			BeginTransaction();

			try
			{
				string commandTextResetPages =
					"UPDATE PageModules " +
					"SET ContainerDefinitionId = NULL" +
					"WHERE ContainerDefinitionId = @ContainerDefinitionId; ";

				this.ExecuteNonQuery(
					commandTextResetPages,
						new SqliteParameter[]
						{
							new SqliteParameter("@ContainerDefinitionId", containerDefinition.Id)
						});

				string commandText =
					"DELETE " +
					"FROM ContainerDefinitions " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", containerDefinition.Id)
						});

				CommitTransaction();
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
		}

		/// <summary>
		/// Create a new containerDefinition record
		/// </summary>
		/// <param name="containerDefinition"></param>
		private Guid AddContainerDefinition(ContainerDefinition containerDefinition)
		{
			if (containerDefinition.Id == Guid.Empty) containerDefinition.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ContainerDefinitions " +
				"(Id, FriendlyName, RelativePath, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @FriendlyName, @RelativePath, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", containerDefinition.Id),
					new SqliteParameter("@FriendlyName", containerDefinition.FriendlyName),
					new SqliteParameter("@RelativePath", containerDefinition.RelativePath),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return containerDefinition.Id;
		}

		/// <summary>
		/// Update an existing containerDefinition record
		/// </summary>
		/// <param name="containerDefinition"></param>
		private void UpdateContainerDefinition(ContainerDefinition containerDefinition)
		{
			string commandText =
				"UPDATE ContainerDefinitions " +
				"SET " +
				"  FriendlyName = @FriendlyName, " +
				"  RelativePath = @RelativePath, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", containerDefinition.Id),
					new SqliteParameter("@FriendlyName", containerDefinition.FriendlyName),
					new SqliteParameter("@RelativePath", containerDefinition.RelativePath),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}
		#endregion

		#region "    ScheduledTasks    "

		public IEnumerable<ScheduledTask> ListScheduledTasks()
		{
			return ListItems<ScheduledTask>("ScheduledTasks");
		}

		public ScheduledTask GetScheduledTask(Guid scheduledTaskId)
		{
			return GetItem<ScheduledTask>("ScheduledTasks", scheduledTaskId);
		}

		public void ScheduledNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime)
		{
			string commandText =
				"UPDATE ScheduledTasks " +
				"SET " +
				"  NextScheduledRun = @NextScheduledRun " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", scheduledTask.Id),
					new SqliteParameter("@NextScheduledRun", nextRunDateTime)
				});

			// ScheduleNextRun does not raise an EventManager event
		}

		public void SaveScheduledTask(ScheduledTask scheduledTask)
		{
			ScheduledTask current = GetScheduledTask(scheduledTask.Id);

			if (current == null)
			{
				scheduledTask.Id = AddScheduledTask(scheduledTask);
				this.EventManager.RaiseEvent<ScheduledTask, Create>(scheduledTask);
			}
			else
			{
				UpdateScheduledTask(scheduledTask);
				this.EventManager.RaiseEvent<ScheduledTask, Update>(scheduledTask);
			}
		}

		public void DeleteScheduledTask(ScheduledTask scheduledTask)
		{
			try
			{
				BeginTransaction();

				string historyCommandText =
					"DELETE " +
					"FROM ScheduledTasksHistory " +
					"WHERE ScheduledTaskId = @Id ";

				this.ExecuteNonQuery(
					historyCommandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", scheduledTask.Id)
						});


				string commandText =
					"DELETE " +
					"FROM ScheduledTasks " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", scheduledTask.Id)
						});

				this.EventManager.RaiseEvent<ScheduledTask, Delete>(scheduledTask);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting scheduled task {0}", scheduledTask.Id);
				RollbackTransaction();
				throw;
			}
		}

		private Guid AddScheduledTask(ScheduledTask scheduledTask)
		{
			if (scheduledTask.Id == Guid.Empty) scheduledTask.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ScheduledTasks " +
				"(Id, TypeName, Name, Interval, IntervalType, Enabled, NextScheduledRun, KeepHistoryCount, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @TypeName, @Name, @Interval, @IntervalType, @Enabled, @NextScheduledRun, @KeepHistoryCount, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", scheduledTask.Id),
					new SqliteParameter("@TypeName", scheduledTask.TypeName),
					new SqliteParameter("@Name", scheduledTask.Name),
					new SqliteParameter("@Interval", scheduledTask.Interval),
					new SqliteParameter("@IntervalType", scheduledTask.IntervalType),
					new SqliteParameter("@Enabled", scheduledTask.Enabled),
					new SqliteParameter("@NextScheduledRun", scheduledTask.NextScheduledRun),
					new SqliteParameter("@KeepHistoryCount", scheduledTask.KeepHistoryCount),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return scheduledTask.Id;
		}

		private void UpdateScheduledTask(ScheduledTask scheduledTask)
		{
			string commandText =
				"UPDATE ScheduledTasks " +
				"SET " +
				"  TypeName = @TypeName, " +
				"  Name = @Name, " +
				"  Interval = @Interval, " +
				"  IntervalType = @IntervalType, " +
				"  Enabled = @Enabled, " +
				"  NextScheduledRun = @NextScheduledRun, " +
				"  KeepHistoryCount = @KeepHistoryCount, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", scheduledTask.Id),
					new SqliteParameter("@TypeName", scheduledTask.TypeName),
					new SqliteParameter("@Name", scheduledTask.Name),
					new SqliteParameter("@Interval", scheduledTask.Interval),
					new SqliteParameter("@IntervalType", scheduledTask.IntervalType),
					new SqliteParameter("@Enabled", scheduledTask.Enabled),
					new SqliteParameter("@NextScheduledRun", scheduledTask.NextScheduledRun),
					new SqliteParameter("@KeepHistoryCount", scheduledTask.KeepHistoryCount),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		private ScheduledTaskHistory GetScheduledTaskHistory(Guid id)
		{
			return GetItem<ScheduledTaskHistory>("ScheduledTasksHistory", id);
		}

		public List<ScheduledTaskHistory> ListScheduledTaskHistory(Guid scheduledTaskId)
		{
			return ListItems<ScheduledTaskHistory>(scheduledTaskId, "ScheduledTaskId", "ScheduledTasksHistory");
		}

		public void SaveScheduledTaskHistory(ScheduledTaskHistory history)
		{
			ScheduledTaskHistory current = GetScheduledTaskHistory(history.Id);

			if (current == null)
			{
				history.Id = AddScheduledTaskHistory(history);
			}
			else
			{
				UpdateScheduledTaskHistory(history);
			}
		}

		private Guid AddScheduledTaskHistory(ScheduledTaskHistory history)
		{
			if (history.Id == Guid.Empty) history.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ScheduledTasksHistory " +
				"(Id, ScheduledTaskId, StartDate, FinishDate, NextScheduledRun, Server, Status, DateAdded) " +
				"VALUES " +
				"(@Id, @ScheduledTaskId, @StartDate, @FinishDate, @NextScheduledRun, @Server, @Status, @DateAdded); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", history.Id),
					new SqliteParameter("@ScheduledTaskId", history.ScheduledTaskId),
					new SqliteParameter("@StartDate", history.StartDate),
					new SqliteParameter("@FinishDate", history.FinishDate),
					new SqliteParameter("@NextScheduledRun", history.NextScheduledRun),
					new SqliteParameter("@Server", history.Server),
					new SqliteParameter("@Status", history.Status),
					new SqliteParameter("@DateAdded", DateTime.UtcNow)
				});

			return history.Id;
		}

		private void UpdateScheduledTaskHistory(ScheduledTaskHistory history)
		{
			string commandText =
				"UPDATE ScheduledTasksHistory " +
				"SET " +
				"  StartDate = @StartDate, " +
				"  FinishDate = @FinishDate, " +
				"  NextScheduledRun = @NextScheduledRun, " +
				"  Server = @Server, " +
				"  Status = @Status, " +
				"  DateChanged = @DateChanged " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", history.Id),
					new SqliteParameter("@ScheduledTaskId", history.ScheduledTaskId),
					new SqliteParameter("@StartDate", history.StartDate),
					new SqliteParameter("@FinishDate", history.FinishDate),
					new SqliteParameter("@NextScheduledRun", history.NextScheduledRun),
					new SqliteParameter("@Server", history.Server),
					new SqliteParameter("@Status", history.Status),
					new SqliteParameter("@DateChanged", DateTime.UtcNow)
				});
		}

		public void DeleteScheduledTaskHistory(ScheduledTaskHistory history)
		{
			string commandText =
				"DELETE FROM ScheduledTasksHistory " +
				"WHERE Id = @Id";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", history.Id)
				});
		}

		#endregion

		#region "    IFileSystemDataProvider    "

		public Folder GetFolder(Guid folderId)
		{
			return GetItem<Folder>("FileSystemItems", folderId);
		}

		public Folder GetFolder(Site site, string provider, string path)
		{
			Folder result;

			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM FileSystemItems " +
				"WHERE SiteId = @SiteId " +
				"AND Provider = @Provider " +
				"AND Path = @Path ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@SiteId", site.Id),
						new SqliteParameter("@Provider", provider),
						new SqliteParameter("@Path", path)
					});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<Folder>(reader);
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

			return result;
		}

		public Folder SaveFolder(Site site, Folder folder)
		{
			Folder current = GetFolder(folder.Id);

			if (current == null)
			{
				folder.Id = AddFolder(site, folder);
			}
			else
			{
				UpdateFolder(folder);
			}

			return folder;
		}

		/// <summary>
		/// Create a new folder record
		/// </summary>
		/// <param name="folder"></param>
		private Guid AddFolder(Site site, Folder folder)
		{
			if (folder.Id == Guid.Empty) folder.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO FileSystemItems " +
				"(Id, SiteId, Provider, Path, ItemType, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @Provider, @Path, @ItemType, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", folder.Id),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Provider", folder.Provider),
					new SqliteParameter("@Path", folder.Path),
					new SqliteParameter("@ItemType", ItemTypes.Folder),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return folder.Id;
		}

		/// <summary>
		/// Update an existing folder record
		/// </summary>
		/// <param name="folder"></param>
		private void UpdateFolder(Folder folder)
		{
			string commandText =
				"UPDATE FileSystemItems " +
				"SET " +
				"  Path = @Path, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", folder.Id),
					new SqliteParameter("@Path", folder.Path),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public void DeleteFolder(Folder folder)
		{
			string commandText =
				"DELETE " +
				"FROM FileSystemItems " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", folder.Id)
					});
		}

		//
		public File GetFile(Guid fileId)
		{
			return GetItem<File>("FileSystemItems", fileId);
		}

		public File GetFile(Site site, string provider, string path)
		{
			File result;

			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM FileSystemItems " +
				"WHERE SiteId = @SiteId " +
				"AND Provider = @Provider " +
				"AND Path = @Path ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@SiteId", site.Id),
						new SqliteParameter("@Provider", provider),
						new SqliteParameter("@Path", path)
					});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<File>(reader);
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

			return result;
		}

		public File SaveFile(Site site, File file)
		{
			File current = GetFile(file.Id);

			if (current == null)
			{
				file.Id = AddFile(site, file);
			}
			else
			{
				UpdateFile(file);
			}

			return file;
		}

		/// <summary>
		/// Create a new file record
		/// </summary>
		/// <param name="file"></param>
		private Guid AddFile(Site site, File file)
		{
			if (file.Id == Guid.Empty) file.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO FileSystemItems " +
				"(Id, SiteId, Provider, Path, ItemType, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @SiteId, @Provider, @Path, @ItemType, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", file.Id),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@Provider", file.Provider),
					new SqliteParameter("@Path", file.Path),
					new SqliteParameter("@ItemType", ItemTypes.File),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return file.Id;
		}

		/// <summary>
		/// Update an existing file record
		/// </summary>
		/// <param name="file"></param>
		private void UpdateFile(File file)
		{
			string commandText =
				"UPDATE FileSystemItems " +
				"SET " +
				"  Path = @Path, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", file.Id),
					new SqliteParameter("@Path", file.Path),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		public void DeleteFile(File file)
		{
			string commandText =
				"DELETE " +
				"FROM FileSystemItems " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", file.Id)
					});
		}

		#endregion

		#region "    Site Groups    "

		public List<SiteGroup> ListSiteGroups()
		{
			return ListItems<SiteGroup>("SiteGroups");
		}

		public SiteGroup GetSiteGroup(Guid siteGroupId)
		{
			SiteGroup result;

			IDataReader reader;

			if (siteGroupId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM SiteGroups " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", siteGroupId)
					});

			try
			{
				if (reader.Read())
				{
					result = ModelExtensions.Create<SiteGroup>(reader);

					if (result != null)
					{
						result.PrimarySite = GetSite(DataHelper.GetGUID(reader, "PrimarySiteId"));
					}
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


			return result;
		}

		public void SaveSiteGroup(SiteGroup siteGroup)
		{
			SiteGroup current = GetSiteGroup(siteGroup.Id);

			if (current == null)
			{
				siteGroup.Id = AddSiteGroup(siteGroup);
				this.EventManager.RaiseEvent<SiteGroup, Create>(siteGroup);
			}
			else
			{
				UpdateSiteGroup(siteGroup);
				this.EventManager.RaiseEvent<SiteGroup, Update>(siteGroup);
			}
		}

		public void DeleteSiteGroup(SiteGroup siteGroup)
		{
			Boolean transactionStarted = false;

			try
			{
				transactionStarted = BeginTransaction();

				string commandTextRoles =
					"UPDATE Sites " +
					"SET SiteGroupId = NULL " +
					"WHERE SiteGroupId = @Id ";

				this.ExecuteNonQuery(
					commandTextRoles,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", siteGroup.Id)
						});

				string commandText =
					"DELETE " +
					"FROM SiteGroups " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandText,
						new SqliteParameter[]
						{
						new SqliteParameter("@Id", siteGroup.Id)
						});

				if (transactionStarted)
				{
					CommitTransaction();

				}
				this.EventManager.RaiseEvent<SiteGroup, Delete>(siteGroup);

			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting site group {0}", siteGroup.Id);
				if (transactionStarted)
				{
					RollbackTransaction();
				}
				throw;
			}
		}


		private Guid AddSiteGroup(SiteGroup siteGroup)
		{
			if (siteGroup.Id == Guid.Empty) siteGroup.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO SiteGroups " +
				"(Id, Name, Description, PrimarySiteId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @Name, @Description, @PrimarySiteId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", siteGroup.Id),
					new SqliteParameter("@Name", siteGroup.Name),
					new SqliteParameter("@Description", siteGroup.Description),
					new SqliteParameter("@PrimarySiteId", siteGroup.PrimarySite?.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return siteGroup.Id;
		}

		private void UpdateSiteGroup(SiteGroup siteGroup)
		{
			string commandText =
				"UPDATE SiteGroups " +
				"SET " +
				"  Name = @Name, " +
				"  Description = @Description, " +
				"  PrimarySiteId = @PrimarySiteId, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", siteGroup.Id),
					new SqliteParameter("@Name", siteGroup.Name),
					new SqliteParameter("@Description", siteGroup.Description),
					new SqliteParameter("@PrimarySiteId", siteGroup.PrimarySite?.Id),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}
		#endregion

		#region "    Lists    "
		public List<List> ListLists(Site site)
		{
			return ListItems<List>(site.Id, "SiteId", "Lists");
		}

		public List GetList(Guid listId)
		{
			IDataReader reader;

			if (listId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM Lists " +
				"WHERE Id = @Id ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", listId)
					});

			try
			{
				if (reader.Read())
				{
					List result = ModelExtensions.Create<List>(reader);
					if (result != null)
					{
						result.Items = ListListItems(result);
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

		public void SaveList(Site site, List list)
		{
			List current = GetList(list.Id);

			if (current == null)
			{
				list.Id = AddList(site, list);
				this.EventManager.RaiseEvent<List, Create>(list);
			}
			else
			{
				UpdateList(list);
				this.EventManager.RaiseEvent<List, Update>(list);
			}

			SaveListItems(list, list.Items, current?.Items);
		}

		private Guid AddList(Site site, List list)
		{
			if (list.Id == Guid.Empty) list.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Lists " +
				"(Id, Name, Description, SiteId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @Name, @Description, @SiteId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", list.Id),
					new SqliteParameter("@Name", list.Name),
					new SqliteParameter("@Description", list.Description),
					new SqliteParameter("@SiteId", site.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return list.Id;
		}

		/// <summary>
		/// Update an existing list record
		/// </summary>
		/// <param name="list"></param>
		private void UpdateList(List list)
		{
			string commandText =
				"UPDATE Lists " +
				"SET " +
				"  Name = @Name, " +
				"  Description = @Description, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", list.Id),
					new SqliteParameter("@Name", list.Name),
					new SqliteParameter("@Description", list.Description),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}

		public void DeleteList(List list)
		{
			string commandText =
				"DELETE " +
				"FROM Lists " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", list.Id)
					});

		}

		private void SaveListItems(List list, IEnumerable<ListItem> listItems, IEnumerable<ListItem> originalListItems)
		{
			if (listItems == null) return;

			if (originalListItems != null)
			{
				foreach (ListItem originalListItem in originalListItems)
				{
					Boolean found = false;

					foreach (ListItem newListItem in listItems)
					{
						if (newListItem.Id == originalListItem.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						DeleteListItem(originalListItem);
					}
				}
			}

			foreach (ListItem listItem in listItems)
			{
				Boolean found = false;

				if (originalListItems != null)
				{
					foreach (ListItem originalListItem in originalListItems)
					{
						if (listItem.Id == originalListItem.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					UpdateListItem(listItem);
				}
				else
				{
					AddListItem(list, listItem);
				}
			}
		}

		public List<ListItem> ListListItems(List list)
		{
			return ListItems<ListItem>(list.Id, "ListId", "ListItems");
		}

		public ListItem GetListItem(Guid listItemId)
		{
			return GetItem<ListItem>("ListItems", listItemId);
		}

		public void SaveListItem(List list, ListItem listItem)
		{
			ListItem current = GetListItem(listItem.Id);

			if (current == null)
			{
				listItem.Id = AddListItem(list, listItem);
				this.EventManager.RaiseEvent<ListItem, Create>(listItem);
			}
			else
			{
				UpdateListItem(listItem);
				this.EventManager.RaiseEvent<ListItem, Update>(listItem);
			}

		}

		private Guid AddListItem(List list, ListItem listItem)
		{
			if (listItem.Id == Guid.Empty) listItem.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ListItems " +
				"(Id, Name, Value, ListId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @Name, @Value, @ListId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", listItem.Id),
					new SqliteParameter("@Name", listItem.Name),
					new SqliteParameter("@Value", listItem.Value),
					new SqliteParameter("@ListId", list.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return listItem.Id;
		}

		/// <summary>
		/// Update an existing listItem record
		/// </summary>
		/// <param name="listItem"></param>
		private void UpdateListItem(ListItem listItem)
		{
			string commandText =
				"UPDATE ListItems " +
				"SET " +
				"  Name = @Name, " +
				"  Value = @Value, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", listItem.Id),
					new SqliteParameter("@Name", listItem.Name),
					new SqliteParameter("@Value", listItem.Value),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});

		}

		public void DeleteListItem(ListItem listItem)
		{
			string commandText =
				"DELETE " +
				"FROM ListItems " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", listItem.Id)
					});

		}
		#endregion
	}
}
