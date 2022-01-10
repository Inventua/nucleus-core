using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a user of a <see cref="Site"/>
	/// </summary>
	public class User
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }
		
		/// <summary>
		/// User login name
		/// </summary>
		[Required(ErrorMessage = "User name is required")]
		public string UserName { get; set; }

		/// <summary>
		/// Flag used to indicate that the user is a system administrator.
		/// </summary>
		/// <remarks>
		/// System administrators can access and manage all sites, pages and their resources.  System administrators do not belong to any site.
		/// </remarks>
		public Boolean IsSystemAdministrator { get; set; }

		/// <summary>
		/// User secrets.
		/// </summary>
		public UserSecrets Secrets { get; set; }

		/// <summary>
		/// List of User profile values.  
		/// </summary>
		/// <remarks>
		/// The available user profile values are controlled by the <see cref="Site"/>s <see cref="UserProfileProperty"/> settings.
		/// </remarks>
		public List<UserProfileValue> Profile { get; set; } = new();

		/// <summary>
		/// List of <see cref="Role"/>s that the user belongs to.
		/// </summary>
		public List<Role> Roles { get; set; }
		//public List<Role> Roles { get; set; }
		
	}
}
