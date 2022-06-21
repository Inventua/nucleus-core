using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Authorization
{
	public class SiteWizardAuthorizationHandler : AuthorizationHandler<SiteWizardAuthorizationRequirement>
	{		
		
		private ISiteManager SiteManager { get; }
		private IUserManager UserManager { get; }
		private Abstractions.Models.Configuration.FolderOptions FolderOptions { get; }
		private Abstractions.Models.Application Application { get; }

		private ILogger<SiteWizardAuthorizationHandler> Logger { get; }

		public SiteWizardAuthorizationHandler(Abstractions.Models.Application application, ISiteManager siteManager, IUserManager userManager, IOptions<Abstractions.Models.Configuration.FolderOptions> folderOptions, ILogger<SiteWizardAuthorizationHandler> logger)
		{
			this.Application = application;
			this.SiteManager = siteManager;
			this.UserManager = userManager;
			this.FolderOptions = folderOptions.Value;

			this.Logger = logger;
		}

		/// <summary>
		/// Makes a decision on whether the user is allowed to run the setup wizard.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="requirement"></param>
		/// <returns></returns>
		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SiteWizardAuthorizationRequirement requirement)
		{			
			if (await CanRunWizard(context))
			{
				Logger.LogTrace("User {userid}: Setup wizard access granted.", context.User.GetUserId());
				context.Succeed(requirement);
			}

			if (!context.HasSucceeded)
			{
				Logger.LogTrace("User {userid}: Setup wizard access denied.", context.User.GetUserId());
				context.Fail();
			}			
		}

		private async Task<Boolean> CanRunWizard(AuthorizationHandlerContext context)
		{
			if (context.User.IsSystemAdministrator())
			{
				return true;
			}
								
			if (!this.Application.IsInstalled)
			{
				return true;
			}
				
			if (await this.SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators() == 0)
			{
				return true;
			}

			return false;
		}
	}
}
