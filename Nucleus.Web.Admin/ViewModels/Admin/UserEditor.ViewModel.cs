using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class UserEditor 
	{		
		public Boolean IsCurrentUser { get; set; }
    public Boolean IsPasswordExpired { get; set; }
    public Boolean HasNameProfileProperties { get; set; }
    public DateTime LockoutResetDate { get; set; }


    public Nucleus.Abstractions.Models.User User { get; set; }
		public string EnteredPassword { get; set; }
    public IEnumerable<SelectListItem> AvailableRoles { get; set; } 
		public Guid SelectedRoleId { get; set; }
		public ClaimTypeOptions ClaimTypeOptions { get; set; }

    public Boolean ExpirePassword { get; set; }
  }
}
