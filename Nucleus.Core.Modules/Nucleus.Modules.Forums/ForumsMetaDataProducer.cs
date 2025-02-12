﻿using System;
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
using Nucleus.Extensions.Logging;
using Nucleus.ViewFeatures;
using Nucleus.Modules.Forums.Models;
using Microsoft.Extensions.Hosting;

namespace Nucleus.Modules.Forums
{
	public class ForumsMetaDataProducer : IContentMetaDataProducer
	{
    private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }

    private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider { get; } = new();

		private IFileSystemManager FileSystemManager { get; }
		private GroupsManager GroupsManager { get; }
		private ForumsManager ForumsManager { get; }
		private IExtensionManager ExtensionManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }

		private ILogger<ForumsMetaDataProducer> Logger { get; }

		public ForumsMetaDataProducer(ISearchIndexHistoryManager searchIndexHistoryManager, ISiteManager siteManager, GroupsManager groupsManager, ForumsManager forumsManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IExtensionManager extensionManager, ILogger<ForumsMetaDataProducer> logger)
		{
      this.SearchIndexHistoryManager = searchIndexHistoryManager;
      this.FileSystemManager = fileSystemManager;
			this.GroupsManager = groupsManager;
			this.ForumsManager = forumsManager;
			this.ExtensionManager = extensionManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.Logger = logger;
		}

		public async override IAsyncEnumerable<ContentMetaData> ListItems(Site site)
		{
			// This must match the value in package.xml
			Guid moduleDefinitionId = Guid.Parse("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

			//List<ContentMetaData> results = new();

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {0} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
				{
					Page page = await this.PageManager.Get(module.PageId);
										
					foreach (Models.Group group in await this.GroupsManager.List(module))
					{
						if (!group.Settings.AllowSearchIndexing)
						{
							Logger?.LogInformation("Skipping forum group {groupName} on page {pageid}/{pagename} because the group's 'Allow search indexing' setting is false.", group.Name, page.Id, page.Name);
						}
						else
						{
							foreach (Models.Forum item in await this.ForumsManager.List(group))
							{
                // get a fully-populated forum object (will generally be in cache)
                Models.Forum forum = await this.ForumsManager.Get(item.Id);

                if (!forum.EffectiveSettings().AllowSearchIndexing)
								{
									Logger?.LogInformation("Skipping forum {forumName} on page {pageid}/{pagename} because the forum's 'Allow search indexing' setting is false.", forum.Name, page.Id, page.Name);
								}
								else
								{                  
                  await foreach (ContentMetaData metaData in BuildContentMetaData(site, page, module, forum))
                  {
                    yield return metaData;
                  }                  
								}
							}
						}
					}
					
				}
			}

			//return results.Where(result => result != null);
		}

		/// <summary>
		/// Return a meta-data entries for each post (including replies in the content body)
		/// </summary>
		/// <param name="site"></param>
		/// <param name="document"></param>
		/// <returns></returns>
		private async IAsyncEnumerable<ContentMetaData> BuildContentMetaData(Site site, Page page, PageModule module, Models.Forum forum)
		{
      //List<ContentMetaData> results = new();

      if (page != null)
      {
        string pageUrl = UrlHelperExtensions.RelativePageLink(page);

        foreach (Models.Post post in await this.ForumsManager.ListPosts(forum, Models.FlagStates.IsTrue))
        {
          IList<Reply> replies = await this.ForumsManager.ListPostReplies(site, post, Models.FlagStates.IsTrue);

          SearchIndexHistory historyItem = await this.SearchIndexHistoryManager.Get(site.Id, Models.Post.URN, post.Id);
          if (historyItem == null || historyItem.LastIndexedDate < PostModifiedDate(post, replies))
          {
            StringBuilder content = new(post.Body);

            ContentMetaData forumPostContentItem = new()
            {
              Site = site,
              Title = post.Subject,
              Url = pageUrl + forum.Name.FriendlyEncode() + $"/{post.Id}",
              PublishedDate = post.DateAdded,
              SourceId = post.Id,
              Scope = Models.Post.URN,
              Type = "Forum Post",
              Roles = GetViewRoles(forum),
              ContentType = "text/html"
            };

            foreach (Models.Reply reply in replies)
            {
              content.Append(reply.Body);
            }

            forumPostContentItem.Content = System.Text.Encoding.UTF8.GetBytes(content.ToString());

            yield return forumPostContentItem;
          }
        }
      }
    }

    private DateTime PostModifiedDate(Models.Post post, IList<Models.Reply> replies)
    {
      DateTime? modifiedDate = post.DateChanged ?? post.DateAdded;
      Models.Reply lastReply = replies.OrderBy(reply => reply.DateChanged ??  reply.DateAdded).FirstOrDefault();

      if (lastReply != null && ((lastReply.DateChanged ?? lastReply.DateAdded) > modifiedDate))
      {
        modifiedDate = lastReply.DateChanged ?? lastReply.DateAdded;
      }

      return modifiedDate ?? DateTime.MaxValue;
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
