using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Account.ViewModels
{
	public class Recover
	{
		[Required(ErrorMessage = "Please enter your email address.")]

		public string Email { get; set; }
		public Boolean AllowUsernameRecovery { get; set; }
		public Boolean AllowPasswordReset { get; set; }

	}
}
