using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.ViewModels
{
	public class Recover
	{
		public string ReturnUrl { get; set; }

		public string Email { get; set; }
		public string Message { get; set; }
		public Boolean AllowUsernameRecovery { get; set; }
		public Boolean AllowPasswordReset { get; set; }

	}
}
