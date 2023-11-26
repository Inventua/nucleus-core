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

namespace Nucleus.Modules.Forums.ScheduledTasks;

[System.ComponentModel.DisplayName("Nucleus Forums: Send Forum Emails (immediate)")]
public class SendImmediateForumEmailsScheduledTask : IScheduledTask
{
  private ForumsManager ForumsManager { get; }
  private IMailClientFactory MailClientFactory { get; }

  private ISiteManager SiteManager { get; }

  private IPageManager PageManager { get; }

  private IPageModuleManager PageModuleManager { get; }
  private IUserManager UserManager { get; }
  private IMailTemplateManager MailTemplateManager { get; }
  private ILogger<SendImmediateForumEmailsScheduledTask> Logger { get; }

  public SendImmediateForumEmailsScheduledTask(ForumsManager forumsManager, IUserManager userManager, ISiteManager siteManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IMailClientFactory mailClientFactory, IMailTemplateManager mailTemplateManager, ILogger<SendImmediateForumEmailsScheduledTask> logger)
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
    long sentMessageCount = 0;

    this.Logger.LogInformation("Sending forum messages (immediate).");

    var queue = await this.ForumsManager.ListMailQueue(NotificationFrequency.Single);

    foreach (var item in queue.OrderBy(item => item.ModuleId))
    {
      try
      {

        Models.MailTemplate.Models.Immediate model = new();

        User user = await this.UserManager.Get(item.UserId);
        UserProfileValue emailAddress = user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault();
        MailTemplate template = await MailTemplateManager.Get(item.MailTemplateId);

        if (cancellationToken.IsCancellationRequested)
        {
          return;
        }

        if (template != null && !String.IsNullOrEmpty(emailAddress?.Value))
        {
          Forum forum = await this.ForumsManager.Get(item.Post.ForumId);

          // only re-populate the module, page, site variables if this item's module id is different to the previous one (for performance).
          if (module == null || module.Id != item.ModuleId)
          {
            module = await this.PageModuleManager.Get(item.ModuleId);
            page = await this.PageManager.Get(module.PageId);
            site = await this.SiteManager.Get(page.SiteId);
          }

          model.Forum = new Models.MailTemplate.Forum(forum);
          model.Post = item.Post;
          model.Reply = item.Reply;
        }

        model.Site = site.Copy<Site>();
        model.Page = Models.MailTemplate.Page.Create(page, page.DefaultPageRoute().Path + $"/{Controllers.ForumsController.MANAGE_SUBSCRIPTIONS_PATH}");
        model.User = user.GetCensored();
        model.Summary = model.Forum.Name;

        Logger.LogTrace("Sending forum email template {name} to user {userid}.", template.Name, user.Id);

        using (IMailClient mailClient = this.MailClientFactory.Create(site))
        {
          try
          {
            await mailClient.Send<Models.MailTemplate.Models.Immediate>(template, model, emailAddress.Value);
            sentMessageCount++;

            // mark handled queue items as sent            
            item.Status = MailQueue.MailQueueStatus.Sent;
            await this.ForumsManager.SetMailQueueStatus(item);
          }
          catch (Exception ex)
          {
            // don't fail the entire task if an email can't be parsed/sent
            this.Logger?.LogError(ex, "Error sending template {template}", template.Name);
          }
        }
      }
      catch (Exception ex)
      {
        this.Logger.LogError(ex, "Sending forum email (immediate).");
      }
    }

    if (sentMessageCount > 0)
    {
      this.Logger?.LogInformation("Sent {count} messages.", sentMessageCount);
    }
    else
    {
      this.Logger?.LogInformation("There were no messages to send.");
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

