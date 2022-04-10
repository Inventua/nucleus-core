using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Routing;
using Nucleus.Abstractions.Managers;

namespace Nucleus.OAuth
{
	public class RemoteAuthenticationHandler : Microsoft.AspNetCore.Authentication.SignInAuthenticationHandler<RemoteAuthenticationOptions>
	{
		public const string REMOTE_AUTH_SCHEME = "urn:nucleus/authentication/remote-authentication";

		private ISessionManager SessionManager { get; }
		private IUserManager UserManager { get; }
		private ISiteManager SiteManager { get; }
		private Context CurrentContext { get; set; }
		private LinkGenerator LinkGenerator { get; }

		public RemoteAuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, LinkGenerator linkGenerator, ISiteManager siteManager,
																 Context context,
																 IOptionsMonitor<RemoteAuthenticationOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder,
																 ISystemClock clock) : base(options, logger, encoder, clock)
		{
			this.SessionManager = sessionManager;
			this.UserManager = userManager;
			this.SiteManager = siteManager;
			this.LinkGenerator = linkGenerator;
			this.CurrentContext = context;

		}

		/// <summary>
		/// Handle Authentication
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// No implementation - return NoResult.  This authentication handler only implements HandleSigninAsync.
		/// </remarks>
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		/// <summary>
		/// Handle sign in by creating/adding the session cookie to the response.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
		{
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}
			ViewModels.SiteSettings settings = new();
			settings.ReadSettings(this.CurrentContext.Site);

			User loginUser = null;
			object email = properties.Parameters.Where(prop => prop.Key == "email").Select(pair => pair.Value).FirstOrDefault();
				
			if (settings.MatchByName && !String.IsNullOrEmpty(user.Identity.Name))
			{
				loginUser = await this.UserManager.Get(this.CurrentContext.Site, user.Identity.Name);
			}

			if (loginUser == null && settings.MatchByEmail)
			{
				if (email != null && !String.IsNullOrEmpty(email.ToString()))
				{
					loginUser = await this.UserManager.GetByEmail(this.CurrentContext.Site, email.ToString());
				}
			}

			if (loginUser == null)
			{
				// user does not exist		

				if (!settings.CreateUsers)
				{
					//await base.ForbidAsync(properties);
					throw new UnauthorizedAccessException("User does not exist");
				}
				else
				{
					// create new user 
					loginUser = await this.UserManager.CreateNew(this.CurrentContext.Site);
					loginUser.UserName = user.Identity.Name;
					
					// fill in all of the user properties that we can find a value for
					foreach (var prop in this.CurrentContext.Site.UserProfileProperties)
					{
						string userPropertyValue = user.FindFirstValue(prop.TypeUri);
						if (userPropertyValue != null)
						{
							loginUser.Profile.Add(new UserProfileValue() { UserProfileProperty = prop, Value = userPropertyValue });
						}
					}

					this.UserManager.SetNewUserFlags(this.CurrentContext.Site, loginUser);

					if (settings.AutomaticallyVerifyNewUsers)
					{
						loginUser.Verified = true;
					}

					if (settings.AutomaticallyApproveNewUsers)
					{
						loginUser.Approved = true;
					}

					await this.UserManager.Save(this.CurrentContext.Site, loginUser);
				}
			}
			
			if (loginUser != null)
			{
				UserSession session = await this.SessionManager.CreateNew(this.CurrentContext.Site, loginUser, false, base.Context.Connection.RemoteIpAddress);
				await this.SessionManager.SignIn(session, base.Context, "/"); 
			}

		}

		protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
		{
			this.Context.Response.Redirect(BuildRedirectUri(this.Options.AccessDeniedPath));
			return Task.CompletedTask;
		}

		protected override Task HandleSignOutAsync(AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}
	}
}
