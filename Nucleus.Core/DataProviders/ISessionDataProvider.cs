using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="UserSession"/> class.
	internal interface ISessionDataProvider : IDisposable//, IDataProvider<ISessionDataProvider>
	{
		abstract Task SaveUserSession(UserSession session);
		abstract Task<UserSession> GetUserSession(Guid sessionId);
		abstract Task DeleteUserSession(UserSession session);
		abstract Task DeleteExpiredSessions();
	}
}
