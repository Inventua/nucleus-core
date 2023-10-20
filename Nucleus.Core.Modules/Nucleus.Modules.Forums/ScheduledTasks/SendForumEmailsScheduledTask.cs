using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Modules.Forums.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Forums.ScheduledTasks
{
	[System.ComponentModel.DisplayName("Nucleus Forums: Send Forum Emails")]
	public class SendForumEmailsScheduledTask : IScheduledTask
	{
		private ForumsManager ForumsManager { get; }
		private IMailClientFactory MailClientFactory { get; }

		private ISiteManager SiteManager { get; }

		private IPageManager PageManager { get; }

		private IPageModuleManager PageModuleManager { get; }
		private IUserManager UserManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private ILogger<SendForumEmailsScheduledTask> Logger { get; }

		public SendForumEmailsScheduledTask(ForumsManager forumsManager, IUserManager userManager, ISiteManager siteManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IMailClientFactory mailClientFactory, IMailTemplateManager mailTemplateManager, ILogger<SendForumEmailsScheduledTask> logger)
		{
			this.ForumsManager = forumsManager;
			this.MailClientFactory = mailClientFactory;
			this.SiteManager = siteManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.UserManager = userManager;
			this.MailTemplateManager = mailTemplateManager;
			this.Logger = logger;
		}

		public async Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			await SendMessages(progress, cancellationToken);
			await TruncateMailQueue(progress, cancellationToken);

			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
		}

		private async Task SendMessages(IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			Site site = null;
			Page page = null;
			PageModule module = null;

			this.Logger.LogInformation("Sending forum messages.");

			var queue = (await this.ForumsManager.ListMailQueue())
				.GroupBy(item => new { item.UserId, ForumId = item.Post.ForumId, item.MailTemplateId })
				.Select(group => new { Key = group.Key, GroupedItems = group, MailQueueItem = group.Select(gr => gr).FirstOrDefault() });

			foreach (var group in queue)
			{
				Models.MailTemplate.Model model = new Models.MailTemplate.Model();

				User user = await this.UserManager.Get(group.Key.UserId);
				UserProfileValue emailAddress = user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault();
				MailTemplate template = await MailTemplateManager.Get(group.Key.MailTemplateId);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				if (template != null && !String.IsNullOrEmpty(emailAddress?.Value))
				{ 
          foreach (var moduleGroup in group.GroupedItems.GroupBy(item => item.ModuleId))
					{
						Forum forum = await this.ForumsManager.Get(group.Key.ForumId);

						// only re-populate the module, page, site variables if this item's module id is different to the previous one (for performance).
						if (module == null || module.Id != moduleGroup.Key)
						{
							module = await this.PageModuleManager.Get(moduleGroup.Key);
							page = await this.PageManager.Get(module.PageId);
							site = await this.SiteManager.Get(page.SiteId);
						}

						List<Post> posts = moduleGroup.Where(queueItem => queueItem.Reply == null).Select(queueItem => queueItem.Post).ToList();
						List<Reply> replies = moduleGroup.Where(queueItem => queueItem.Reply != null).Select(queueItem => queueItem.Reply).ToList();

						model.Forums.Add(new Models.MailTemplate.Forum(forum, posts, replies));
					}

					model.Site = site.Copy<Site>();
					model.Page = Models.MailTemplate.Page.Create(page, page.DefaultPageRoute().Path + $"/{Controllers.ForumsController.MANAGE_SUBSCRIPTIONS_PATH}");
					model.User = user.GetCensored();

					Logger.LogTrace("Sending forum email template {name} to user {userid}.", template.Name, user.Id);

					using (IMailClient mailClient = this.MailClientFactory.Create(site))
					{
						try
						{
							await mailClient.Send<Models.MailTemplate.Model>(template, model, emailAddress.Value);

              var postIds = model.Forums.SelectMany(forum => forum.Posts.Select(post => post.Id)).ToList();
              var replyIds = model.Forums.SelectMany(forum => forum.Replies.Select(reply => reply.Id)).ToList();

              // mark handled queue items as sent
              foreach (MailQueue item in group.GroupedItems)
							{
								item.Status = MailQueue.MailQueueStatus.Sent;
								await this.ForumsManager.SetMailQueueStatus(item);
							}
            }
						catch (Exception ex)
						{
							// don't fail the entire task if an email can't be parsed/sent
							this.Logger?.LogError(ex, "Error sending template {template}", template.Name);
						}
					}
				}
			}
		}

		private async Task TruncateMailQueue(IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			try
			{
				this.Logger?.LogInformation("Truncating mail queue.");
				await this.ForumsManager.TruncateMailQueue(TimeSpan.FromDays(30));
			}
			catch (Exception ex)
			{
				this.Logger?.LogError(ex, "Error truncating mail queue.");
			}
		}
	}
}
