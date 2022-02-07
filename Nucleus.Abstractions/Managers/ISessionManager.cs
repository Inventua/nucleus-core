using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	///  Defines the interface for the session manager.
	/// </summary>
	/// <remarks>
	/// User sessions are not cached.  This allows us to immediately log a user out, if required.
	/// </remarks>
	public interface ISessionManager
	{
		/// <summary>
		/// Create a new <see cref="UserSession"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="UserSession"/> to the database.  Call <see cref="Save(UserSession)"/> to save the role group.
		/// </remarks>
		public Task<UserSession> CreateNew(Site site, User user, Boolean rememberMe, System.Net.IPAddress remoteIpAddress);

		/// <summary>
		/// Retrieve an existing <see cref="UserSession"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<UserSession> Get(Guid id);

		/// <summary>
		/// Delete the specified <see cref="UserSession"/> from the database.
		/// </summary>
		/// <param name="userSession"></param>
		public Task Delete(UserSession userSession);

		/// <summary>
		/// Create or update the specified <see cref="UserSession"/>.
		/// </summary>
		/// <param name="userSession"></param>
		public Task Save(UserSession userSession);

		/// <summary>
		/// Sign in the user represented by useSession.
		/// </summary>
		/// <param name="userSession"></param>
		/// <param name="httpContext"></param>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		public Task SignIn(UserSession userSession, HttpContext httpContext, string returnUrl);

		/// <summary>
		/// Sign out the current user.
		/// </summary>
		/// <param name="httpContext"></param>
		/// <returns></returns>
		public Task SignOut(HttpContext httpContext);

		/// <summary>
		/// Remove all expired sessions.
		/// </summary>
		/// <returns></returns>
		public Task DeleteExpiredSessions();

		/// <summary>
		/// Return a count of active user sessions.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// An active user session is defined as a session which has not expired, and has been updated in the last 5 minutes.
		/// </remarks>
		public Task<long> CountUsersOnline(Site site);
	}
}
