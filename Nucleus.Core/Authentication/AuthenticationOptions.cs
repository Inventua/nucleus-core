using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Nucleus.Core.Authentication
{
	public class AuthenticationOptions : AuthenticationSchemeOptions
	{
		public const string Section = "Nucleus:AuthenticationSchemeOptions";

		/// <summary>
		/// Default cookie name used to represent a logged-in user.
		/// </summary>
		/// <remarks>
		/// This value can be overridden by configuration files.
		/// </remarks>
		private const string DEFAULT_COOKIE_NAME = "nucleus-session";

		/// <summary>
		/// Constructor
		/// </summary>
		public AuthenticationOptions()
		{
			this.ClaimsIssuer = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
			
			this.CookieName = DEFAULT_COOKIE_NAME;
			
			this.ExpiryTimeSpan = TimeSpan.FromHours(1);
			this.LongExpiryTimeSpan = TimeSpan.FromDays(30);

			this.SlidingExpirationTimeSpan = TimeSpan.FromHours(1);
			
			//this.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
			//this.SlidingExpiration = true;
			//this.Events = new CookieAuthenticationEvents();
		}

		/// <summary>
		/// Gets the session cookie name.
		/// </summary>
		public string CookieName { get; private set; }
		
		/// <summary>
		/// Gets the expiry interval for new sessions and their associated cookie.
		/// </summary>
		public TimeSpan ExpiryTimeSpan { get; private set; }

		/// <summary>
		/// Gets the expiry interval for new sessions and their associated cookie.
		/// </summary>
		public TimeSpan LongExpiryTimeSpan { get; private set; }

		/// <summary>
		/// Gets the interval to add to a session expiry each time a new request is received.
		/// </summary>
		public TimeSpan SlidingExpirationTimeSpan { get; private set; }

	}
}
