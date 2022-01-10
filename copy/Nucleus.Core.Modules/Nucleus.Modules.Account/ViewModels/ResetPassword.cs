using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Account.ViewModels
{
	public class ResetPassword
	{
		public string ControlId { get; set; }

		[Required(ErrorMessage = "User name is required")]
		public string UserName { get; set; }

		[Required(ErrorMessage = "Password reset token is required")]
		public string PasswordResetToken { get; set; }

		[Required(ErrorMessage = "New Password is required")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Confirm Password is required")]
		[Compare(nameof(NewPassword), ErrorMessage = "The new password and confirm password values must match")]
		public string ConfirmPassword { get; set; }

		public string Message { get; set; }


	}
}
