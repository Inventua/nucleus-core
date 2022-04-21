using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Client.Models.Configuration
{
	/// <summary>
	/// Collection type used to retrieve ClaimType settings from the configuration files.
	/// </summary>
	public class OAuthProviders : List<OAuthProvider>
	{

		/// <summary>
		/// Configuration file section path for claim type options.
		/// </summary>
		public const string Section = "Nucleus:OAuthProviders";
	}
}
