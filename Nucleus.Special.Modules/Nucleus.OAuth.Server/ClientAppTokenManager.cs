using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.OAuth.Server.DataProviders;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="ClientAppToken"/>s.
	/// </summary>
	public class ClientAppTokenManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public ClientAppTokenManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="ClientAppToken"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="ClientAppToken"/> is not saved to the database until you call <see cref="Save(ClientAppToken)"/>.
		/// </remarks>
		public ClientAppToken CreateNew()
		{
			ClientAppToken result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="ClientAppToken"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ClientAppToken> Get(Guid id)
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				return await provider.GetToken(id);
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="ClientAppToken"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ClientAppToken> GetByCode(string code)
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				return await provider.GetTokenByCode(code);
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="ClientAppToken"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ClientAppToken> GetByAccessToken(string accessToken)
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				return await provider.GetTokenByAccessToken(accessToken);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="ClientAppToken"/> from the database.
		/// </summary>
		/// <param name="ClientAppToken"></param>
		public async Task Delete(ClientAppToken clientAppToken)
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				await provider.DeleteToken(clientAppToken);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ClientAppToken"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="ClientAppToken"></param>
		public async Task Save(ClientAppToken clientAppToken)
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				await provider.SaveToken(clientAppToken);
			}
		}

		/// <summary>
		/// Remove expired tokens
		/// </summary>
		/// <param name="expiryThreshold"></param>
		/// <returns></returns>
		public async Task ExpireTokens()
		{
			using (IClientAppTokenDataProvider provider = this.DataProviderFactory.CreateProvider<IClientAppTokenDataProvider>())
			{
				await provider.ExpireTokens(TimeSpan.FromDays(1));
			}
		}
	}
}
