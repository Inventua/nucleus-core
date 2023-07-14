using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extensions for the User class.
	/// </summary>
	public static class UserExtensions
	{
    /// <summary>
		/// Retrieve a true/false value indicating whether the user is a system administrator.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsSystemAdministrator(this User user)
    {
      if (user.Approved && user.Verified)
      {
        return user.IsSystemAdministrator;
      }
      else
      {
        // If the user account isn't approved and verified, the user doesn't have system admin rights.  System admins can be 
        // "un-approved" and/or "un-verified"
        return false;
      }
    }

    /// <summary>
    /// Retrieve a true/false value indicating whether the user s a site administrator or system administrator.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="site"></param>
    /// <returns></returns>
    public static Boolean IsSiteAdmin(this User user, Site site)
    {
      if (user.Approved && user.Verified)
      {
        return user.IsSystemAdministrator() || (site.AdministratorsRole != null && user.Roles.Where(role=>role.Id == site.AdministratorsRole.Id).Any());
      }
      else
      {
        // If the user account isn't approved and verified, the user doesn't have site admin rights
        return false;
      }
    }

    /// <summary>
    /// Return a copy of the supplied user with sensitive data (role memberships, and most secrets) removed.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static User GetCensored(this User user)
		{
      return new User()
      {
        Id = user.Id,
        Profile = user.Profile,
        UserName = user.UserName,
        Secrets = new()
        {
          PasswordResetToken = user.Secrets?.PasswordResetToken,
          PasswordResetTokenExpiryDate = user.Secrets?.PasswordResetTokenExpiryDate,
          VerificationToken = user.Secrets?.VerificationToken,
          VerificationTokenExpiryDate = user.Secrets?.VerificationTokenExpiryDate
        }
			};
		}
	}
}
