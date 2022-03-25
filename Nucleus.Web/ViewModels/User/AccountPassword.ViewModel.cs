using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.User
{
	public class AccountPassword 
	{
		public string ReturnUrl { get; set; }

		public string Username { get; set; }
		public string Password { get; set; }
		public string NewPassword { get; set; }
		public string ConfirmPassword { get; set; }
		public Boolean RememberMe { get; set; }

		public Boolean ShowVerificationToken { get; set; }
		public string VerificationToken { get; set; }

	}
}
