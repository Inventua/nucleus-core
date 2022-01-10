using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Permissions
{
	/// <summary>
	/// Represents a permission entry in a PermissionList
	/// </summary>
	public class PermissionsListItem 
	{ 
		/// <summary>
		/// The Role assigned to this permission.
		/// </summary>
		public Role Role { get; set; }

		/// <summary>
		/// List of permissions for the assigned Role.
		/// </summary>
		public IList<Permission > Permissions { get; set; }
	}
}
