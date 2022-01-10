using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Permissions
{
	/// <summary>
	/// Represent a list of permissions, keyed by Id.
	/// </summary>
	/// <remarks>
	/// This class is used by views which render controls used to selected permissions for an entity.
	/// </remarks>
	public class PermissionsList : Dictionary<Guid, PermissionsListItem>
	{
		/// <summary>
		/// Create and add a new PermissionsListItem.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="permissions"></param>
		public void Add(Role role, IList<Permission> permissions)
		{
			base.Add(role.Id, new PermissionsListItem() { Role = role, Permissions = permissions });
		}
	}

}
