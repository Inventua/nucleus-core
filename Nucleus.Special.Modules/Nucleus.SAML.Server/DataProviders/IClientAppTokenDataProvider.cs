using Nucleus.Abstractions.Models;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server.DataProviders
{
	public interface IClientAppTokenDataProvider : IDisposable
	{
		public Task<ClientAppToken> GetToken(Guid id);
		public Task<ClientAppToken> GetTokenByCode(string code);
		public Task SaveToken(ClientAppToken clientAppToken);
		public Task DeleteToken(ClientAppToken clientAppToken);

		public Task ExpireTokens(TimeSpan expiryThreshold);
	}
}
