using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to retrieve password complexity rules from configuration files.
	/// </summary>
	public class PasswordOptions
	{
		/// <summary>
		/// Configuration file section key
		/// </summary>
		public const string Section = "Nucleus:PasswordOptions";
		
		/// <summary>
		/// Specifies the time frame in which the user can continue to retry logging in after a failed password, up to FailedPasswordMaxAttempts.
		/// </summary>
		public TimeSpan FailedPasswordWindowTimeout { get; set; } = TimeSpan.FromMinutes(15);

		/// <summary>
		/// Specifies the number of times a user can re-attempt to log in after entering the wrong password, within the time window specified by 
		/// FailedPasswordWindowTimeout.
		/// </summary>
		public int FailedPasswordMaxAttempts { get; set; } = 3;

		/// <summary>
		/// Specifies the duration of a lockout after too many failed password attempts.
		/// </summary>
		public TimeSpan FailedPasswordLockoutReset { get; set; } = TimeSpan.FromMinutes(10);

		/// <summary>
		/// Specifies the time that a password reset token is valid for.
		/// </summary>
		public TimeSpan PasswordResetTokenExpiry { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
		/// Specifies the time that a password is valid for.
		/// </summary>
		public TimeSpan? PasswordExpiry { get; set; }

    /// <summary>
    /// Specifies the time that a user verification token is valid for.
    /// </summary>
    public TimeSpan VerificationTokenExpiry { get; set; } = TimeSpan.FromDays(7);

		/// <summary>
		/// List of password complexity rules
		/// </summary>
		public List<PasswordComplexityRule> PasswordComplexityRules { get; private set; } = new();
	}
}
