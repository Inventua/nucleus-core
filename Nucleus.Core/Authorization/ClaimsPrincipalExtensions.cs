// Moved to Nucleus.Abstractions


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Security.Claims;
//using Nucleus.Abstractions.Models;

//namespace Nucleus.Core.Authorization
//{
//	public static class ClaimsPrincipalExtensions
//	{
//		/// <summary>
//		/// Retrieve the name identifier claim  from the user (claims principal)
//		/// </summary>
//		/// <param name="user"></param>
//		/// <returns></returns>
//		/// <remarks>
//		/// The name identifier is the User Id from the Users table.
//		/// </remarks>
//		public static Guid GetUserId(this System.Security.Claims.ClaimsPrincipal user)
//		{
//			return GetUserClaim<Guid>(user, ClaimTypes.NameIdentifier);
//		}

//		/// <summary>
//		/// Retrieve a true/false value indicating whether the user (claims principal) is a system administrator.
//		/// </summary>
//		/// <param name="user"></param>
//		/// <returns></returns>
//		public static Boolean IsSystemAdministrator(this System.Security.Claims.ClaimsPrincipal user)
//		{
//			return user.HasClaim(claim => claim.Type == Authentication.AuthenticationHandler.SYSADMIN_CLAIMTYPE);			
//		}

//		/// <summary>
//		/// Retrieve a true/false value indicating whether the user (claims principal) is a site administrator or system administrator.
//		/// </summary>
//		/// <param name="user"></param>
//		/// <returns></returns>
//		public static Boolean IsSiteAdmin(this ClaimsPrincipal user, Site site)
//		{
//			return user.IsInRole(site.AdministratorsRole.Name) || user.IsSystemAdministrator();
//		}

//		/// <summary>
//		/// Retrieve the specified claim from the user (claims principal)
//		/// </summary>
//		/// <param name="user"></param>
//		/// <returns></returns>
//		public static T GetUserClaim<T>(this System.Security.Claims.ClaimsPrincipal user, string nameIdentifier)
//		{
//			Claim claim = user.Claims.FirstOrDefault((claim) => claim.Type == nameIdentifier);

//			if (claim != null)
//			{
//				System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
//				if (converter != null)
//				{
//					if (converter.CanConvertFrom(typeof(String)))
//					{
//						return (T)converter.ConvertFrom(claim.Value);
//					}
//				}
//			}

//			return default(T);
//		}
//	}
//}
