using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extensions for the User class.
	/// </summary>
	public static class UserExtensions
	{
		/// <summary>
		/// Return a copy of the supplied user with sensitive data removed.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static User GetCensored(this User user)
		{
			return new User()
			{
				Id = user.Id,
				Profile = user.Profile,
				UserName = user.UserName,
				Secrets = new() { PasswordResetToken = user.Secrets?.PasswordResetToken }
			};
		}
	}
}
