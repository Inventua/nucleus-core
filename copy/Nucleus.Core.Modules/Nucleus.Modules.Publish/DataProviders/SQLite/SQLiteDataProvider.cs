using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Modules.Publish.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Nucleus.Core.DataProviders.Abstractions.SQLiteDataProvider, IArticleDataProvider
	{
		private const string SCRIPT_NAMESPACE = "Nucleus.Modules.Publish.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "Nucleus.Modules.Publish";

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

		public Article Get(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
					"SELECT * " +
					"FROM Articles " +
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
					Article result = ModelExtensions.Create<Article>(reader);

					if (result != null)
					{
						result.ImageFile = new() { Id = DataHelper.GetGUID(reader, "ImageFileId") };
						result.Attachments = ListAttachments(result.Id);
						result.Categories = ListCategories(result.Id);
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

		public Article Find(PageModule module, string encodedTitle)
		{
			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM Articles " +
				"WHERE ModuleId = @ModuleId " + 
				"AND EncodedTitle = @EncodedTitle ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ModuleId", module.Id),
						new SqliteParameter("@EncodedTitle", encodedTitle)
					});
			try
			{
				if (reader.Read())
				{
					Article result = ModelExtensions.Create<Article>(reader);

					if (result != null)
					{
						result.ImageFile = new() { Id = DataHelper.GetGUID(reader, "ImageFileId") };
						result.Attachments = ListAttachments(result.Id);
						result.Categories = ListCategories(result.Id);
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

		public IList<Article> List(PageModule pageModule)
		{
			IDataReader reader;
			List<Article> results = new();

			string commandText =
				"SELECT * " +
				"FROM Articles " +
				"WHERE ModuleId = @ModuleId ";

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
					Article item = ModelExtensions.Create<Article>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public PagedResult<Article> List(PageModule pageModule, PagingSettings settings)
		{
			IDataReader reader;
			List<Article> results = new();

			string whereText =
				"AND Enabled = 1 " +
				"AND (PublishDate IS NULL OR PublishDate <= @DateNow) " +
				"AND (ExpireDate IS NULL OR ExpireDate >= @DateNow) ";

			string countCommandText =
				"SELECT Count(Id) " +
				"FROM Articles " +
				"WHERE ModuleId = @ModuleId " + whereText;

			settings.TotalCount = (long)this.ExecuteScalar(
				countCommandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", pageModule.Id),
					new SqliteParameter("@DateNow", DateTime.UtcNow)
				});
			
			string commandText =
				"SELECT * " +
				"FROM Articles " +
				"WHERE ModuleId = @ModuleId " +
				whereText + 
				"ORDER BY Featured DESC, DateAdded DESC " +
				"LIMIT @FirstRowIndex, @LastRowIndex ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ModuleId", pageModule.Id),
					new SqliteParameter("@DateNow", DateTime.UtcNow),
					new SqliteParameter("@FirstRowIndex", settings.FirstRowIndex),
					new SqliteParameter("@LastRowIndex", settings.PageSize)
				});

			try
			{
				while (reader.Read())
				{
					Article item = ModelExtensions.Create<Article>(reader);
					item.ImageFile = new() { Id = DataHelper.GetGUID(reader, "ImageFileId") };
					item.Attachments = ListAttachments(item.Id);
					item.Categories = ListCategories(item.Id);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return new PagedResult<Article>(settings, results);

		}

		public void Save(PageModule pageModule, Article article)
		{
			Article current = Get(article.Id);

			if (current == null)
			{
				article.Id = AddArticle(pageModule, article);
				this.EventManager.RaiseEvent<Article, Create>(article);
			}
			else
			{
				UpdateArticle(article);
				this.EventManager.RaiseEvent<Article, Update>(article);
			}

			SaveAttachments(article.Id, article.Attachments, current.Attachments);
			SaveCategories(article.Id, article.Categories, current.Categories);

		}

		public void Delete(Article article)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
						"DELETE " +
						"FROM Articles " +
						"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", article.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Article, Delete>(article);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Article {0}", article.Id);
				RollbackTransaction();
				throw;
			}

		}

		private Guid AddArticle(PageModule pageModule, Article article)
		{
			if (article.Id == Guid.Empty) article.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO Articles " +
				"(Id, ModuleId, Title, EncodedTitle, SubTitle, Description, Summary, Body, ImageFileId, PublishDate, ExpireDate, Enabled, Featured, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ModuleId, @Title, @EncodedTitle, @SubTitle, @Description, @Summary, @Body, @ImageFileId, @PublishDate, @ExpireDate, @Enabled, @Featured, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", article.Id),
					new SqliteParameter("@ModuleId", pageModule.Id),
					new SqliteParameter("@Title", article.Title),
					new SqliteParameter("@EncodedTitle", article.EncodedTitle()),
					new SqliteParameter("@SubTitle", article.SubTitle),
					new SqliteParameter("@Description", article.Description),
					new SqliteParameter("@Summary", article.Summary),
					new SqliteParameter("@Body", article.Body),
					new SqliteParameter("@ImageFileId", article.ImageFile?.Id),
					new SqliteParameter("@PublishDate", article.PublishDate.UtcDateTime),
					new SqliteParameter("@ExpireDate", article.ExpireDate.UtcDateTime),
					new SqliteParameter("@Enabled", article.Enabled),
					new SqliteParameter("@Featured", article.Featured),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return article.Id;
		}

		private void UpdateArticle(Article article)
		{
			string commandText =
				"UPDATE Articles " +
				"SET " +
				"  Title = @Title, " +
				"  EncodedTitle = @EncodedTitle, " +
				"  SubTitle = @SubTitle, " +
				"  Description = @Description, " +
				"  Summary = @Summary, " +
				"  Body = @Body, " +
				"  ImageFileId = @ImageFileId, " +
				"  PublishDate = @PublishDate, " +
				"  ExpireDate = @ExpireDate, " +
				"  Enabled = @Enabled, " +
				"  Featured = @Featured, " +
				"  DateChanged = @DateChanged, " +
				"  ChangedBy = @ChangedBy " +
				"WHERE Id = @Id ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", article.Id),
					new SqliteParameter("@Title", article.Title),
					new SqliteParameter("@EncodedTitle", article.EncodedTitle()),
					new SqliteParameter("@SubTitle", article.SubTitle),
					new SqliteParameter("@Description", article.Description),
					new SqliteParameter("@Summary", article.Summary),
					new SqliteParameter("@Body", article.Body),
					new SqliteParameter("@ImageFileId", article.ImageFile?.Id),
					new SqliteParameter("@PublishDate", article.PublishDate.UtcDateTime),
					new SqliteParameter("@ExpireDate", article.ExpireDate.UtcDateTime),
					new SqliteParameter("@Enabled", article.Enabled),
					new SqliteParameter("@Featured", article.Featured),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
				});
		}

		private void SaveAttachments(Guid articleId, IEnumerable<Attachment> attachments, IEnumerable<Attachment> originalAttachments)
		{
			if (attachments == null) return;

			if (originalAttachments != null)
			{
				foreach (Attachment originalAttachment in originalAttachments)
				{
					Boolean found = false;

					foreach (Attachment newAttachment in attachments)
					{
						if (newAttachment.Id == originalAttachment.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						DeleteAttachment(originalAttachment.Id);
					}
				}
			}

			foreach (Attachment attachment in attachments)
			{
				Boolean found = false;

				if (originalAttachments != null)
				{
					foreach (Attachment originalAttachment in originalAttachments)
					{
						if (attachment.Id == originalAttachment.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					// attachments are never updated
				}
				else
				{
					AddAttachment(articleId, attachment);
				}
			}
		}

		private Guid AddAttachment(Guid articleId, Attachment attachment)
		{
			if (attachment.Id == Guid.Empty) attachment.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ArticleAttachments " +
				"(Id, ArticleId, FileId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ArticleId,  @FileId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", attachment.Id),
					new SqliteParameter("@ArticleId", articleId),
					new SqliteParameter("@FileId", attachment.File.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return attachment.Id;
		}


		public List<Attachment> ListAttachments(Guid articleId)
		{
			IDataReader reader;
			List<Attachment> results = new();

			string commandText =
				"SELECT * " +
				"FROM ArticleAttachments " +
				"WHERE ArticleId = @ArticleId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ArticleId", articleId)
					});

			try
			{
				while (reader.Read())
				{
					Attachment result = ModelExtensions.Create<Attachment>(reader);
					if (result != null)
					{
						result.File = new Abstractions.Models.FileSystem.File { Id = DataHelper.GetGUID(reader, "FileId") };
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


		private void DeleteAttachment(Guid Id)
		{
			string commandText =
				"DELETE FROM ArticleAttachments " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", Id)
				});
		}

		private void SaveCategories(Guid articleId, IEnumerable<Category> categories, IEnumerable<Category> originalCategories)
		{
			if (categories == null) return;

			if (originalCategories != null)
			{
				foreach (Category originalCategory in originalCategories)
				{
					Boolean found = false;

					foreach (Category newCategory in categories)
					{
						if (newCategory.Id == originalCategory.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						DeleteCategory(originalCategory.Id);
					}
				}
			}

			foreach (Category category in categories)
			{
				Boolean found = false;

				if (originalCategories != null)
				{
					foreach (Category originalCategory in originalCategories)
					{
						if (category.Id == originalCategory.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					// categorys are never updated
				}
				else
				{
					AddCategory(articleId, category);
				}
			}
		}

		private Guid AddCategory(Guid articleId, Category category)
		{
			if (category.Id == Guid.Empty) category.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ArticleCategories " +
				"(Id, ArticleId, CategoryListItemId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ArticleId,  @CategoryListItemId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", category.Id),
					new SqliteParameter("@ArticleId", articleId),
					new SqliteParameter("@CategoryListItemId", category.CategoryItem.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return category.Id;
		}

		private void DeleteCategory(Guid Id)
		{
			string commandText =
				"DELETE FROM ArticleCategories " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", Id)
				});
		}

		public List<Category> ListCategories(Guid articleId)
		{
			IDataReader reader;
			List<Category> results = new();

			string commandText =
				"SELECT * " +
				"FROM ArticleCategories " +
				"WHERE ArticleId = @ArticleId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ArticleId", articleId)
					});

			try
			{
				while (reader.Read())
				{
					Category result = ModelExtensions.Create<Category>(reader);
					if (result != null)
					{
						result.CategoryItem = new Abstractions.Models.ListItem { Id = DataHelper.GetGUID(reader, "CategoryListItemId") };
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

	}
}
