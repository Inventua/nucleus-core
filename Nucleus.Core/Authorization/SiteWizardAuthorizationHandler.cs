using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Authorization
{
	public class SiteWizardAuthorizationHandler : AuthorizationHandler<SiteWizardAuthorizationRequirement>
	{		
		
		private ISiteManager SiteManager { get; }
		private IUserManager UserManager { get; }

		private ILogger<SiteWizardAuthorizationHandler> Logger { get; }

		public SiteWizardAuthorizationHandler(ISiteManager siteManager, IUserManager userManager, ILogger<SiteWizardAuthorizationHandler> logger)
		{
			this.SiteManager = siteManager;
			this.UserManager = userManager;
			this.Logger = logger;
		}

		/// <summary>
		/// Makes a decision if the user is allowed to administer the requested <see cref="Site"/>.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="requirement"></param>
		/// <returns></returns>
		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SiteWizardAuthorizationRequirement requirement)
		{			
			if (context.User.IsSystemAdministrator() || (await this .SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators () == 0))
			{
				Logger.LogTrace("User {0}: System Administrator access granted.");
				context.Succeed(requirement);
			}

			if (!context.HasSucceeded)
			{
				Logger.LogTrace("User {0}: System Administrator access denied.");
				context.Fail();
			}			
		}


	}
}
