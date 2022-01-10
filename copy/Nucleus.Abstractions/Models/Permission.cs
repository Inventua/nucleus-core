using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents access rights for <see cref="Page"/> or <see cref="PageModule"/> and <see cref="Role"/>.
	/// </summary>
	public class Permission
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Specifies the access right (<see cref="PermissionType"/>) that this permission record applies to.
		/// </summary>
		/// <remarks>
		/// Permission types are extensible, but the default values represent Page View, Page Edit, Module View and Module Edit permissions.
		/// </remarks>
		public PermissionType PermissionType { get; set; }

		/// <summary>
		/// The <see cref="Role"/> that this permission applies to.
		/// </summary>		
		public Role Role { get; set; }
		
		/// <summary>
		/// Flag indicating whether to allow access, or not.
		/// </summary>
		/// <remarks>
		/// A False value for the permission does not automatically prevent access, a user may be granted access by belonging to another role
		/// which has permission to access the resource.
		/// </remarks>
		public Boolean AllowAccess { get; set; }

	}
}
