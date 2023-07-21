using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.Authorization
{
	/// <summary>
	/// Extensions used to access the current user's claims.
	/// </summary>
	public static class ClaimsPrincipalExtensions
	{
		/// <summary>
		/// Retrieve the name identifier claim  from the user (claims principal)
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		/// <remarks>
		/// The name identifier is the User Id from the Users table.
		/// </remarks>
		public static Guid GetUserId(this System.Security.Claims.ClaimsPrincipal user)
		{
			if (GetUserClaim<string>(user, ClaimTypes.AuthenticationMethod) == Nucleus.Abstractions.Authentication.Constants.AUTHENTICATED_BY_COOKIE)
			{ 
				return GetUserClaim<Guid>(user, ClaimTypes.NameIdentifier);
			}
			else
			{
				return Guid.Empty;
			}
		}

		/// <summary>
		/// Retrieve a true/false value indicating whether the user (claims principal) is a system administrator.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsSystemAdministrator(this System.Security.Claims.ClaimsPrincipal user)
		{
			if (user.IsApproved() && user.IsVerified())
			{
				return user.HasClaim(claim => claim.Type == Nucleus.Abstractions.Authentication.Constants.SYSADMIN_CLAIMTYPE);
			}
			else
			{
				// If the user account isn't approved and verified, the user doesn't have system admin rights.  System admins can be 
				// "un-approved" and/or "un-verified"
				return false;
			}		
		}

		/// <summary>
		/// Retrieve a true/false value indicating whether the user (claims principal) is a site administrator or system administrator.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <returns></returns>
		public static Boolean IsSiteAdmin(this ClaimsPrincipal user, Site site)
		{
			if (user.IsApproved() && user.IsVerified())
			{
				return user.IsSystemAdministrator() || (site.AdministratorsRole != null && user.IsInRole(site.AdministratorsRole.Name));
			}
			else
			{
				// If the user account isn't approved and verified, the user doesn't have site admin rights
				return false;
			}
		}

		/// <summary>
		/// Retrieve a true/false value indicating whether the user (claims principal) account has been approved.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsApproved(this ClaimsPrincipal user)
		{
			return !user.Claims.Where(claim => claim.Type == Nucleus.Abstractions.Authentication.Constants.NOT_APPROVED_CLAIMTYPE).Any();
		}

		/// <summary>
		/// Retrieve a true/false value indicating whether the user (claims principal) account has been verified.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsVerified(this ClaimsPrincipal user)
		{
			return !user.Claims.Where(claim => claim.Type == Nucleus.Abstractions.Authentication.Constants.NOT_VERIFIED_CLAIMTYPE).Any();
		}

    /// <summary>
		/// Retrieve a true/false value indicating whether the user (claims principal) password has expired.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsPasswordExpired(this ClaimsPrincipal user)
    {
      return user.Claims.Where(claim => claim.Type == Nucleus.Abstractions.Authentication.Constants.PASSWORD_EXPIRED_CLAIMTYPE).Any();
    }


    /// <summary>
    /// Retrieve the specified claim from the user (claims principal)
    /// </summary>
    /// <param name="user"></param>
    /// <param name="nameIdentifier"></param>
    /// <typeparam name="T">Type to return.</typeparam>
    /// <returns></returns>
    public static T GetUserClaim<T>(this System.Security.Claims.ClaimsPrincipal user, string nameIdentifier)
		{
			Claim claim = user.Claims.Where(claim => claim.Type == nameIdentifier).FirstOrDefault();

			if (claim != null)
			{
				System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
				if (converter != null)
				{
					if (converter.CanConvertFrom(typeof(String)))
					{
						return (T)converter.ConvertFrom(claim.Value);
					}
				}
			}

			return default;
		}
	}
}
