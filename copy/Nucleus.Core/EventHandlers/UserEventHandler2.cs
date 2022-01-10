using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Core.EventHandlers.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Nucleus.Core.EventHandlers
{
	/// <summary>
	/// Perform operations after a user is created.
	/// </summary>
	/// <param name="user"></param>
	/// <remarks>
	/// This class has no implementation and is currently used for testing.
	/// </remarks>
	public class UserEventHandler2 : ISystemEventHandler<User, Create>
	{
		public UserEventHandler2()
		{

		}

		public void Invoke(User user)
		{
			
		}
	}
}
