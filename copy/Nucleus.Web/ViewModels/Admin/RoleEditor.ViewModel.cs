using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class RoleEditor
	{
		public Role Role { get; set; }
		public IEnumerable<RoleGroup> RoleGroups { get; set; } = new List<RoleGroup>();

	}
}
