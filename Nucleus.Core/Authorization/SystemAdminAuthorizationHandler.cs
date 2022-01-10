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

namespace Nucleus.Core.Authorization
{
	public class SystemAdminAuthorizationHandler : AuthorizationHandler<SystemAdminAuthorizationRequirement>
	{
		

		private Nucleus.Abstractions.Models.Context CurrentContext { get; }
		private ILogger<SystemAdminAuthorizationHandler> Logger { get; }

		public SystemAdminAuthorizationHandler(Nucleus.Abstractions.Models.Context context, ILogger<SystemAdminAuthorizationHandler> logger)
		{
			this.CurrentContext = context;
			this.Logger = logger;
		}

		/// <summary>
		/// Makes a decision if the user is allowed to administer the requested <see cref="Site"/>.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="requirement"></param>
		/// <returns></returns>
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SystemAdminAuthorizationRequirement requirement)
		{			
			if (context.User.IsSystemAdministrator())
			{
				Logger.LogTrace("User {0}: System Administrator access granted.");
				context.Succeed(requirement);
			}

			if (!context.HasSucceeded)
			{
				Logger.LogTrace("User {0}: System Administrator access denied.");
				context.Fail();
			}
			
			return Task.CompletedTask;
		}


	}
}
