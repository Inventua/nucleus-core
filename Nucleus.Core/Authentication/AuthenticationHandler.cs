using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Routing;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Authentication
{
	public class AuthenticationHandler : Microsoft.AspNetCore.Authentication.SignInAuthenticationHandler<AuthenticationOptions>
	{
		
		private ISessionManager SessionManager { get; }
		private IUserManager UserManager { get; }
		private ISiteManager SiteManager { get; }
		private Context CurrentContext { get; set; }
		private LinkGenerator LinkGenerator { get; }

		public AuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, LinkGenerator linkGenerator, ISiteManager siteManager,
																 Context context,
																 IOptionsMonitor<AuthenticationOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder,
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
		/// Retrieve the Session ID from the session cookie, update the cookie and user session expiry date if sliding expiration is enabled,
		/// set user claims for use by the request of request handling.
		/// </remarks>
		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			List<Claim> claims = new();
			ClaimsIdentity identity;
			ClaimsPrincipal principal;
			UserSession userSession = null;

			string sessionId = this.Context.Request.Cookies[this.Options.CookieName];//[COOKIE_NAME];

			if (!String.IsNullOrEmpty(sessionId))
			{
				Logger.LogTrace("Reading session {sessionId}.", sessionId);
				userSession = await this.SessionManager.Get(Guid.Parse(sessionId));

				if (userSession != null)
				{
					// user session exists, update sliding expiration in the database, and update the cookie expiry date
					if (!userSession.RemoteIpAddress.Equals (this.Context.Connection.RemoteIpAddress))
					{
						Logger.LogCritical("User {UserId} attempted to use a session {SessionId} from {CurrentRemoteIpAddress} when the original session was from {OriginalRemoteIpAddress}!", userSession.UserId, userSession.Id, this.Context.Connection.RemoteIpAddress, userSession.RemoteIpAddress);
						await this.SessionManager.Delete(userSession);
						_ = this.SessionManager.SignOut(this.Context);
						return AuthenticateResult.Fail("Access Denied");
					}

					if (userSession.ExpiryDate > DateTime.UtcNow)
					{
						if (userSession.SlidingExpiry)
						{
							Logger.LogInformation("Session {sessionId} is valid, updating sliding expiration.", sessionId);

							userSession.ExpiryDate = DateTime.UtcNow.Add(this.Options.SlidingExpirationTimeSpan);
							await this.SessionManager.Save(userSession);
						
							AppendCookie(userSession.Id.ToString(), new AuthenticationProperties()
							{
								AllowRefresh = userSession.SlidingExpiry,
								ExpiresUtc = userSession.ExpiryDate,
								IsPersistent = userSession.IsPersistent,
								IssuedUtc = userSession.IssuedDate								
							});
						}
					}
					else
					{
						// Session has expired
						Logger.LogInformation("Session has expired.");
						await this .SessionManager.Delete(userSession);
						_ = this.SessionManager.SignOut(this.Context);
						return AuthenticateResult.Fail("Session Expired");
					}
				}
				else
				{
					Logger.LogWarning("Invalid session Id {sessionId} sent from {remoteIpAddress}.", sessionId, this.Context.Connection.RemoteIpAddress);
				}
			}
			else
			{
				Logger.LogInformation("Session cookie blank or not present.");
			}

			if (userSession != null)
			{
				// User found, set context User Identity
				User user;
								
				user = await this.UserManager.Get(await this.SiteManager.Get(userSession.SiteId), userSession.UserId);

				if (user == null)
				{
					Logger.LogWarning("Session Id {sessionId} for user Id {userId} was not found in the database.", sessionId, userSession.UserId);
					return AuthenticateResult.Fail("Access Denied");
				}
				else
				{
					Logger.LogTrace("User Id {userId} was found for session Id {sessionId}: Adding Claims.", userSession.UserId, sessionId);

					Logger.LogTrace("User Id {userId} Adding Claim {claimType} {userName}.", userSession.UserId, ClaimTypes.Name, user.UserName);
					claims.Add(new Claim(ClaimTypes.Name, user.UserName));
					Logger.LogTrace("User Id {userId} Adding Claim {claimType} {userId}.", userSession.UserId, ClaimTypes.NameIdentifier, user.Id.ToString());
					claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

					if (user.IsSystemAdministrator)
					{
						Logger.LogTrace("User Id {userId} Adding Claim {claimType}.", userSession.UserId, Nucleus.Abstractions.Authentication.Constants.SYSADMIN_CLAIMTYPE);
						claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.SYSADMIN_CLAIMTYPE, ""));
					}

					if (user.Roles != null)
					{
						foreach (Role role in user.Roles)
						{
							Logger.LogTrace("User Id {userId} Adding Claim {claimType} {roleName}.", userSession.UserId, ClaimTypes.Role, role.Name);
							claims.Add(new Claim(ClaimTypes.Role, role.Name));
						}
					}
				
					foreach (UserProfileValue profileValue in user.Profile)
					{
						if (!String.IsNullOrWhiteSpace(profileValue.Value) && !String.IsNullOrEmpty(profileValue?.UserProfileProperty.TypeUri) )
						{
							Logger.LogTrace("User Id {userId} Adding Claim {claimType} {claimValue}.", userSession.UserId, profileValue.UserProfileProperty.TypeUri, profileValue.Value);
							claims.Add(new Claim(profileValue.UserProfileProperty.TypeUri, profileValue.Value));
						}
					}

					identity = new ClaimsIdentity(claims, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME);
				}				
			}				
			else
			{
				Logger.LogTrace("Anonymous user");
				// Anonymous user.  Set a claim for Role=Anonymous Users				
				Logger.LogTrace("Adding Claim {claimType}.", ClaimTypes.Anonymous);
				claims.Add(new Claim(ClaimTypes.Anonymous, ""));

				// This overload creates an identity with IsAuthenticated=false, which is what we want for unauthenticated users
				identity = new ClaimsIdentity(claims);
			}

			principal = new ClaimsPrincipal(identity);

			return AuthenticateResult.Success(new AuthenticationTicket(principal, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME));
		}

		/// <summary>
		/// Handle 401 Challenge responses by redirecting to the login page.
		/// </summary>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
		{
			// special handling for admin menu
			if (this.Context.Request.Path.StartsWithSegments(new PathString("/admin")))
			{
				await this.Context.Response.WriteAsync("Session expired.");
			}
			else
			{
				if (this.CurrentContext.Site != null)
				{
					SitePages sitePage = this.Context.RequestServices.GetService<Context>().Site.GetSitePages();
					PageRoute loginPageRoute = null;
				
					if (sitePage.LoginPageId.HasValue)
					{
						Page loginPage = await this.Context.RequestServices.GetService<IPageManager>().Get(sitePage.LoginPageId.Value);
						if (loginPage != null)
						{
							loginPageRoute = loginPage.DefaultPageRoute();
						}
					}
					if (loginPageRoute == null)
					{
						// Use default login page
						this.RedirectToDefaultLogin();
					}
					else
					{
						string redirectUrl = loginPageRoute.Path + $"?returnUrl={System.Uri.EscapeDataString(this.Context.Request.Path)}";
						Logger.LogTrace("Challenge: Redirecting to site login page {redirectUrl}", redirectUrl);
						this.Context.Response.Redirect(redirectUrl);
					}
				}
				else
				{
					// use default login page
					this.RedirectToDefaultLogin();
				}
			}
			
		}

		/// <summary>
		/// Redirect to the default "built in" login page
		/// </summary>
		private void RedirectToDefaultLogin()
		{
			RouteValueDictionary routeDictionary = new();

			routeDictionary.Add("area", "User");
			routeDictionary.Add("controller", "Account");
			routeDictionary.Add("action", "Index");

			string redirectUrl = this.LinkGenerator.GetPathByRouteValues("Admin", routeDictionary, this.Context.Request.PathBase, FragmentString.Empty, null);
			Logger.LogTrace("Challenge: Redirecting to default login page {redirectUrl}", redirectUrl);
			this.Context.Response.Redirect(redirectUrl);
		}

		/// <summary>
		/// Create and add the session cookie to the response.
		/// </summary>
		/// <param name="sessionId"></param>
		/// <param name="properties"></param>
		private void AppendCookie(string sessionId, AuthenticationProperties properties)
		{
			CookieOptions options = new()
			{
				Expires = properties.ExpiresUtc,
				IsEssential = true,
				HttpOnly = true,
				SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict					  
			};

			this.Context.Response.Cookies.Append(this.Options.CookieName, sessionId, options);
		}

		/// <summary>
		/// Handle sign in by creating/adding the session cookie to the response.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
		{
			Logger.LogTrace("Logging in {name}.", user.Identity.Name);
			AppendCookie(user.FindFirstValue(Nucleus.Abstractions.Authentication.Constants.SESSION_ID_CLAIMTYPE), properties);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Handle sign out by deleting the session cookie and removing the session from the database.
		/// </summary>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected override async Task HandleSignOutAsync(AuthenticationProperties properties)
		{
			string sessionId = this.Context.Request.Cookies[this.Options.CookieName];//[COOKIE_NAME];

			if (!String.IsNullOrEmpty(sessionId))
			{
				Logger.LogTrace("Logging out: Removing session {sessionId}.", sessionId);

				UserSession userSession = await this.SessionManager.Get(Guid.Parse(sessionId));

				if (userSession != null)
				{
					await this.SessionManager.Delete(userSession);
				}
			}

			this.Context.Response.Cookies.Delete(this.Options.CookieName);
			
		}
	}
}
