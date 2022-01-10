using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace Nucleus.Modules.Forums.DataProviders.SQLite
{
	/// <summary>
	/// Nucleus SQLite data provider.
	/// </summary>
	/// <remarks>
	/// This class implements all of the data provider interfaces for use with SQLite.
	/// </remarks>
	public class SQLiteDataProvider : Nucleus.Core.DataProviders.Abstractions.SQLiteDataProvider, IForumsDataProvider
	{
		private const string SCRIPT_NAMESPACE = "Nucleus.Modules.Forums.DataProviders.SQLite.Scripts.";
		private const string SCHEMA_NAME = "Nucleus.Modules.Forums";

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

		#region "    Groups    "
		public Group GetGroup(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM ForumGroups " +
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
					Group result = ModelExtensions.Create<Group>(reader);
					if (result != null)
					{
						result.Settings = GetSettings(result.Id);
						result.AttachmentsFolder = new() { Id = DataHelper.GetGUID(reader, "AttachmentsFolderId") };
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

		public IList<Group> ListGroups(PageModule pageModule)
		{
			IDataReader reader;
			List<Group> results = new();

			string commandText =
					"SELECT * " +
					"FROM ForumGroups " +
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
					Group item = ModelExtensions.Create<Group>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void SaveGroup(PageModule module, Group group)
		{
			Group current = GetGroup(group.Id);

			if (current == null)
			{
				group.Id = AddGroup(module, group);
				this.EventManager.RaiseEvent<Group, Create>(group);
			}
			else
			{
				UpdateGroup(group);
				this.EventManager.RaiseEvent<Group, Update>(group);
			}

			if (group.Settings != null)
			{
				// save settings
				SaveSettings(group.Id, group.Settings);
			}


		}

		public void DeleteGroup(Group groups)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM ForumGroups " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", groups.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Group, Delete>(groups);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Group {0}", groups.Id);
				RollbackTransaction();
				throw;
			}

		}

		private long GetTopForumGroupSortOrder(Guid moduleId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM ForumGroups " +
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

		private Guid AddGroup(PageModule module, Group group)
		{
			if (group.Id == Guid.Empty) group.Id = Guid.NewGuid();

			group.SortOrder = GetTopForumGroupSortOrder(module.Id) + 10;

			string commandText =
				"INSERT INTO ForumGroups " +
				"(Id, ModuleId, Name, SortOrder, AttachmentsFolderId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ModuleId, @Name, @SortOrder, @AttachmentsFolderId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", group.Id),
					new SqliteParameter("@ModuleId", module.Id),
					new SqliteParameter("@Name", group.Name),
					new SqliteParameter("@SortOrder", group.SortOrder),
					new SqliteParameter("@AttachmentsFolderId", group.AttachmentsFolder?.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return group.Id;
		}

		private void UpdateGroup(Group group)
		{
			string commandText =
					"UPDATE ForumGroups " +
					"SET " +
					"  Name = @Name, " +
					"  SortOrder = @SortOrder, " +
					"  AttachmentsFolderId = @AttachmentsFolderId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", group.Id),
						new SqliteParameter("@Name", group.Name),
						new SqliteParameter("@SortOrder", group.SortOrder),
						new SqliteParameter("@AttachmentsFolderId", group.AttachmentsFolder?.Id),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}
		#endregion

		#region "    Forums    "
		public Forum GetForum(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM Forums " +
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
					Forum result = ModelExtensions.Create<Forum>(reader);
					if (result != null)
					{
						result.Settings = GetSettings(result.Id);
						result.Statistics = GetForumStatistics(result.Id);
						result.AttachmentsFolder = new() { Id = DataHelper.GetGUID(reader, "AttachmentsFolderId") };

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

		public Guid GetForumGroupId(Forum forum)
		{
			string value;

			string commandText =
				"SELECT ForumGroupId " +
				"FROM Forums " +
				"WHERE Id = @Id ";

			value = (string)this.ExecuteScalar(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", forum.Id)
					});

			if (value == null)
			{
				return Guid.Empty;
			}
			else
			{
				return Guid.Parse(value);
			}
		}

		private ForumStatistics GetForumStatistics(Guid forumId)
		{
			IDataReader reader;

			string commandText =
				"SELECT Count(ForumPosts.Id) AS PostCount, " +
				"( " +
				"	 SELECT Count(ForumReplies.Id) AS ReplyCount " +
				"	 FROM ForumReplies " +
				"	 WHERE ForumReplies.ForumPostId IN " +
				"	 ( " +
				"	   SELECT Id " +
				"	   FROM ForumPosts " +
				"    WHERE ForumPosts.ForumId = @ForumId " +
				"	 ) " +
				") AS ReplyCount " +
				"FROM ForumPosts " +
				"WHERE ForumPosts.ForumId = @ForumId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ForumId", forumId)
					});

			try
			{
				if (reader.Read())
				{
					ForumStatistics result = ModelExtensions.Create<ForumStatistics>(reader);
					if (result != null)
					{
						result.LastPost = GetLastPost(forumId);
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

		private Post GetLastPost(Guid forumId)
		{
			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM ForumPosts " +
				"WHERE ForumId = @ForumId " +
				"ORDER BY DateAdded DESC " +
				"LIMIT 1";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ForumId", forumId)
					});

			try
			{
				if (reader.Read())
				{
					Post result = ModelExtensions.Create<Post>(reader);
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

		public IList<Forum> ListForums(Group group)
		{
			IDataReader reader;
			List<Forum> results = new();

			string commandText =
					"SELECT * " +
					"FROM Forums " +
					"WHERE ForumGroupId = @ForumGroupId ";

			reader = this.ExecuteReader(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ForumGroupId", group.Id)
				});

			try
			{
				while (reader.Read())
				{
					Forum item = ModelExtensions.Create<Forum>(reader);
					results.Add(item);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}


		public void SaveForum(Group group, Forum forum)
		{
			Forum current = GetForum(forum.Id);

			if (current == null)
			{
				forum.Id = AddForum(group, forum);
				this.EventManager.RaiseEvent<Forum, Create>(forum);
			}
			else
			{
				UpdateForum(forum);
				this.EventManager.RaiseEvent<Forum, Update>(forum);
			}

			if (forum.Settings != null)
			{
				// save settings
				SaveSettings(forum.Id, forum.Settings);
			}
		}

		public void DeleteForum(Forum forums)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM Forums " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", forums.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Forum, Delete>(forums);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Forum {0}", forums.Id);
				RollbackTransaction();
				throw;
			}
		}

		private long GetTopForumSortOrder(Guid groupId)
		{
			object topSortOrder;

			string commandText =
				"SELECT SortOrder " +
				"FROM Forums " +
				"WHERE ForumGroupId = @ForumGroupId " +
				"ORDER BY SortOrder DESC " +
				"LIMIT 1; ";

			topSortOrder = this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@ForumGroupId", groupId)
				});

			return topSortOrder == null ? 10 : (long)topSortOrder;
		}

		private Guid AddForum(Group group, Forum forum)
		{
			if (forum.Id == Guid.Empty) forum.Id = Guid.NewGuid();

			forum.SortOrder = GetTopForumSortOrder(group.Id) + 10;

			string commandText =
				"INSERT INTO Forums " +
				"(Id, ForumGroupId, Name, Description, SortOrder, UseGroupSettings, AttachmentsFolderId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ForumGroupId, @Name, @Description, @SortOrder, @UseGroupSettings, @AttachmentsFolderId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", forum.Id),
					new SqliteParameter("@ForumGroupId", group.Id),
					new SqliteParameter("@Name", forum.Name),
					new SqliteParameter("@Description", forum.Description),
					new SqliteParameter("@UseGroupSettings", forum.UseGroupSettings),
					new SqliteParameter("@SortOrder", forum.SortOrder),
					new SqliteParameter("@AttachmentsFolderId", forum.AttachmentsFolder?.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return forum.Id;
		}

		private void UpdateForum(Forum forum)
		{
			string commandText =
					"UPDATE Forums " +
					"SET " +
					"  Name = @Name, " +
					"  Description = @Description, " +
					"  UseGroupSettings = @UseGroupSettings, " +
					"  AttachmentsFolderId = @AttachmentsFolderId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", forum.Id),
						new SqliteParameter("@Name", forum.Name),
						new SqliteParameter("@Description", forum.Description),
						new SqliteParameter("@UseGroupSettings", forum.UseGroupSettings),
						new SqliteParameter("@AttachmentsFolderId", forum.AttachmentsFolder?.Id),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		#endregion

		#region "    Settings    "
		public Settings GetSettings(Guid relatedId)
		{
			IDataReader reader;

			if (relatedId == Guid.Empty) return null;

			string commandText =
				"SELECT * " +
				"FROM ForumSettings " +
				"WHERE RelatedId = @RelatedId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@RelatedId", relatedId)
					});

			try
			{
				if (reader.Read())
				{
					Settings result = ModelExtensions.Create<Settings>(reader);
					if (result != null)
					{
						result.StatusList = this.ListManager.Get(DataHelper.GetGUID(reader, "StatusListId"));
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

		public void SaveSettings(Guid relatedId, Settings settings)
		{
			Settings current = GetSettings(relatedId);

			if (current == null)
			{
				AddSettings(relatedId, settings);

			}
			else
			{
				UpdateSettings(relatedId, settings);
			}

		}

		private void AddSettings(Guid relatedId, Settings settings)
		{
			string commandText =
				"INSERT INTO ForumSettings " +
				"(RelatedId, Enabled, Visible, IsModerated, AllowAttachments, AllowSearchIndexing, StatusListId, SubscriptionMailTemplateId, ModerationRequiredMailTemplateId, ModerationApprovedMailTemplateId, ModerationRejectedMailTemplateId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@RelatedId, @Enabled, @Visible, @IsModerated, @AllowAttachments, @AllowSearchIndexing, @StatusListId, @SubscriptionMailTemplateId, @ModerationRequiredMailTemplateId, @ModerationApprovedMailTemplateId, @ModerationRejectedMailTemplateId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@RelatedId", relatedId),
					new SqliteParameter("@Enabled", settings.Enabled),
					new SqliteParameter("@Visible", settings.Visible),
					new SqliteParameter("@IsModerated", settings.IsModerated),
					new SqliteParameter("@AllowAttachments", settings.AllowAttachments),
					new SqliteParameter("@AllowSearchIndexing", settings.AllowSearchIndexing),
					new SqliteParameter("@StatusListId", settings.StatusList?.Id),
					new SqliteParameter("@SubscriptionMailTemplateId", settings.SubscriptionMailTemplateId),
					new SqliteParameter("@ModerationRequiredMailTemplateId", settings.ModerationRequiredMailTemplateId),
					new SqliteParameter("@ModerationApprovedMailTemplateId", settings.ModerationApprovedMailTemplateId),
					new SqliteParameter("@ModerationRejectedMailTemplateId", settings.ModerationRejectedMailTemplateId),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

		}

		private void UpdateSettings(Guid relatedId, Settings settings)
		{
			string commandText =
					"UPDATE ForumSettings " +
					"SET " +
					"  Enabled = @Enabled, " +
					"  Visible = @Visible, " +
					"  IsModerated = @IsModerated, " +
					"  AllowAttachments = @AllowAttachments, " +
					"  AllowSearchIndexing = @AllowSearchIndexing, " +
					"  StatusListId = @StatusListId, " +
					"  SubscriptionMailTemplateId = @SubscriptionMailTemplateId, " +
					"  ModerationRequiredMailTemplateId = @ModerationRequiredMailTemplateId, " +
					"  ModerationApprovedMailTemplateId = @ModerationApprovedMailTemplateId, " +
					"  ModerationRejectedMailTemplateId = @ModerationRejectedMailTemplateId, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE RelatedId = @RelatedId ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@RelatedId", relatedId),
						new SqliteParameter("@Enabled", settings.Enabled),
						new SqliteParameter("@Visible", settings.Visible),
						new SqliteParameter("@IsModerated", settings.IsModerated),
						new SqliteParameter("@AllowAttachments", settings.AllowAttachments),
						new SqliteParameter("@AllowSearchIndexing", settings.AllowSearchIndexing),
						new SqliteParameter("@StatusListId", settings.StatusList?.Id),
						new SqliteParameter("@SubscriptionMailTemplateId", settings.SubscriptionMailTemplateId),
						new SqliteParameter("@ModerationRequiredMailTemplateId", settings.ModerationRequiredMailTemplateId),
						new SqliteParameter("@ModerationApprovedMailTemplateId", settings.ModerationApprovedMailTemplateId),
						new SqliteParameter("@ModerationRejectedMailTemplateId", settings.ModerationRejectedMailTemplateId),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}
		#endregion

		#region "    Posts    "
		public Post GetForumPost(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return default(Post);

			string commandText =
				"SELECT * " +
				"FROM ForumPosts " +
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
					Post post = ModelExtensions.Create<Post>(reader);
					if (post != null)
					{
						post.Status = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "StatusId"));
						post.Statistics = GetPostStatistics(post.Id);
						post.Attachments = ListPostAttachments(post.Id);
					}
					return post;
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

		public IList<Post> ListForumPosts(Forum forum, FlagStates approved)
		{
			List<Post> results = new();
			IDataReader reader;

			if (approved == FlagStates.IsAny)
			{
				string commandText =
				"SELECT * " +
				"FROM ForumPosts " +
				"WHERE ForumId = @ForumId " +
				"ORDER BY IsPinned DESC, DateAdded DESC";

				reader = this.ExecuteReader(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@ForumId", forum.Id)
					});
			}
			else
			{
				string commandText =
				"SELECT * " +
				"FROM ForumPosts " +
				"WHERE ForumId = @ForumId " +
				"AND IsApproved = @Approved " +
				"ORDER BY IsPinned DESC, DateAdded DESC";

				reader = this.ExecuteReader(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@ForumId", forum.Id),
					new SqliteParameter("@Approved", approved)
					});
			}

			try
			{
				while (reader.Read())
				{
					Post post = ModelExtensions.Create<Post>(reader);
					post.Status = this.ListManager.GetListItem(DataHelper.GetGUID(reader, "StatusId"));
					post.Statistics = GetPostStatistics(post.Id);

					results.Add(post);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;
		}

		public void DeleteForumPost(Post post)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM ForumPosts " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", post.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Post, Delete>(post);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Post {0}", post.Id);
				RollbackTransaction();
				throw;
			}
		}

		public void SaveForumPost(Forum forum, Post post)
		{
			Post current = GetForumPost(post.Id);

			if (current == null)
			{
				post.Id = AddForumPost(forum, post);
				this.EventManager.RaiseEvent<Post, Create>(post);
			}
			else
			{
				UpdateForumPost(post);
				this.EventManager.RaiseEvent<Post, Update>(post);
			}

			SaveAttachments(post.Id, Guid.Empty, post.Attachments, current.Attachments);
		}

		public void SaveAttachments(Guid postId, Guid replyId, IEnumerable<Attachment> attachments, IEnumerable<Attachment> originalAttachments)
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
					AddAttachment(postId, replyId, attachment);
				}
			}
		}

		private Guid AddAttachment(Guid postId, Guid replyId, Attachment attachment)
		{
			if (attachment.Id == Guid.Empty) attachment.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ForumAttachments " +
				"(Id, ForumPostId, ForumReplyId, FileId, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ForumPostId, @ForumReplyId, @FileId, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", attachment.Id),
					new SqliteParameter("@ForumPostId", postId),
					new SqliteParameter("@ForumReplyId", replyId),
					new SqliteParameter("@FileId", attachment.File.Id),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return attachment.Id;
		}

		
		public void DeleteAttachment(Guid Id)
		{
			string commandText =
				"DELETE FROM ForumAttachments " +
				"WHERE Id = @Id; ";

			this.ExecuteNonQuery(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", Id)
				});
		}

		private Guid AddForumPost(Forum forum, Post post)
		{
			if (post.Id == Guid.Empty) post.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ForumPosts " +
				"(Id, ForumId, Subject, Body, IsLocked, IsPinned, IsApproved, Status, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ForumId, @Subject, @Body, @IsLocked, @IsPinned, @IsApproved, @Status, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", post.Id),
					new SqliteParameter("@ForumId", forum.Id),
					new SqliteParameter("@Subject", post.Subject),
					new SqliteParameter("@Body", post.Body),
					new SqliteParameter("@IsLocked", post.IsLocked),
					new SqliteParameter("@IsPinned", post.IsPinned),
					new SqliteParameter("@IsApproved", post.IsApproved),
					new SqliteParameter("@Status", post.Status),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return post.Id;
		}

		private void UpdateForumPost(Post post)
		{
			string commandText =
					"UPDATE ForumPosts " +
					"SET " +
					"  Subject = @Subject, " +
					"  Body = @Body, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			// Locked, pinned, approved and status updates must use specific calls
			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", post.Id),
						new SqliteParameter("@Subject", post.Subject),
						new SqliteParameter("@Body", post.Body),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		public void SetForumPostPinned(Post post, Boolean value)
		{
			string commandText =
					"UPDATE ForumPosts " +
					"SET " +
					"  IsPinned = @Value, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", post.Id),
						new SqliteParameter("@Value", value),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		public void SetForumPostLocked(Post post, Boolean value)
		{
			string commandText =
					"UPDATE ForumPosts " +
					"SET " +
					"  IsLocked = @Value, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", post.Id),
						new SqliteParameter("@Value", value),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		public void SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value)
		{
			string commandText =
					"UPDATE ForumPosts " +
					"SET " +
					"  StatusId = @Value, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			// Locked, pinned, approved and status updates must use specific calls
			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@Id", post.Id),
					new SqliteParameter("@Value", value?.Id),
					new SqliteParameter("@DateChanged", DateTime.UtcNow),
					new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		public void SetForumPostApproved(Post post, Boolean value)
		{
			string commandText =
					"UPDATE ForumPosts " +
					"SET " +
					"  IsApproved = @Value, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			// Locked, pinned, approved and status updates must use specific calls
			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", post.Id),
						new SqliteParameter("@Value", value),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		private PostStatistics GetPostStatistics(Guid postId)
		{
			IDataReader reader;

			string commandText =
				"SELECT Count(ForumPosts.Id) AS PostCount, Count(ForumReplies.Id) AS ReplyCount " +
				"FROM ForumPosts " +
				"LEFT JOIN ForumReplies ON ForumReplies.ForumPostId IN " +
				"  (SELECT Id FROM ForumPosts WHERE ForumPosts.Id = @PostId) " +
				"WHERE ForumPosts.Id = @PostId ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@PostId", postId)
					});

			try
			{
				if (reader.Read())
				{
					PostStatistics result = ModelExtensions.Create<PostStatistics>(reader);
					if (result != null)
					{
						result.LastReply = GetLastReply(postId);
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

		public List<Attachment> ListPostAttachments(Guid postId)
		{
			IDataReader reader;
			List<Attachment> results = new();
						
			string commandText =
				"SELECT * " +
				"FROM ForumAttachments " +
				"WHERE ForumPostId = @PostId " + 
				"AND ForumReplyId IS NULL ";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@PostId", postId)
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

		public List<Attachment> ListReplyAttachments(Guid postId, Guid replyId)
		{
			IDataReader reader;
			List<Attachment> results = new();

			string commandText =
				"SELECT * " +
				"FROM ForumAttachments " +
				"WHERE ForumPostId = @PostId " +
				"AND ForumReplyId = @ReplyId";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@PostId", postId),
						new SqliteParameter("@ReplyId", replyId)
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

		public Reply GetForumPostReply(Guid id)
		{
			IDataReader reader;

			if (id == Guid.Empty) return default(Reply);

			string commandText =
				"SELECT * " +
				"FROM ForumReplies " +
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
					Reply reply = ModelExtensions.Create<Reply>(reader);
					if (reply != null)
					{
						reply.Attachments = ListReplyAttachments(reply.ForumPostId, reply.Id);
					}
					return reply;
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

		public IList<Reply> ListForumPostReplies(Post post, FlagStates approved)
		{
			List<Reply> results = new();
			IDataReader reader;

			if (approved == FlagStates.IsAny)
			{
				string commandText =
				"SELECT * " +
				"FROM ForumReplies " +
				"WHERE ForumPostId = @ForumPostId " +
				"ORDER BY DateAdded ";

				reader = this.ExecuteReader(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@ForumPostId", post.Id)
					});
			}
			else
			{
				string commandText =
				"SELECT * " +
				"FROM ForumReplies " +
				"WHERE ForumPostId = @ForumPostId " +
				"AND IsApproved = @Approved " +
				"ORDER BY DateAdded ";

				reader = this.ExecuteReader(
					commandText,
					new SqliteParameter[]
					{
					new SqliteParameter("@ForumPostId", post.Id),
					new SqliteParameter("@Approved", approved)
					});
			}

			try
			{
				while (reader.Read())
				{
					Reply reply = ModelExtensions.Create<Reply>(reader);

					if (reply != null)
					{
						reply.Attachments = ListReplyAttachments(post.Id, reply.Id);
					}

					results.Add(reply);
				}
			}
			finally
			{
				reader.Close();
			}

			return results;

		}

		public void DeleteForumPostReply(Reply reply)
		{
			try
			{
				BeginTransaction();

				string commandTextPermissions =
					"DELETE " +
					"FROM ForumReplies " +
					"WHERE Id = @Id ";

				this.ExecuteNonQuery(
					commandTextPermissions,
						new SqliteParameter[]
						{
							new SqliteParameter("@Id", reply.Id)
						});

				CommitTransaction();

				this.EventManager.RaiseEvent<Reply, Delete>(reply);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Deleting Reply {0}", reply.Id);
				RollbackTransaction();
				throw;
			}
		}

		public void SaveForumPostReply(Post post, Reply reply)
		{
			Reply current = GetForumPostReply(reply.Id);

			if (current == null)
			{
				post.Id = AddForumPostReply(post, reply);
				this.EventManager.RaiseEvent<Post, Create>(post);
			}
			else
			{
				UpdateForumPostReply(reply);
				this.EventManager.RaiseEvent<Post, Update>(post);
			}

			SaveAttachments(post.Id, reply.Id, reply.Attachments, current.Attachments);
		}

		private Guid AddForumPostReply(Post post, Reply reply)
		{
			if (reply.Id == Guid.Empty) reply.Id = Guid.NewGuid();

			string commandText =
				"INSERT INTO ForumReplies " +
				"(Id, ForumPostId, Body, IsApproved, DateAdded, AddedBy) " +
				"VALUES " +
				"(@Id, @ForumPostId, @Body, @IsApproved, @DateAdded, @AddedBy); ";

			this.ExecuteScalar(
				commandText,
				new SqliteParameter[]
				{
					new SqliteParameter("@Id", reply.Id),
					new SqliteParameter("@ForumPostId", post.Id),
					new SqliteParameter("@Body", reply.Body),
					new SqliteParameter("@IsApproved", reply.IsApproved),
					new SqliteParameter("@DateAdded", DateTime.UtcNow),
					new SqliteParameter("@AddedBy", CurrentUserId())
				});

			return reply.Id;
		}

		private void UpdateForumPostReply(Reply reply)
		{
			string commandText =
					"UPDATE ForumReplies " +
					"SET " +
					"  Body = @Body, " +
					"  IsApproved = @IsApproved, " +
					"  DateChanged = @DateChanged, " +
					"  ChangedBy = @ChangedBy " +
					"WHERE Id = @Id ";

			this.ExecuteNonQuery(
					commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@Id", reply.Id),
						new SqliteParameter("@Body", reply.Body),
						new SqliteParameter("@IsApproved", reply.IsApproved),
						new SqliteParameter("@DateChanged", DateTime.UtcNow),
						new SqliteParameter("@ChangedBy", CurrentUserId())
					});
		}

		private Reply GetLastReply(Guid postId)
		{
			IDataReader reader;

			string commandText =
				"SELECT * " +
				"FROM ForumReplies " +
				"WHERE ForumPostId = @ForumPostId " +
				"ORDER BY DateAdded DESC " +
				"LIMIT 1";

			reader = this.ExecuteReader(
				commandText,
					new SqliteParameter[]
					{
						new SqliteParameter("@ForumPostId", postId)
					});

			try
			{
				if (reader.Read())
				{
					Reply result = ModelExtensions.Create<Reply>(reader);
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


		#endregion
	}
}
