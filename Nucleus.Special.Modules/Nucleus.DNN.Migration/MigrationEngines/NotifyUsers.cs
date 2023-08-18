using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Nucleus.DNN.Migration.Models;
using Nucleus.Extensions;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class NotifyUsers : MigrationEngineBase<Models.NotifyUser>
{
  private IUserManager UserManager { get; }

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IMailTemplateManager MailTemplateManager { get; }
  private IPageManager PageManager { get; }

  private Abstractions.Models.Mail.MailTemplate MailTemplate { get; set; }
  private Nucleus.Abstractions.Mail.IMailClientFactory MailClientFactory { get; }

  public NotifyUsers(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IUserManager userManager, IMailTemplateManager mailTemplateManager, Abstractions.Mail.IMailClientFactory mailClientFactory, IPageManager pageManager) : base("Generating User Notification Emails")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.UserManager = userManager;
    this.MailTemplateManager = mailTemplateManager;
    this.MailClientFactory = mailClientFactory;
    this.PageManager = pageManager;
  }

  public void SetTemplate(Nucleus.Abstractions.Models.Mail.MailTemplate template)
  {
    this.MailTemplate = template;
  }

  override public void UpdateSelections(List<Models.NotifyUser> items)
  {
    foreach (Models.NotifyUser item in items)
    {
      Models.NotifyUser existing = this.Items.Where(existing => existing.User.Id == item.User.Id).FirstOrDefault();
      if (existing != null)
      {
        existing.IsSelected = item.IsSelected && item.CanSelect;
      }
    }
  }

  public override async Task Migrate(Boolean updateExisting)
  {
    foreach (NotifyUser user in this.Items)
    {
      if (user.CanSelect && user.IsSelected)
      {
        try
        {
          // generate a reset token and verification token
          await this.UserManager.SetPasswordResetToken(user.User);
          await this.UserManager.SetVerificationToken(user.User);

          Nucleus.Abstractions.Models.SitePages sitePages = this.Context.Site.GetSitePages();

          // send mail
          Nucleus.Abstractions.Models.Mail.Template.UserMailTemplateData args = new()
          {
            Site = this.Context.Site,
            User = user.User.GetCensored(),
            LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
            PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
            TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
          };

          //Logger.LogTrace("Sending Welcome email {emailTemplateName} to user {userid}.", template.Name, user.Id);

          using (Nucleus.Abstractions.Mail.IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
          {
            await mailClient.Send(this.MailTemplate, args, user.User.Profile.GetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value);
          }
        }
        catch (Exception ex) 
        {
          user.AddError($"User '{user.User.UserName}' was not notified because of an error: {ex.Message}.");
        }

        this.Progress();
      }
      else
      {
        user.AddWarning($"User '{user.User.UserName}' was not selected for import.");
      }
    }

    this.SignalCompleted();
  }

  public override Task Validate()
  {
    foreach (NotifyUser user in this.Items)
    {
      // we need to check roles directly here instead of using .IsSiteAdmin because user.IsSiteAdmin checks the verified flag, and we expect
      // users in the "notify" list to be un-verified
      if (this.Context.Site.AdministratorsRole != null && user.User.Roles.Where(role => role.Id == this.Context.Site.AdministratorsRole.Id).Any())
      {
        user.AddError("Administrator users are not notified.");
      }
      if (!user.User.Approved)
      {
        user.AddError("Un-approved users are not notified.");
      }      
      if (String.IsNullOrEmpty(user.User.Profile.GetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value))
      {
        user.AddError("User has a blank email address and cannot be notified.");
      }
    }

    return Task.CompletedTask;
  }
}
