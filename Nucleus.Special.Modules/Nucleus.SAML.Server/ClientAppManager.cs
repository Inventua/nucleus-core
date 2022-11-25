using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.SAML.Server.DataProviders;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="ClientApp"/>s.
	/// </summary>
	public class ClientAppManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public ClientAppManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="ClientApp"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="ClientApp"/> is not saved to the database until you call <see cref="Save(ClientApp)"/>.
		/// </remarks>
		public Task<ClientApp> CreateNew()
		{
			ClientApp result = new();

			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="ClientApp"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ClientApp> Get(Guid id)
		{
			using (IClientAppDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppDataProvider>())
			{
					return await provider.GetClientApp(id);
				}
		}

		/// <summary>
		/// Retrieve an existing <see cref="ClientApp"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ClientApp> GetByIssuer(Site site, string issuer)
		{
			using (IClientAppDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppDataProvider>())
			{
				return await provider.GetClientAppByIssuer(site, issuer);
			}			
		}

		/// <summary>
		/// Delete the specified <see cref="ClientApp"/> from the database.
		/// </summary>
		/// <param name="clientApp"></param>
		public async Task Delete(ClientApp clientApp)
		{
			using (IClientAppDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppDataProvider>())
			{
				await provider.DeleteClientApp(clientApp);
			}
		}

		/// <summary>
		/// List all <see cref="ClientApp"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IClientAppDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppDataProvider>())
			{
				return await provider.ListClientApps(site, pagingSettings);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ClientApp"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="clientApp"></param>
		public async Task Save(Site site, ClientApp clientApp)
		{
			using (IClientAppDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppDataProvider>())
			{
				await provider.SaveClientApp(site, clientApp);
			}
		}

	}
}
