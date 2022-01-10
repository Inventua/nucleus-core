using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="UserSession"/> class.
	internal interface ISessionDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract void SaveUserSession(UserSession session);
		abstract UserSession GetUserSession(Guid sessionId);
		abstract void DeleteUserSession(UserSession session);
	}
}
