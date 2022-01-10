using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Permissions
{
	public static class PermissionsListExtensions
	{
		public static PermissionsList ToPermissionsList(this IEnumerable<Permission> permissions)
		{
			PermissionsList results = new();

			foreach (Role role in permissions
				.Select((permission) => permission.Role)
				.GroupBy(r => r.Id)
				.Select(group => group.FirstOrDefault())
				.OrderBy(role => role.Name))
			{
				List<Permission> rolePermissions = permissions.Where(permission => permission.Role.Id == role.Id).OrderBy(permission => permission.PermissionType.SortOrder).ToList();
				results.Add(role, rolePermissions);
			}

			return results;
		}

		public static List<Permission> ToList(this PermissionsList permissionsList)
		{
			return permissionsList.SelectMany(item => item.Value.Permissions).ToList();
		}

	}
}
