using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.ViewModels
{
	public class Login
	{
		public string ReturnUrl { get; set; }
		public string ControlId { get; set;  } 
		public string Username { get; set; }
		public string Password { get; set; }
		
		public Boolean RememberMe { get; set; }

		public Boolean AllowRememberMe { get; set; }		
		public Boolean AllowUsernameRecovery { get; set; }
		public Boolean AllowPasswordReset { get; set; }
		
	}
}
