using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Permissions
{
	public class PermissionsListItem 
	{ 
		public Role Role { get; set; }
		public IList<Permission > Permissions { get; set; }
	}
}
