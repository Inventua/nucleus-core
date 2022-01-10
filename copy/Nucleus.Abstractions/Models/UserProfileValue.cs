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
	public class UserProfileValue
	{
		public UserProfileProperty UserProfileProperty { get; set; }
		public string Value { get; set; }
	}
}
