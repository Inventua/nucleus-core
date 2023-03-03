using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Extensions;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using System.Security.Claims;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Mail.Template;

namespace Nucleus.Core.EventHandlers;

/// <summary>
/// Perform operations after a user is created.
/// </summary>
/// <param name="user"></param>
/// <remarks>
/// This class sends a Welcome email to the new user.
/// </remarks>
public class UserEventHandler : ISystemEventHandler<User, Create>
{
	private Context Context { get; }
	private IMailClientFactory MailClientFactory { get; }
	private IMailTemplateManager MailTemplateManager { get; }
	private ILogger<UserEventHandler> Logger { get; }
	private IUserManager UserManager { get; }
	private IPageManager PageManager { get; }

	public UserEventHandler(Context context, IMailClientFactory mailClientFactory, IUserManager userManager, IPageManager pageManager, IMailTemplateManager mailTemplateManager, ILogger<UserEventHandler> logger)
	{
		this.Context = context;
		this.MailClientFactory = mailClientFactory;
		this.MailTemplateManager = mailTemplateManager;
		this.UserManager = userManager;
		this.PageManager = pageManager;
		this.Logger = logger;
	}

	public async Task Invoke(User user)
	{
		// send welcome email (if set and the new user has an email address)

		if (this.Context != null && this.Context.Site != null)
		{
			// the user may not be fully populated, so we read it again
			user = await this.UserManager.Get(this.Context.Site, user.Id);

			UserProfileValue mailTo = user.Profile.Where(value => value.UserProfileProperty?.TypeUri == ClaimTypes.Email).FirstOrDefault();

			if (!String.IsNullOrEmpty(mailTo?.Value))
			{
				SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

				if (templateSelections.WelcomeNewUserTemplateId.HasValue)
				{
					MailTemplate template = await this.MailTemplateManager.Get(templateSelections.WelcomeNewUserTemplateId.Value);
					SitePages sitePages = this.Context.Site.GetSitePages();
					
					if (template != null)
					{
						UserMailTemplateData args = new()
						{
							Site = this.Context.Site,
							User = user.GetCensored(),
							LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
							PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
							TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
						};

						Logger.LogTrace("Sending Welcome email {emailTemplateName} to user {userid}.", template.Name, user.Id);

						using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
						{
							await mailClient.Send(template, args, mailTo.Value);
						}
					}
				}
				else
				{
					Logger.LogTrace("Not sending Welcome email to user {userid} because no welcome email is configured for site {siteId}.", user.Id, this.Context.Site.Id);
				}
			}
			else
			{
				Logger.LogTrace("Not sending Welcome email to user {userid} because the user does not have an email address set.", user.Id);
			}

		}
	}
}

