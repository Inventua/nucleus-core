using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Forums
{
	public class ForumsMetaDataProducer : IContentMetaDataProducer
	{
		private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider  { get; } = new();

		private IFileSystemManager FileSystemManager { get; }
		private GroupsManager GroupsManager { get; }		
		private ForumsManager ForumsManager { get; }
		private IExtensionManager ExtensionManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }

		private ILogger<ForumsMetaDataProducer> Logger { get; }

		public ForumsMetaDataProducer(ISiteManager siteManager, GroupsManager groupsManager, ForumsManager forumsManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IExtensionManager extensionManager, ILogger<ForumsMetaDataProducer> logger)
		{
			this.FileSystemManager = fileSystemManager;
			this.GroupsManager = groupsManager;
			this.ForumsManager = forumsManager;
			this.ExtensionManager = extensionManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.Logger = logger;
		}

		public override async Task<IEnumerable<ContentMetaData>> ListItems(Site site)
		{
			// This must match the value in package.xml
			Guid moduleDefinitionId = Guid.Parse("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

			List<ContentMetaData> results = new();

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {0} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
				{
					foreach (Models.Group group in await this.GroupsManager.List(module))
					{
						foreach (Models.Forum forum in await this.ForumsManager.List(group))
						{
							results.AddRange(await BuildContentMetaData(site, module, forum));
						}
					}
				}
			}

			return results.Where(result=>result != null);
		}

		/// <summary>
		/// Return a meta-data entries for each post (including replies in the content body)
		/// </summary>
		/// <param name="site"></param>
		/// <param name="document"></param>
		/// <returns></returns>
		private async Task<List<ContentMetaData>> BuildContentMetaData(Site site, PageModule module, Models.Forum forum)
		{
			Page page = await this.PageManager.Get(module.PageId);
			Uri pageUri = null;
			List<ContentMetaData> results = new();

			if (page != null)
			{
				string pageUrl = UrlHelperExtensions.RelativePageLink(page);

				if (!String.IsNullOrEmpty(pageUrl))
				{
					pageUri = new System.Uri(new System.Uri("http" + Uri.SchemeDelimiter + site.DefaultSiteAlias.Alias), pageUrl.Replace("~", ""));
				}
			}

			if (pageUri != null)
			{
				foreach (Models.Post post in await this.ForumsManager.ListPosts(site, forum, null, Models.FlagStates.IsTrue))
				{
					StringBuilder content = new(post.Body);
					
					ContentMetaData forumPostContentItem = new()
					{
						Site = site,
						Title = post.Subject,
						Url = new System.Uri(pageUri, forum.Name.FriendlyEncode() + $"/{post.Id}").ToString(),
						PublishedDate = post.DateAdded,
						SourceId = post.Id,
						Scope = Models.Post.URN,
						Roles = GetViewRoles(forum),
						ContentType = "text/html"
					};

					foreach (Models.Reply reply in await this.ForumsManager.ListPostReplies(site, post, Models.FlagStates.IsTrue))
					{
						content.Append(reply.Body);						
					}

					forumPostContentItem.Content = System.Text.Encoding.UTF8.GetBytes(content.ToString());

					results.Add(forumPostContentItem);
				}

				return results;
			}

			return null;
		}

		private List<Role> GetViewRoles(Models.Forum forum)
		{
			return
				(forum.UseGroupSettings ? forum.Group.Permissions : forum.Permissions)
					.Where(permission => permission.AllowAccess && permission.PermissionType.Scope == ForumsManager.PermissionScopes.FORUM_VIEW)
					.Select(permission => permission.Role).ToList();
		}

	}
}
