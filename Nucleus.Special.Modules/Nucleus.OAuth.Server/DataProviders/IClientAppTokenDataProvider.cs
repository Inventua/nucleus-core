using Nucleus.Abstractions.Models;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.DataProviders
{
	public interface IClientAppTokenDataProvider : IDisposable
	{
		public Task<ClientAppToken> GetToken(Guid Id);
		public Task SaveToken(ClientAppToken clientAppToken);
		public Task DeleteToken(ClientAppToken clientAppToken);

	}
}
