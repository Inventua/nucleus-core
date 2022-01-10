using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents the setting for a user profile value.
	/// </summary>
	public class UserProfileValue : ModelBase
	{
		/// <summary>
		/// Reference to UserProfileProperty which gives context to the <see cref="Value"/>.
		/// </summary>
		public UserProfileProperty UserProfileProperty { get; set; }
		
		/// <summary>
		/// Profile value
		/// </summary>
		public string Value { get; set; }

	}
}
