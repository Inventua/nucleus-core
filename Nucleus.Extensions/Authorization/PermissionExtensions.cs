using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System.Security.Claims;
using Nucleus.Abstractions;

namespace Nucleus.Extensions.Authorization
{
	/// <summary>
	/// Extensions used to check the current user's permissions.
	/// </summary>
	public static class PermissionExtensions
	{
		/// <summary>
		/// Name of the cookie used to track whether the user is in "edit mode"
		/// </summary>
		public static string EDIT_COOKIE_NAME = "nucleus_editmode";

		/// <summary>
		/// Returns a true/false value indicating whether the user is allowed to access the resource represented by the specified permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <param name="site"></param>
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
			return IsPageViewPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission type is a <see cref="Page"/> View permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsPageViewPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.PAGE_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Page"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsPageEditPermission(this Permission permission)
		{
			return IsPageEditPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission type is a <see cref="Page"/> Edit permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsPageEditPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.PAGE_EDIT;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Folder"/> View permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsFolderViewPermission(this Permission permission)
		{
			return IsFolderViewPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission typeis a <see cref="Folder"/> View permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsFolderViewPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.FOLDER_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="Folder"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsFolderEditPermission(this Permission permission)
		{
			return IsFolderEditPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permissiontype  is a <see cref="Folder"/> Edit permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsFolderEditPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.FOLDER_EDIT;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="PageModule"/> View permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsModuleViewPermission(this Permission permission)
		{
			return IsModuleViewPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission type is a <see cref="PageModule"/> View permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsModuleViewPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.MODULE_VIEW;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission is a <see cref="PageModule"/> Edit permission.
		/// </summary>
		/// <param name="permission"></param>
		/// <returns></returns>
		public static Boolean IsModuleEditPermission(this Permission permission)
		{
			return IsModuleEditPermission(permission.PermissionType);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the specified permission type is a <see cref="PageModule"/> Edit permission.
		/// </summary>
		/// <param name="permissionType"></param>
		/// <returns></returns>
		public static Boolean IsModuleEditPermission(this PermissionType permissionType)
		{
			return permissionType.Scope == PermissionType.PermissionScopes.MODULE_EDIT;
		}

		/// <summary>
		/// Returns a value specifying whether the user is in edit mode.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context)
		{			
			if (Boolean.TryParse(context.Request.Cookies[EDIT_COOKIE_NAME], out Boolean isEditMode))
			{
				return isEditMode;
			}			

			return false;
		}

		/// <summary>
		/// Returns a value specifying whether the user has permissions to edit the specified module and has clicked the control panel button to enable
		/// editing (which sets a cookie).
		/// </summary>
		/// <param name="user"></param>
		/// <param name="context"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="module"></param>
		/// <returns></returns>
		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context, Site site, Page page, PageModule module)
		{
			if (user.HasEditPermission(site, page, module))
			{
				return IsEditing(user, context);				
			}

			return false;
		}

		/// Returns a value specifying whether the user has permissions to edit the specified page and has clicked the control panel button to enable
		/// editing (which sets a cookie).
		public static Boolean IsEditing(this System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Http.HttpContext context, Site site, Page page)
		{
			if (user.CanEditContent(site, page))
			{
				return IsEditing(user, context);
			}

			return false;
		}

		/// <summary>
		/// Returns a value specifying whether the user has permission to edit the specified page.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <returns></returns>
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
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
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
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
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
		/// Returns a true/false value indicating whether the role has view rights for the page.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="page"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this Role role, Page page)
		{
			foreach (Permission permission in page.Permissions)
			{
				if (permission.IsPageViewPermission() && permission.Role.Equals(role) && permission.AllowAccess)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has view rights for the module.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="moduleInfo"></param>
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
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="moduleInfo"></param>
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
		/// Returns a true/false value indicating whether the role has view rights for the module.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="moduleInfo"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this Role role, PageModule moduleInfo)
		{
			foreach (Permission permission in moduleInfo.Permissions)
			{
				if (permission.IsModuleViewPermission() && permission.Role.Equals(role) && permission.AllowAccess)
				{
					return true;
				}
			}

			return false;
		}


		/// <summary>
		/// Returns a true/false value indicating whether the user has view rights for the folder.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Folder folder)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			if (folder == null) return false;

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
		/// <param name="user"></param>
		/// <param name="site"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		public static Boolean HasEditPermission(this System.Security.Claims.ClaimsPrincipal user, Site site, Folder folder)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}

			if (folder == null) return false;

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

		/// <summary>
		/// Returns a true/false value indicating whether the role has view rights for the folder.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		public static Boolean HasViewPermission(this Role role, Folder folder)
		{
			if (folder == null) return false;

			foreach (Permission permission in folder.Permissions)
			{
				if (permission.IsFolderViewPermission() && permission.Role.Equals(role) && permission.AllowAccess)
				{
					return true;
				}
			}

			return false;
		}
	}
}
