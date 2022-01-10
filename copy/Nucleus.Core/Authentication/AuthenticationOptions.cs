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
		/// Constructor
		/// </summary>
		public AuthenticationOptions()
		{
			this.ClaimsIssuer = AuthenticationHandler.DEFAULT_AUTH_SCHEME;
			
			//this.CookieManager = new Microsoft.AspNetCore.Authentication.Cookies.ChunkingCookieManager();
			this.CookieName = AuthenticationHandler.DEFAULT_AUTH_SCHEME;
			
			this.ExpireTimeSpan = TimeSpan.FromHours(1);
			this.SlidingExpirationTimeSpan= TimeSpan.FromHours(1);
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
		public TimeSpan ExpireTimeSpan { get; private set; }

		/// <summary>
		/// Gets the interval to add to a session expiry each time a new request is received.
		/// </summary>
		public TimeSpan SlidingExpirationTimeSpan { get; private set; }

	}
}
