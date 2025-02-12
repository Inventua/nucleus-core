﻿using Nucleus.Abstractions.Models;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.DataProviders
{
	public interface IClientAppDataProvider : IDisposable
	{
		public Task<ClientApp> GetClientApp(Guid Id);
		public Task<ClientApp> GetClientAppByApiKey(Guid Id);
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp>> ListClientApps(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		public Task SaveClientApp(Site site, ClientApp clientApp);
		public Task DeleteClientApp(ClientApp clientApp);

	}
}
