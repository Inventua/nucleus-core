using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.ViewModels
{
	public class Login
	{
		public string Message { get; set; }

		public string ReturnUrl { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }
		
		public Boolean RememberMe { get; set; }

		public Boolean AllowRememberMe { get; set; }		

		public Boolean AllowUsernameRecovery { get; set; }

		public Boolean AllowPasswordReset { get; set; }

		public Boolean ShowVerificationToken { get; set; }
		
		public string VerificationToken { get; set; }

	}
}
