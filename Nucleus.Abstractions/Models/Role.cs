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
	/// Represents a role.  
	/// </summary> 
	/// <remarks>
	/// Roles provide access rights to users who belong to them for the site and it's pages and modules.
	/// </remarks>
	public class Role : ModelBase
	{
		/// <summary>
		/// Role Type is used to configure restrictions on roles.  The value is a flag, so the values can be combined.
		/// </summary>
		[Flags]
		public enum RoleType
		{
			/// <summary>
			/// Normal roles have no restrictions
			/// </summary>
			Normal = 0,	
			/// <summary>
			/// System roles cannot be deleted or modified, but can have users added and removed from them.
			/// </summary>
			System = 1,
			/// <summary>
			/// Restricted roles cannot have users added or removed from them.
			/// </summary>
			Restricted = 2
		}

		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }
			
		/// <summary>
		/// Role Name
		/// </summary>
		/// <remarks>
		/// Role names are displayed in the administrative interface.
		/// </remarks>		
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Role Group
		/// </summary>
		/// <remarks>
		/// Role groups can be used to categorize roles.
		/// </remarks>
		public RoleGroup RoleGroup { get; set; }

		/// <summary>
		/// Role Description
		/// </summary>
		/// <remarks>
		/// Role descriptions are for reference purposes only.  They are displayed in the administrative interface.
		/// </remarks>
		public string Description { get; set; }

		/// <summary>
		/// Role Type
		/// </summary>
		/// <remarks>
		/// Role types can be combined to restrict the ability of administrators to change the role, or add/remove users.
		/// </remarks>
		public RoleType Type { get; set; }

		/// <summary>
		/// A list of users who belong to the role.
		/// </summary>
		/// <remarks>
		/// This property exists primarily to establish a relationship for Entity-framework, but may be used in the future for
		/// other purposes.
		/// </remarks>
		public List<User> Users { get; set; }

		/// <summary>
		/// Determines whether two Role instances are equal by comparing their Ids.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override Boolean Equals(object other)
		{
			return this.Id == (other as Role)?.Id;
		}

		/// <summary>
		/// Executes the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		/// <remarks>
		/// This implementation exists to prevent a CS0659 compiler warning
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}
	}
}
