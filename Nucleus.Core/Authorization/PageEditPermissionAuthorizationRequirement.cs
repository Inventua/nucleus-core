using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Core.Authorization
{
	/// <summary>
	/// The PageEditPermissionAuthorizationRequirement interface doesn't do anything, it is just used as a marker ("Represents an authorization requirement") 
	/// for the <see cref="PageEditPermissionAuthorizationHandler"/>
	/// </summary>
	public class PageEditPermissionAuthorizationRequirement : IAuthorizationRequirement { }
}
