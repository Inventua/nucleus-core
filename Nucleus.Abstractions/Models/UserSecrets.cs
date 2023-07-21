using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents password and other user-login information.
	/// </summary>
	public class UserSecrets : ModelBase
	{
		

		/// <summary>
		/// User's password hash.
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// Algorithm used to create the <see cref="PasswordHash"/>.
		/// </summary>
		public string PasswordHashAlgorithm { get; set; }		

		/// <summary>
		/// Auto-generated token used for password resets.
		/// </summary>
		public string PasswordResetToken { get; set; }

    /// <summary>
		/// Expiry date/time for the password.  When a password is expired, the user must enter a new password the next time they log in.
		/// </summary>
		public DateTime? PasswordExpiryDate { get; set; }

    /// <summary>
    /// Expiry date/time for <see cref="PasswordResetToken"/>
    /// </summary>
    public DateTime? PasswordResetTokenExpiryDate { get; set; }

		/// <summary>
		/// Auto-generated token used for new user verification.
		/// </summary>
		public string VerificationToken { get; set; }

		/// <summary>
		/// Expiry date/time for <see cref="VerificationToken"/>
		/// </summary>
		public DateTime? VerificationTokenExpiryDate { get; set; }

		/// <summary>
		/// Not in use
		/// </summary>
		public string PasswordQuestion { get; set; }

		/// <summary>
		/// Not in use
		/// </summary>
		public string PasswordAnswer { get; set; }

		/// <summary>
		/// Date/time of the last successful login.
		/// </summary>
		public DateTime? LastLoginDate { get; set; }

		/// <summary>
		/// Date/time that the user last changed their password.
		/// </summary>
		public DateTime? LastPasswordChangedDate { get; set; }

		/// <summary>
		/// Date/Time of the last lockout for the user.
		/// </summary>
		/// <remarks>
		/// Users are locked out for a configurable period of time after a configurable number of failed password attempts.
		/// </remarks>
		public DateTime? LastLockoutDate { get; set; }

		/// <summary>
		/// Gets or sets whether the user is locked out.
		/// </summary>
		public Boolean IsLockedOut { get; set; }

		/// <summary>
		/// Gets or sets the number of times that the user has entered the wrong password, within the configured time window.
		/// </summary>
		public int FailedPasswordAttemptCount { get; set; }

		/// <summary>
		/// Gets or sets the date/time of the first password attempt failure, within the configured time window.
		/// </summary>
		public DateTime? FailedPasswordWindowStart { get; set; }

		/// <summary>
		/// Not in use
		/// </summary>
		public int FailedPasswordAnswerAttemptCount { get; set; }

		/// <summary>
		/// Not in use
		/// </summary>
		public DateTime? FailedPasswordAnswerWindowStart { get; set; }

		/// <summary>
		/// Random value used by the hash algorithm.
		/// </summary>
		public string Salt { get; set; }

		/// <summary>
		/// Compare the submitted password with the user's saved password.
		/// </summary>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// Login modules should use <see langword="Nucleus.Core.UserManager.VerifyPassword" /> rather than calling this function directly, 
		/// because UserManager.VerifyPassword tracks login failures and manages account suspension.
		/// If the user 'secrets' record has a blank password or blank password hash algorithm, this function always returns false, because
		/// users without a password cannot login using the login module.  Users can have no password if they are authenticated by a different
		/// method such as OAuth, or an Authenticator app.
		/// </remarks>
		public Boolean VerifyPassword(string password)
		{
			if (String.IsNullOrEmpty(password)) return false;
			if (String.IsNullOrEmpty(this.PasswordHash)) return false;
			if (String.IsNullOrEmpty(this.PasswordHashAlgorithm)) return false;

			return HashPassword(password) == this.PasswordHash;
		}

		/// <summary>
		/// Set the password for the user.
		/// </summary>
		/// <param name="newPassword"></param>
    /// <remarks>
    /// Use <seealso cref="Nucleus.Abstractions.Managers.IUserManager.SetPassword"/> rather than this function.  The UserManager method 
    /// sets other properties like the password expiry date.
    /// </remarks>
		public void SetPassword(string newPassword)
		{
			if (String.IsNullOrEmpty(newPassword))
			{
				throw new ArgumentException("Password cannot be blank.", nameof(newPassword));
			}

			if (String.IsNullOrEmpty(this.Salt))
			{
				this.Salt = Guid.NewGuid().ToString();
			}

			this.PasswordHash = HashPassword(newPassword);
			this.PasswordHashAlgorithm = "SHA512";

			this.LastPasswordChangedDate = DateTime.UtcNow;
      this.PasswordExpiryDate = null;
		}

		private string HashPassword(string password)
		{
			using (System.Security.Cryptography.SHA512 hash = System.Security.Cryptography.SHA512.Create())
			{
				return Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + this.Salt)));				
			}
		}
	}
}
