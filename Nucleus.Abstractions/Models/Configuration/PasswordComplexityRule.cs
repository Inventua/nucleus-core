using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents a password validation rule.  
	/// </summary>
	/// <remarks>
	/// New passwords are validated by every rule specified in configuration files and must pass every rule.
	/// </remarks>
	public class PasswordComplexityRule
	{
		/// <summary>
		/// Regular expression used to validate the password.
		/// </summary>
		public string Pattern { get; private set; }

		/// <summary>
		/// Validation failure message.
		/// </summary>
		public string Message { get; private set; }
	}
}
