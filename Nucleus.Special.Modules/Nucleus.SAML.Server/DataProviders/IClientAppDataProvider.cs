using Nucleus.Abstractions.Models;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server.DataProviders
{
	public interface IClientAppDataProvider : IDisposable
	{
		public Task<ClientApp> GetClientApp(Guid Id);
		public Task<ClientApp> GetClientAppByIssuer(string issuer);
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp>> ListClientApps(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		public Task SaveClientApp(Site site, ClientApp clientApp);
		public Task DeleteClientApp(ClientApp clientApp);

	}
}
