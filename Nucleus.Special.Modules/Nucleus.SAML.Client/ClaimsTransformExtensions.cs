using ITfoxtec.Identity.Saml2.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.SAML.Client
{
	internal class ClaimsTransformExtensions
	{
		public static ClaimsPrincipal Transform(ClaimsPrincipal incomingPrincipal, List<Models.Configuration.MapClaim> claimActions, ILogger logger)
		{
			if (!incomingPrincipal.Identity.IsAuthenticated)
			{
				return incomingPrincipal;
			}

			return CreateClaimsPrincipal(incomingPrincipal, claimActions, logger);
		}

		private static ClaimsPrincipal CreateClaimsPrincipal(ClaimsPrincipal incomingPrincipal, List<Models.Configuration.MapClaim> claimActions, ILogger logger)
		{
			var claims = new List<Claim>();

			// Look for claim actions (set in config/code with MapJsonType) in the JWT payload (token claims).  If found, add claims
			// to the identity with the claim types specified.  Normally claim actions are populated (automatically) by a call to
			// options.UserInformationEndpoint, and .net core doesn't seem to pay any attention to JWT tokens (hence this code is required).
			foreach (Models.Configuration.MapClaim action in claimActions)
			{				
				// The JWT token can contain multiple claims with the same claim type (roles, for example), so we must loop through them.
				IEnumerable<System.Security.Claims.Claim> samlClaims = incomingPrincipal.Claims
					.Where(claim => claim.Type.Equals(action.SAMLKey, StringComparison.OrdinalIgnoreCase));

				if (samlClaims.Any())
				{
					foreach (System.Security.Claims.Claim claim in samlClaims)
					{
						logger?.LogTrace("Adding claim {claimtype}: '{value}' from the SAML {inputClaimType} property.", action.ClaimType, claim.Value, claim.Type);
						claims.Add(new(action.ClaimType, claim.Value, claim.ValueType, claim.Issuer, claim.Issuer));
					}
				}
				else
				{
					logger?.LogTrace("No mapping was found for the JWT token {inputClaimType} property.", action.SAMLKey);
				}
			}

			return new ClaimsPrincipal(new ClaimsIdentity(claims, incomingPrincipal.Identity.AuthenticationType, ClaimTypes.NameIdentifier, ClaimTypes.Role)
			{
				BootstrapContext = ((ClaimsIdentity)incomingPrincipal.Identity).BootstrapContext
			});
		}


		private static IEnumerable<Claim> GetSaml2LogoutClaims(ClaimsPrincipal principal)
		{
			yield return GetClaim(principal, Saml2ClaimTypes.NameId);
			yield return GetClaim(principal, Saml2ClaimTypes.NameIdFormat);
			yield return GetClaim(principal, Saml2ClaimTypes.SessionIndex);
		}

		private static Claim GetClaim(ClaimsPrincipal principal, string claimType)
		{
			return ((ClaimsIdentity)principal.Identity).Claims.Where(c => c.Type == claimType).FirstOrDefault();
		}

		private static string GetClaimValue(ClaimsPrincipal principal, string claimType)
		{
			var claim = GetClaim(principal, claimType);
			return claim != null ? claim.Value : null;
		}
	}
}
