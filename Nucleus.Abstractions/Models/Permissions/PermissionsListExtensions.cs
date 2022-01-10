//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Nucleus.Abstractions.Models;

//namespace Nucleus.Abstractions.Models.Permissions
//{
//	/// <summary>
//	/// Utilities to convert a list of permissions to a "PermissionsList" and back by pivoting the data to a form which suits on-screen display.
//	/// </summary>
//	public static class PermissionsListExtensions
//	{
//		/// <summary>
//		/// Convert a list of permissions to a PermissionsList.  PermissionsList is used for on-screen display of permissions in editors.
//		/// </summary>
//		/// <param name="permissions"></param>
//		/// <returns>PermissionsList</returns>
//		public static PermissionsList ToPermissionsList(this IEnumerable<Permission> permissions)
//		{
//			PermissionsList results = new();

//			foreach (Role role in permissions
//				.Select((permission) => permission.Role)
//				.GroupBy(role => role.Id)
//				.Select(group => group.FirstOrDefault())
//				.OrderBy(role => role.Name))
//			{
//				List<Permission> rolePermissions = permissions.Where(permission => permission.Role.Id == role.Id).OrderBy(permission => permission.PermissionType.SortOrder).ToList();
//				results.Add(role, rolePermissions);
//			}

//			return results;
//		}

//		/// <summary>
//		/// Convert a PermissionsList to an ordinary list of permissions.
//		/// </summary>
//		/// <param name="permissionsList"></param>
//		/// <returns></returns>
//		public static List<Permission> ToList(this PermissionsList permissionsList)
//		{
//			return permissionsList.SelectMany(item => item.Value.Permissions).ToList();
//		}

//	}
//}
