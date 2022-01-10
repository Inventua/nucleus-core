using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Nucleus.Core.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http; 

namespace Nucleus.Core
{
	/// <summary>
	/// Class used to access/manage user sessions.
	/// </summary>
	/// <remarks>
	/// User sessions are not cached.  This allows us to immediately log a user out, if required.
	/// </remarks>
	public class SessionManager
	{
		private DataProviderFactory DataProviderFactory { get; }
		private UserManager UserManager { get; }
		private IOptions<Authentication.AuthenticationOptions> Options { get; }

		public SessionManager(DataProviderFactory dataProviderFactory, UserManager userManager, IOptions<Authentication.AuthenticationOptions> options)
		{
			this.DataProviderFactory = dataProviderFactory;
			this.UserManager = userManager;
			this.Options = options;
		}

		/// <summary>
		/// Create a new <see cref="UserSession"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="UserSession"/> to the database.  Call <see cref="Save(Site, UserSession)"/> to save the role group.
		/// </remarks>
		public UserSession CreateNew(Site site, User user, Boolean rememberMe, System.Net.IPAddress remoteIpAddress)
		{
			return new UserSession(site, user, rememberMe, remoteIpAddress, DateTime.UtcNow.Add(this.Options.Value.ExpireTimeSpan), !this.Options.Value.ExpireTimeSpan.Equals(TimeSpan.Zero));
		}

		/// <summary>
		/// Retrieve an existing <see cref="UserSession"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public UserSession Get(Guid id)
		{
			using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
			{
				return provider.GetUserSession(id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="UserSession"/> from the database.
		/// </summary>
		/// <param name="userSession"></param>
		public void Delete(UserSession userSession)
		{
			using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
			{
				provider.DeleteUserSession(userSession);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="UserSession"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="userSession"></param>
		public void Save(UserSession userSession)
		{
			using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
			{
				provider.SaveUserSession(userSession);
			}
		}

		public async Task SignIn(UserSession userSession, HttpContext httpContext, string returnUrl)
		{	
			Save(userSession);

			Site site;
			User user;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				site = provider.GetSite(userSession.SiteId);
			}

			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				user = provider.GetUser(userSession.UserId);
			}

			user.Secrets.LastLoginDate = DateTime.UtcNow;
			this.UserManager.Save(site, user);

			List<Claim> claims = new();

			claims.Add(new Claim(ClaimTypes.Name, user.UserName));
			claims.Add(new Claim(AuthenticationHandler.SESSION_ID_CLAIMTYPE, userSession.Id.ToString()));

			ClaimsIdentity claimsIdentity = new(claims, AuthenticationHandler.DEFAULT_AUTH_SCHEME);

			AuthenticationProperties authProperties = new()
			{
				AllowRefresh = true,
				IsPersistent = userSession.IsPersistent,
				IssuedUtc = DateTime.UtcNow,
				RedirectUri = returnUrl
			};

			await httpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity), authProperties);

		}

		public async Task SignOut(HttpContext httpContext)
		{
			// The AuthenticationHandler manages deleting the session
			await httpContext.SignOutAsync();
		}
	}
}
