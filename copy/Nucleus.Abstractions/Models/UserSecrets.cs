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
	public class UserSecrets
	{
		public string PasswordHash { get; set; }
		public string PasswordHashAlgorithm { get; set; }		
		public string PasswordResetToken { get; set; }
		public DateTime PasswordResetTokenExpiryDate { get; set; }

		public string PasswordQuestion { get; set; }
		public string PasswordAnswer { get; set; }
		public DateTime LastLoginDate { get; set; }
		public DateTime LastPasswordChangedDate { get; set; }
		public DateTime LastLockoutDate { get; set; }
		public Boolean IsLockedOut { get; set; }
		public int FailedPasswordAttemptCount { get; set; }
		public DateTime FailedPasswordWindowStart { get; set; }
		public int FailedPasswordAnswerAttemptCount { get; set; }
		public DateTime FailedPasswordAnswerWindowStart { get; set; }
		public string Salt { get; set; }

		/// <summary>
		/// Compare the submitted password with the user's saved password.
		/// </summary>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// Login modules should use <see langword="Nucleus.Core.UserManager.VerifyPassword" /> rather than calling this function directly, because
		/// UserManager.VerifyPassword tracks login failures and manages account suspension.
		/// </remarks>
		public Boolean VerifyPassword(string password)
		{
			if (String.IsNullOrEmpty(password)) return false;

			return HashPassword(password) == this.PasswordHash;
		}

		/// <summary>
		/// Set the password for the user.
		/// </summary>
		/// <param name="newPassword"></param>

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
