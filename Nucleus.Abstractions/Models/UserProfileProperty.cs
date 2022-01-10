using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a <see cref="Site"/>s User Profile Properties, which are the available values for a <see cref="User"/>'s profile.
	/// </summary>
	public class UserProfileProperty : ModelBase
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the display name for the user profile property.
		/// </summary>
		[Required]
		public string Name { get; set; }
		
		/// <summary>
		/// Gets or sets the help text for the user profile property.
		/// </summary>
		public string HelpText { get; set; }

#nullable enable
		/// <summary>
		/// Gets or sets the Type Uri for the user profile property.
		/// </summary>
		/// <remarks>
		/// If set, the Type Uri is used to set a Claim, which can be used by elements of the site, including core functions, modules and 
		/// other extensions.  The Type Uri can be null.
		/// </remarks>
		public string? TypeUri { get; set; }
#nullable disable

		/// <summary>
		/// Gets or sets the sort order for the user profile property.
		/// </summary>
		/// <remarks>
		/// The user profile user interface displays user profile properties in the order specified by SortOrder.
		/// </remarks>
		public int SortOrder { get; set; }
	}
}
