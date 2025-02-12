﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Account.ViewModels
{
	public class ChangePassword
	{		
		public string ReturnUrl { get; set; }

		public Boolean ExistingPasswordBlank { get; set; } = false;
    public Boolean UserPasswordExpired { get; set; } = false;

    [Required(ErrorMessage = "Password is required")]
		public string Password { get; set; }

		[Required(ErrorMessage = "New Password is required")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Confirm Password is required")]
		[Compare(nameof(NewPassword), ErrorMessage = "The new password and confirm password values must match")]
		public string ConfirmPassword { get; set; }



	}
}
