using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Account.ViewModels
{
	public class Signup
	{
		public string ControlId { get; set; }
		public Boolean ShowForm { get; set; }
		public string ReturnUrl { get; set; }

		public Nucleus.Abstractions.Models.User User { get; set; }
		public ClaimTypeOptions ClaimTypeOptions { get; set; }

		[Required(ErrorMessage = "Password is required")]
		public string NewPassword { get; set; }
		public string ConfirmPassword { get; set; }

		public string Message { get; set; }
	}
}
