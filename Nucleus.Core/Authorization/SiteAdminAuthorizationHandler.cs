using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Models;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Core.Authorization
{
	public class SiteAdminAuthorizationHandler : AuthorizationHandler<SiteAdminAuthorizationRequirement>
	{
		
		private Nucleus.Abstractions.Models.Context CurrentContext { get; }
		private ILogger<SiteAdminAuthorizationHandler> Logger { get; }

		public SiteAdminAuthorizationHandler(Nucleus.Abstractions.Models.Context context, ILogger<SiteAdminAuthorizationHandler> logger)
		{
			this.CurrentContext = context;
			this.Logger = logger;
		}

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SiteAdminAuthorizationRequirement requirement)
		{
			if (this.CurrentContext.Site != null)
			{
				if (context.User.IsSiteAdmin(this.CurrentContext.Site))
				{
					Logger.LogTrace("User {0}: Site Administrator access granted to site {1}.", context.User, this.CurrentContext.Site.Id);
					context.Succeed(requirement);
				}
				else
				{
					Logger.LogTrace("User {0}: Site Administrator access denied to site {1}.", context.User, this.CurrentContext.Site.Id);
					context.Fail();
				}
			}
			else
			{
				// if we were unable to figure out which site is being run, then the SiteAdminAuthoriationHandler
				// doesn't have an opinion on whether the user can access the function
			}
			return Task.CompletedTask;
		}

		

	}
}
