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

namespace Nucleus.OAuth.Client
{
	public class RemoteAuthenticationHandler : Microsoft.AspNetCore.Authentication.SignInAuthenticationHandler<RemoteAuthenticationOptions>
	{
		public const string REMOTE_AUTH_SCHEME = "urn:nucleus/authentication/remote-authentication";

		private ISessionManager SessionManager { get; }
		private IUserManager UserManager { get; }
		private IRoleManager RoleManager { get; }
		private ISiteManager SiteManager { get; }
		private Context CurrentContext { get; set; }
		private LinkGenerator LinkGenerator { get; }

		public RemoteAuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, IRoleManager roleManager, LinkGenerator linkGenerator, ISiteManager siteManager,
																 Context context,
																 IOptionsMonitor<RemoteAuthenticationOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder,
																 ISystemClock clock) : base(options, logger, encoder, clock)
		{
			this.SessionManager = sessionManager;
			this.UserManager = userManager;
			this.RoleManager = roleManager;
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

			ViewModels.SiteClientSettings settings = new();
			settings.ReadSettings(this.CurrentContext.Site);

			User loginUser = null;
			object email = properties.Parameters.Where(prop => prop.Key == "email").Select(pair => pair.Value).FirstOrDefault();

			Logger?.LogTrace("Signing in remote user '{username}'", user.Identity.Name);

			if (settings.MatchByName && !String.IsNullOrEmpty(user.Identity.Name))
			{
				Logger?.LogTrace("Checking for existing user '{username}' by name.", user.Identity.Name);
				loginUser = await this.UserManager.Get(this.CurrentContext.Site, user.Identity.Name);
				if (loginUser == null)
				{
					Logger?.LogTrace("Existing user '{username}' not found by name.", user.Identity.Name);
				}
			}

			if (loginUser == null && settings.MatchByEmail)
			{
				if (email != null && !String.IsNullOrEmpty(email.ToString()))
				{
					Logger?.LogTrace("Checking for existing user with email address '{email}'.", email);
					loginUser = await this.UserManager.GetByEmail(this.CurrentContext.Site, email.ToString());
					if (loginUser == null)
					{
						Logger?.LogTrace("Existing user with email '{email}' not found by name, or more than one user with that email address is present in the database.", email);
					}
				}
			}

			if (loginUser == null)
			{
				// user does not exist		
				if (!settings.CreateUsers)
				{
					Logger?.LogTrace("A matching user was not found, and the OAUTH server CreateUsers setting is set to false.");
					await base.ForbidAsync(properties);
				}
				else
				{
					Logger?.LogTrace("A matching user was not found, creating a new user '{username}'.", user.Identity.Name);

					// create new user 
					loginUser = await this.UserManager.CreateNew(this.CurrentContext.Site);
					loginUser.UserName = user.Identity.Name;

					// fill in all of the user properties that we can find a value for
					foreach (var prop in this.CurrentContext.Site.UserProfileProperties)
					{
						string userPropertyValue = user.FindFirstValue(prop.TypeUri);
						if (userPropertyValue != null)
						{
							Logger?.LogTrace("Adding profile value {name}:'{value}'.", prop.TypeUri, userPropertyValue);
							UserProfileValue existing = loginUser.Profile.Where(value => value.UserProfileProperty.TypeUri == prop.TypeUri).FirstOrDefault();
							if (existing == null)
							{
								loginUser.Profile.Add(new UserProfileValue() { UserProfileProperty = prop, Value = userPropertyValue });
							}
							else
							{
								existing.Value = userPropertyValue;
							}
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
				Boolean userProfileUpdated = false;
				Boolean userRolesUpdated = false;

				if (settings.SynchronizeProfile)
				{
					Logger?.LogTrace("Synchronizing profile for user '{name}'.", loginUser.UserName);

					foreach (Claim claim in user.Claims)
					{
						UserProfileValue prop = loginUser?.Profile.Where(profileProperty => profileProperty.UserProfileProperty.TypeUri == claim.Type).FirstOrDefault();
						// If a property with the same type as the incoming claim is found, update the property if the new value is different than the old value (with extra
						// checking so that an empty string is equivalent to null for the purpose of checking the value)
						if (prop != null && (String.IsNullOrEmpty(prop.Value) ? "" : prop.Value) != (String.IsNullOrEmpty(claim.Value) ? "" : claim.Value))
						{
							Logger?.LogTrace("Setting profile value '{typeuri}' [{profilePropertyName}] to '{value}'.", prop.UserProfileProperty.TypeUri, prop.UserProfileProperty.Name, claim.Value);
							prop.Value = claim.Value;
							userProfileUpdated = true;
						}
					}
				}

				if (settings.SynchronizeRoles)
				{
					IEnumerable<Claim> roleClaims = user.Claims.Where(claim => claim.Type == ClaimTypes.Role);

					// Only sync roles if the OAUTH provider returned role claims
					if (roleClaims.Any())
					{
						Logger?.LogTrace("Synchronizing roles for user '{name}'.", loginUser.UserName);

						if (settings.AddToRoles)
						{
							// Add user to roles in role claims, if a matching role name is found, and it isn't one of the special site roles.
							foreach (Claim claim in roleClaims)
							{
								Role role = await this.RoleManager.GetByName(this.CurrentContext.Site, claim.Value);
								if (role != null)
								{
									if (!IsSpecialRole(claim.Value))
									{
										if (!loginUser.Roles.Where(existing => existing.Name == role.Name).Any())
										{

											loginUser.Roles.Add(role);
											Logger?.LogTrace("Added user '{name}' to role '{role}'.", loginUser.UserName, claim.Value);
											userRolesUpdated = true;
										}
										else
										{
											Logger?.LogTrace("Did not add a role named '{roleName}' for user '{name}' because the user is already a member of that role.", claim.Value, loginUser.UserName);
										}
									}
									else
									{
										Logger?.LogTrace("Ignored a role named '{roleName}' because the role is a special role.", claim.Value);
									}
								}
								else
								{
									Logger?.LogTrace("Did not add a role named '{roleName}' for user '{name}' because no role with that name exists.", claim.Value, loginUser.UserName);
								}
							}
						}

						if (settings.RemoveFromRoles)
						{
							foreach (Role role in loginUser.Roles.ToArray())
							{
								if (!roleClaims.Where(claim => claim.Value == role.Name).Any())
								{
									if (!IsSpecialRole(role.Name))
									{
										// Role assigned to user is not present in role claims, remove it if it isn't one of the special site roles
										loginUser.Roles.Remove(role);
										Logger?.LogTrace("Removed user '{name}' from role '{role}'.", loginUser.UserName, role.Name);
										userRolesUpdated = true;
									}
									else
									{
										Logger?.LogTrace("Ignored a role named '{roleName}' because the role is a special role.", role.Name);
									}
								}

							}
						}
					}
					else
					{
						Logger?.LogTrace("Nucleus did not synchronize roles for user '{name}' because the OAUTH provider did not return any roles.", loginUser.UserName);
					}

					if (userProfileUpdated || userRolesUpdated)
					{
						// save user if any roles have changed
						await this.UserManager.Save(this.CurrentContext.Site, loginUser);
					}
				}

				UserSession session = await this.SessionManager.CreateNew(this.CurrentContext.Site, loginUser, false, base.Context.Connection.RemoteIpAddress);
				await this.SessionManager.SignIn(session, base.Context, properties.RedirectUri ?? "/");
				string url = properties.RedirectUri ?? "/";
				Logger?.LogTrace("Signin for user '{name}' was successful, redirecting to '{url}'.", loginUser.UserName, url);
				base.Response.Redirect(url);
			}
		}

		private Boolean IsSpecialRole(string name)
		{
			if (name == this.CurrentContext.Site.AdministratorsRole.Name) return true;
			if (name == this.CurrentContext.Site.AllUsersRole.Name) return true;
			if (name == this.CurrentContext.Site.AnonymousUsersRole.Name) return true;
			if (name == this.CurrentContext.Site.RegisteredUsersRole.Name) return true;

			return false;
		}

		protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
		{
			Logger?.LogTrace("Signin for user was forbidden.");

			// This code handles the response when a user logs in using a remote provider, but does not have a Nucleus account, and the 
			// "create new account" option is disabled.
			//base.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
			await RedirectToLogin(System.Net.HttpStatusCode.Forbidden.ToString());

			// Using base.Response.StartAsync here is not ideal, because it causes the Microsoft authentication classes to throw a
			// "System.InvalidOperationException: StatusCode cannot be set because the response has already started" exception,
			// but it's the only way that seems to stop the .net core authentication system from ignoring/overriding what we do
			// here & directing to properties.RedirectUri, regardless of our already having set the status to forbidden
			// or redirected to login.			
			await base.Response.StartAsync();
		}

		protected override Task HandleSignOutAsync(AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}

		private async Task RedirectToLogin(string reason)
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
					this.RedirectToDefaultLogin(reason);
				}
				else
				{
					string redirectUrl = loginPageRoute.Path + $"?reason={reason}";
					Logger?.LogTrace("Challenge: Redirecting to site login page {redirectUrl}", redirectUrl);
					this.Context.Response.Redirect(redirectUrl);
				}
			}
			else
			{
				// use default login page
				this.RedirectToDefaultLogin(reason);
			}
		}

		private void RedirectToDefaultLogin(string reason)
		{
			RouteValueDictionary routeDictionary = new();

			routeDictionary.Add("area", "User");
			routeDictionary.Add("controller", "Account");
			routeDictionary.Add("action", "Index");

			string redirectUrl = this.LinkGenerator.GetPathByRouteValues("Admin", routeDictionary, this.Context.Request.PathBase, FragmentString.Empty, null);
			Logger?.LogTrace("Challenge: Redirecting to default login page {redirectUrl}", redirectUrl);
			this.Context.Response.Redirect(redirectUrl + $"?reason={reason}");
		}
	}
}
