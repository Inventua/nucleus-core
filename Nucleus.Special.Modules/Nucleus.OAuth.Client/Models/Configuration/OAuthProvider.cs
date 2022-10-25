using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Client.Models.Configuration
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
		public string AuthenticateEndpoint(Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string returnUrl)
		{
			string path = $"~/extensions/OAuthClient/Authenticate/{System.Uri.EscapeDataString(SafeProviderName())}";

			return $"{urlHelper.Content(path)}?returnUrl={returnUrl ?? urlHelper.Content("~/")}";
		}

		/// <summary>
		/// Replace characters in the provider name so that it is safe for use in an Url
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		///  MVC does not url-decode "/" (%2F) in urls - https://github.com/dotnet/aspnetcore/issues/11544.  This issue was first
		/// reported in .NET 3.1 and has been put on the backlog to be addressed in .NET 6,7 and has now been moved to .NET 8.
		/// So we replace "/" with in urls.  This means that any comparison to the provider name must use this function.
		///	</remarks>
		public string SafeProviderName()
		{
			return (this.Name ?? this.Type).Replace("/", "-");
		}

	}
}
