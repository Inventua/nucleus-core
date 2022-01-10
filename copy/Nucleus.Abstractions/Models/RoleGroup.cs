using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a role group.  Role groups can be used to categorize roles.
	/// </summary>
	public class RoleGroup
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Role Group Name
		/// </summary>
		/// <remarks>
		/// Role Group names are displayed in the administrative interface.
		/// </remarks>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Role descriptions are for reference purposes only.  They are displayed in the administrative interface.
		/// </summary>
		public string Description { get; set; }
		
	}
}
