using DocumentFormat.OpenXml.Office2016.Excel;
using ITfoxtec.Identity.Saml2;
using Nucleus.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Client.Models.Configuration
{
	public class SAMLProvider
	{
		public string Name { get; set; }
		public string FriendlyName { get; set; }

		public List<MapClaim> MapClaims { get; set; } = new();
		
		public string SigningCertificateFile { get; set; }
		public string SigningCertificateThumbprint { get; set; }
		public string SigningCertificatePassword { get; set; }
		public string IdPMetadataUrl { get; set; }

		public string Issuer { get; set; }
		public string AllowedIssuer { get; set; }
		public string SignatureValidationCertificateFile { get; set; }
		public string SignatureValidationCertificatePassword { get; set; }
		
		public string SignatureAlgorithm { get; set; }
		public Boolean SignAuthnRequest { get; set; }
		public string SingleSignOnDestination { get; set; }
		public string SingleLogoutDestination { get; set; }
		public string ArtifactResolutionServiceUrl { get; set; }

		public string ResponseProtocolBinding { get; set; } = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";
		public string RequestProtocolBinding { get; set; } = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";

		public enum ProtocolBindingTypes
		{
			HttpRedirect,
			HttpArtifact,
			HttpPost,
			Soap,
			ReverseSoap,
			Uri,
			Unhandled
		}

		public ProtocolBindingTypes GetRequestProtocolBinding()
		{
			return GetProtocolBinding(this.RequestProtocolBinding);
		}
		public ProtocolBindingTypes GetResponseProtocolBinding()
		{
			return GetProtocolBinding(this.ResponseProtocolBinding);
		}

		public ProtocolBindingTypes GetProtocolBinding(string value)
		{
			switch (value.ToLower())
			{
				case "urn:oasis:names:tc:saml:2.0:bindings:http-redirect":
					return ProtocolBindingTypes.HttpRedirect;
				
				case "urn:oasis:names:tc:saml:2.0:bindings:http-artifact":
					return ProtocolBindingTypes.HttpArtifact;

				case "urn:oasis:names:tc:saml:2.0:bindings:http-post":
					return ProtocolBindingTypes.HttpPost;

				case "urn:oasis:names:tc:saml:2.0:bindings:soap":
					return ProtocolBindingTypes.Soap;

				case "urn:oasis:names:tc:saml:2.0:bindings:paos":
					return ProtocolBindingTypes.ReverseSoap;

				case "urn:oasis:names:tc:saml:2.0:bindings:uri":
					return ProtocolBindingTypes.Uri;

				default:
					return ProtocolBindingTypes.Unhandled;
			}
		}

		/// <summary>
		/// Return the display name for the SAML provider.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Both FriendlyName and Name are optional, but take precedence over Type if specified,.
		/// </remarks>
		public string DisplayName()
		{
			return this.FriendlyName ?? this.Name;
		}

		/// <summary>
		/// Return the route used to start remote authentication.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The SAML Authenticate action uses a custom (attribute based) route, so we can't use Url.NucleusAction.  
		/// The 'proper' way to start authentication is to call Challenge(), but this would add an extra redirect, and it makes
		/// no difference to just call the Url directly.
		/// </remarks
		public string AuthenticateEndpoint(Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string returnUrl)
		{
			string path = $"{Routes.AUTHENTICATE}/{System.Uri.EscapeDataString(SafeProviderName())}";

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
			return this.Name.Replace("/", "-");
		}

	}
}
