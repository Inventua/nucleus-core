using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.DataProviders
{
	/// <summary>
	/// Data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the IOAuthServerDataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class OAuthServerDataProvider : Nucleus.Data.EntityFramework.DataProvider, IClientAppDataProvider, IClientAppTokenDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new OAuthServerDbContext Context { get; }

		public OAuthServerDataProvider(OAuthServerDbContext context, IEventDispatcher eventManager, ILogger<OAuthServerDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<ClientApp> GetClientApp(Guid id) 
		{
			return await this.Context.ClientApps
				.Where(ClientApp => ClientApp.Id == id)
				.Include(ClientApp => ClientApp.LoginPage)
				.Include(ClientApp => ClientApp.ApiKey)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ClientApp> GetClientAppByApiKey(Guid id)
		{
			return await this.Context.ClientApps
				.Where(ClientApp => ClientApp.ApiKey.Id == id)
				.Include(ClientApp => ClientApp.LoginPage)
				.Include(ClientApp => ClientApp.ApiKey)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}


		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp>> ListClientApps(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			List<ClientApp> results = new();

			var query = this.Context.ClientApps
				.Where(clientApp => EF.Property<Guid>(clientApp, "SiteId") == site.Id)
				.Include(clientApp => clientApp.LoginPage)
				.Include(clientApp => clientApp.ApiKey);

			pagingSettings.TotalCount = await query.CountAsync();

			results = await query
				.Skip(pagingSettings.FirstRowIndex)
				.Take(pagingSettings.PageSize)
				.AsNoTracking()
				.AsSingleQuery()
				.ToListAsync();

			return new Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp>(pagingSettings, results);
			
		}

		public async Task SaveClientApp(Site site, ClientApp clientApp)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.ClientApps.Where(existing => existing.Id == clientApp.Id).AnyAsync();

			this.Context.Attach(clientApp);
			this.Context.Entry(clientApp).Property("SiteId").CurrentValue = site.Id;

			if (isNew)
			{
				this.Context.Entry(clientApp).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<ClientApp, Create>(clientApp); });
			}
			else
			{
				this.Context.Entry(clientApp).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<ClientApp, Update>(clientApp); });
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task DeleteClientApp(ClientApp clientApp)
		{
			this.Context.Remove(clientApp);
			await this.Context.SaveChangesAsync<ClientApp>();
		}


		public async Task<ClientAppToken> GetToken(Guid id)
		{
			return await this.Context.ClientAppTokens
				.Where(clientAppToken => clientAppToken.Id == id)
				.Include(clientAppToken => clientAppToken.ClientApp)
					.ThenInclude(clientApp => clientApp.ApiKey)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ClientAppToken> GetTokenByCode(string code)
		{
			return await this.Context.ClientAppTokens
			.Where(clientAppToken => clientAppToken.Code == code)
			.Include(clientAppToken => clientAppToken.ClientApp)
				.ThenInclude(clientApp => clientApp.ApiKey)
			.AsNoTracking()
			.FirstOrDefaultAsync();
		}

		public async Task<ClientAppToken> GetTokenByAccessToken(string accessToken)
		{
			return await this.Context.ClientAppTokens
			.Where(clientAppToken => clientAppToken.AccessToken == accessToken)
			.Include(clientAppToken => clientAppToken.ClientApp)
				.ThenInclude(clientApp => clientApp.ApiKey)
			.AsNoTracking()
			.FirstOrDefaultAsync();
		}

		public async Task SaveToken(ClientAppToken clientAppToken)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.ClientAppTokens.Where(existing => existing.Id == clientAppToken.Id).AnyAsync();

			this.Context.Attach(clientAppToken);

			if (isNew)
			{
				this.Context.Entry(clientAppToken).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<ClientAppToken, Create>(clientAppToken); });
			}
			else
			{
				this.Context.Entry(clientAppToken).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<ClientAppToken, Update>(clientAppToken); });
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task DeleteToken(ClientAppToken clientAppToken)
		{
			this.Context.Remove(clientAppToken);
			await this.Context.SaveChangesAsync<ClientAppToken>();
		}

		public async Task ExpireTokens(TimeSpan expiryThreshold)
		{
			DateTime expiryDate = DateTime.UtcNow.Add(-expiryThreshold);
			
			List<ClientAppToken> expiredTokens = await this.Context.ClientAppTokens
				.Where(clientAppToken => clientAppToken.DateAdded < expiryDate)
				.ToListAsync();

			this.Context.RemoveRange(expiredTokens);

			await this.Context.SaveChangesAsync<ClientAppToken>();
		}
	}
}