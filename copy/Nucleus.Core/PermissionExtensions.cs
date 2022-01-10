using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;

namespace Nucleus.Core
{
	public static class PermissionExtensions
	{
		/// <summary>
		/// Returns a true/false value indicating whether the user is allowed to access the resource represented by the specified permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean IsValid(this Permission permission, Site site, ClaimsPrincipal user)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			if (permission.AllowAccess)
			{
				if (user.IsInRole(permission.Role.Name))
				{
					return true;
				}

				if (user.HasClaim(claim => claim.Type == ClaimTypes.Anonymous) && permission.Role.Id == site.AnonymousUsersRole.Id)
				{
					return true;
				}

				if (permission.Role.Id == site.AllUsersRole.Id)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Page"/> View permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsPageViewPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.PAGE_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Page"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsPageEditPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.PAGE_EDIT;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Folder"/> View permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsFolderViewPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.FOLDER_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Folder"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsFolderEditPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.FOLDER_EDIT;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="PageModule"/> View permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsModuleViewPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.MODULE_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="PageModule"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsModuleEditPermission(this Permission permission)
		{
			return permission.PermissionType.Scope == PermissionType.PermissionScopes.MODULE_EDIT;
		}

		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context)
		{			
			if (Boolean.TryParse(context.Request.Cookies[Constants.EDIT_COOKIE_NAME], out Boolean isEditMode))
			{
				if (isEditMode)
				{
					// refresh cookie expiry
					Microsoft.AspNetCore.Http.CookieOptions options = new()
					{
						Expires = DateTime.UtcNow.AddMinutes(60),
						IsEssential = true,
						SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
					};

					context.Response.Cookies.Append(Constants.EDIT_COOKIE_NAME, "true", options);
				}

				return isEditMode;
			}			

			return false;
		}

		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context, Site site, Page page, PageModule module)
		{
			if (user.HasEditPermission(site, page, module))
			{
				return IsEditing(user, context);				
			}

			return false;
		}

		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context, Site site, Page page)
		{
			if (user.CanEditContent(site, page))
			{
				return IsEditing(user, context);
			}

			return false;
		}

		public static Boolean CanEditContent(this System.Security.Claims.ClaimsPrincipal user, Site site, Page page)
		{
			if (page == null) return false;

			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			if (user.HasEditPermission(site, page))
			{
				return true;
			}
			else
			{
				foreach (PageModule module in page.Modules)
				{
					if (user.HasEditPermission(site, page, module))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has view rights for the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Page page)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			foreach (Permission permission in page.Permissions)
			{
				if (permission.IsPageViewPermission())
				{
					if (permission.IsValid(site, user))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has edit rights for the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasEditPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Page page)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			foreach (Permission permission in page.Permissions)
			{

				if (permission.IsPageEditPermission())
				{
					if (permission.IsValid(site, user))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has view rights for the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Page page, PageModule moduleInfo)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			if (moduleInfo.InheritPagePermissions)
			{
				foreach (Permission permission in page.Permissions)
				{

					if (permission.IsPageViewPermission())
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}
			else
			{
				foreach (Permission permission in moduleInfo.Permissions)
				{

					if (permission.IsModuleViewPermission())
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has edit rights for the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasEditPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Page page, PageModule moduleInfo)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}
						
			if (moduleInfo.InheritPagePermissions)
			{
				foreach (Permission permission in page.Permissions)
				{

					if (permission.IsPageEditPermission())
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}
			else
			{
				foreach (Permission permission in moduleInfo.Permissions)
				{

					if (permission.IsModuleEditPermission())
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has view rights for the folder.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Folder folder)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			foreach (Permission permission in folder.Permissions)
			{
				if (permission.IsFolderViewPermission())
				{
					if (permission.IsValid(site, user))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has edit rights for the folder.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static Boolean HasEditPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Folder folder)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			foreach (Permission permission in folder.Permissions)
			{

				if (permission.IsFolderEditPermission())
				{
					if (permission.IsValid(site, user))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
