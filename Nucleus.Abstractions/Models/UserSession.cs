using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represent the session for a logged-in user.
	/// </summary>
	public class UserSession
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// User Id of the user who owns the session.
		/// </summary>
		public Guid UserId { get; set; }

		/// <summary>
		/// Site Id of the site that the user is logged into.
		/// </summary>
		public Guid SiteId { get; set; }

		/// <summary>
		/// Gets or sets whether to update the session expiry date when a new request is processed.
		/// </summary>
		public Boolean SlidingExpiry { get; set; }

		/// <summary>
		/// Gets or sets whether the browser cookie representing the session should persist between browser sessions.
		/// </summary>
		public Boolean IsPersistent { get; set; }

		/// <summary>
		/// Session expiry date/time.
		/// </summary>
		public DateTime ExpiryDate { get; set; }

		/// <summary>
		/// Date/Time that the session was created.
		/// </summary>
		public DateTime IssuedDate { get; set; }

		/// <summary>
		/// Address of the user-agent used to log in.  
		/// </summary>
		/// <remarks>
		/// The remote IP address must match for all subsequent uses of the session.
		/// </remarks>
		public System.Net.IPAddress RemoteIpAddress { get; set; }

		/// <summary>
		/// Initialize a new instance of UserSession.
		/// </summary>
		/// <remarks>
		/// This constructor is used when reading an existing session.
		/// </remarks>
		public UserSession()
		{

		}

		/// <summary>
		/// Initialize a new UserSession for a specified site, user, and IP address.
		/// </summary>
		/// <remarks>
		/// This constructor is used when a user logs in.
		/// </remarks>
		public UserSession(Site site, User user, Boolean rememberMe, System.Net.IPAddress remoteIpAddress, DateTime expiry, Boolean slidingExpiry)
		{
			this.Id = Guid.NewGuid();
			this.IssuedDate = DateTime.UtcNow;
			this.SlidingExpiry = slidingExpiry;
			this.IsPersistent = rememberMe;
			this.UserId = user.Id;
			this.SiteId = site.Id;
			this.ExpiryDate = expiry;
			this.RemoteIpAddress = remoteIpAddress;
		}
	}
}
