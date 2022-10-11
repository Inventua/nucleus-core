using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class UserEditor 
	{		
		public Boolean IsCurrentUser { get; set; }
		public Boolean HasNameProfileProperties { get; set; }

		public Nucleus.Abstractions.Models.User User { get; set; }
		public string EnteredPassword { get; set; }
		public List<Role> AvailableRoles { get; set; } = new();
		public Guid SelectedRoleId { get; set; }
		public ClaimTypeOptions ClaimTypeOptions { get; set; }
	}
}
