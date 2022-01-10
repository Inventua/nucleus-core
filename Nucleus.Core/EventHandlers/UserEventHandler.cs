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
using System.Threading.Tasks;

namespace Nucleus.Core.EventHandlers
{
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

		public UserEventHandler(Context context, IMailClientFactory mailClientFactory, IMailTemplateManager mailTemplateManager, ILogger<UserEventHandler> logger)
		{
			this.Context = context;
			this.MailClientFactory = mailClientFactory;
			this.MailTemplateManager = mailTemplateManager;
			this.Logger = logger;
		}

		public async Task Invoke(User user)
		{
			// send welcome email (if set and the new user has an email address)

			if (this.Context != null && this.Context.Site != null)
			{
				UserProfileValue mailTo = user.Profile.Where(value => value.UserProfileProperty?.TypeUri == ClaimTypes.Email).FirstOrDefault();

				if (!String.IsNullOrEmpty(mailTo?.Value))
				{
					SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

					if (templateSelections.WelcomeNewUserTemplateId.HasValue)
					{
						MailTemplate template = await this.MailTemplateManager.Get(templateSelections.WelcomeNewUserTemplateId.Value);
						if (template != null)
						{
							MailArgs args = new()
							{
								{ "Site", this.Context.Site },
								{ "User", user }
							};

							Logger.LogTrace("Sending Welcome email {0} to user {1}.", template.Name, user.Id);

							using (IMailClient mailClient = this.MailClientFactory.Create())
							{
								mailClient.Send(template, args, mailTo.Value);
							}
						}
					}
					else
					{
						Logger.LogTrace("Not sending Welcome email to user {0} because no welcome email is configured for site {1}.", user.Id, this.Context.Site.Id);
					}
				}
				else
				{
					Logger.LogTrace("Not sending Welcome email to user {0} because the user does not have an email address set.", user.Id, this.Context.Site.Id);
				}

			}
		}
	}
}

