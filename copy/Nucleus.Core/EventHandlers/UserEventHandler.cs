using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Core;
using Nucleus.Core.EventHandlers.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
		private MailClientFactory MailClientFactory { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private ILogger<UserEventHandler> Logger { get; }

		public UserEventHandler(Context context, MailClientFactory mailClientFactory, MailTemplateManager mailTemplateManager, ILogger<UserEventHandler> logger)
		{
			this.Context = context;
			this.MailClientFactory = mailClientFactory;
			this.MailTemplateManager = mailTemplateManager;
			this.Logger = logger;
		}

		public void Invoke(User user)
		{
			// send welcome email (if set and the new user has an email address)
			
			if (this.Context != null && this.Context.Site != null)
			{
				UserProfileValue mailTo = user.Profile.Where(value => value.UserProfileProperty?.TypeUri == ClaimTypes.Email).FirstOrDefault();

				SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

				if (templateSelections.WelcomeNewUserTemplateId.HasValue)
				{
					MailTemplate template = this.MailTemplateManager.Get(templateSelections.WelcomeNewUserTemplateId.Value);
					if (template != null && mailTo.Value != null)
					{
						MailArgs args = new()
						{
							{ "Site", this.Context.Site },
							{ "User", user }
						};

						Logger.LogTrace("Sending Welcome email {0} to user {1}.", template.Name, user.Id);

						using (MailClient mailClient = this.MailClientFactory.Create())
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
		}
	}
}

