using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Nucleus.OAuth
{
	public class RemoteAuthenticationOptions : AuthenticationSchemeOptions
	{
		public const string Section = "Nucleus:RemoteAuthenticationSchemeOptions";

		/// <summary>
		/// Constructor
		/// </summary>
		public RemoteAuthenticationOptions()
		{
			this.ClaimsIssuer = RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME;			
			this.ForwardAuthenticate = RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME;
			this.ForwardForbid = RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME;
			this.AccessDeniedPath = "/extensions/oauth/accessdenied";
		}

		public string AccessDeniedPath { get;	}
	}
}
