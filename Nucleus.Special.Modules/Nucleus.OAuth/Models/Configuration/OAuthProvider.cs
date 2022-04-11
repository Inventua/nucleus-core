using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Models.Configuration
{
	public class OAuthProvider
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string FriendlyName { get; set; }

		public List<MapJsonKey> MapJsonKeys { get; set; } = new();
		public List<string> Scope { get; set; } = new();	

		/// <summary>
		/// Return the display name for the OAuth provider.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Both FriendlyName and Name are optional, but take precedence over Type if specified,.
		/// </remarks>
		public string DisplayName()
		{
			return this.FriendlyName ?? this.Name ?? this.Type;
		}

		/// <summary>
		/// Return the route used to start remote authentication.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The OAuth Authenticate action uses a custom (attribute based) route, so we can't use Url.NucleusAction.  Name is 
		/// optional but takes precedence over Type if specified.
		/// The 'proper' way to start authentication is to call Challenge(), but this would add an extra redirect, and it makes
		/// no difference to just call the Url directly.
		/// </remarks
		public string AuthenticateEndpoint(string returnUrl)
		{
			return $"/extensions/OAuth/Authenticate/{this.Name ?? this.Type}?returnUrl={returnUrl ?? "/"}";
		}
	}
}
