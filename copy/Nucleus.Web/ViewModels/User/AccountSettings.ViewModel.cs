using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Web.ViewModels.User
{
	public class AccountSettings
	{
		public string ReturnUrl { get; set; }

		public Nucleus.Abstractions.Models.User User { get; set; }
		public ClaimTypeOptions ClaimTypeOptions { get; set; }
		public string Message { get; set; }
	}
}
