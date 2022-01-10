using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Account.ViewModels
{
	public class UserProfile
	{
		public string ReturnUrl { get; set; }

		public Nucleus.Abstractions.Models.User User { get; set; }
		public ClaimTypeOptions ClaimTypeOptions { get; set; }
	}
}
