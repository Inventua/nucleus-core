using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Data.Common;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Security.Claims;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Links.Models;
using Microsoft.EntityFrameworkCore;
using Nucleus.Abstractions.Managers;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.DataProviders
{
	/// <summary>
	/// Links module data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the ILinksDataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class LinksDataProvider : Nucleus.Data.EntityFramework.DataProvider, ILinksDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new LinksDbContext Context { get; }
		
		public LinksDataProvider(LinksDbContext context, IEventDispatcher eventManager, ILogger<LinksDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<Link> Get(Guid id)
		{
			Link result = await this.Context.Links
				.Where(link => link.Id == id)
				.Include(link => link.Category)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				switch (result.LinkType)
				{
					case LinkTypes.Url:
						result.LinkUrl = await GetUrlLinkItem(result.Id);
						break;
					case LinkTypes.File:
						result.LinkFile = await GetFileLinkItem(result.Id);
						break;
					case LinkTypes.Page:
						result.LinkPage = await GetPageLinkItem(result.Id);
						break;
				}
			}

			return result;
		}

		private async Task<LinkUrl> GetUrlLinkItem(Guid linkId)
		{
			return await this.Context.LinkUrls
				.Where(linkUrl => EF.Property<Guid>(linkUrl, "LinkId") == linkId)
				.AsNoTracking()
				.FirstOrDefaultAsync();			
		}

		private async Task<LinkFile> GetFileLinkItem(Guid linkId)
		{
			return await this.Context.LinkFiles
				.Where(linkFile => EF.Property<Guid>(linkFile, "LinkId") == linkId)
				.Include(linkFile => linkFile.File)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		private async Task<LinkPage> GetPageLinkItem(Guid linkId)
		{
			return await this.Context.LinkPages
				.Where(linkPage => EF.Property<Guid>(linkPage, "LinkId") == linkId)
				.Include(linkPage => linkPage.Page)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<List<Link>> List(PageModule pageModule)
		{
			List<Link> results = await this.Context.Links
				.Where(link => EF.Property<Guid>(link, "ModuleId") == pageModule.Id)
				.OrderBy(link => link.SortOrder)
				.AsNoTracking()
				.ToListAsync();

			foreach (Link result in results)
			{
				switch (result.LinkType)
				{
					case LinkTypes.Url:
						result.LinkUrl = await GetUrlLinkItem(result.Id);
						break;
					case LinkTypes.File:
						result.LinkFile = await GetFileLinkItem(result.Id);
						break;
					case LinkTypes.Page:
						result.LinkPage = await GetPageLinkItem(result.Id);
						break;
				}
			}		

			return results;
		}

		public async Task Delete(Link link)
		{
			this.Context.Remove(link);
			await this.Context.SaveChangesAsync<Link>();
		}

		public async Task Save(PageModule pageModule, Link link)
		{
			Action raiseEvent;

			Boolean isNew = !this.Context.Links.Where(existing => existing.Id == link.Id)
				.AsNoTracking()
				.Any();

			this.Context.Attach(link);
			this.Context.Entry(link).Property("ModuleId").CurrentValue = pageModule.Id;

			if (isNew)
			{
				link.SortOrder = await GetTopLinkSortOrder(pageModule.Id) + 10;
				this.Context.Entry(link).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Link, Create>(link); });
			}
			else
			{
				this.Context.Entry(link).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Link, Update>(link); });
			}

			await this.Context.SaveChangesAsync<Link>();

			switch (link.LinkType)
			{
				case LinkTypes.Url:
					await SaveLinkItem(link.Id, link.LinkUrl);
					break;
				case LinkTypes.File:
					await SaveLinkItem(link.Id, link.LinkFile);
					break;
				case LinkTypes.Page:
					await SaveLinkItem(link.Id, link.LinkPage);
					break;
			}
						
			raiseEvent.Invoke();
		}

		private async Task<int> GetTopLinkSortOrder(Guid moduleId)
		{
			Link link = await this.Context.Links
				.Where(link => EF.Property<Guid>(link, "ModuleId") == moduleId)
				.OrderByDescending(link => link.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return link == null ? 10 : link.SortOrder;
		}

		private async Task SaveLinkItem(Guid linkId, LinkUrl linkItem)
		{
			LinkUrl existingItem = await this.Context.Set<LinkUrl>()
				.Where(existing => EF.Property<Guid>(existing, "LinkId") == linkId)
				.FirstOrDefaultAsync();

			if (existingItem == null)
			{
				this.Context.Add(linkItem);
				this.Context.Entry(linkItem).Property("LinkId").CurrentValue = linkId;
			}
			else
			{
				existingItem.Url = linkItem.Url;
			}

			await this.Context.SaveChangesAsync<LinkUrl>();
		}

		private async Task SaveLinkItem(Guid linkId, LinkFile linkItem)
		{
			LinkFile existingItem = await this.Context.Set<LinkFile>()
				.Where(existing => EF.Property<Guid>(existing, "LinkId") == linkId)
				.FirstOrDefaultAsync();

			if (existingItem == null)
			{
				this.Context.Add(linkItem);
				this.Context.Entry(linkItem).Property("LinkId").CurrentValue = linkId;
			}
			else
			{
				existingItem.File = linkItem.File;
			}

			await this.Context.SaveChangesAsync<LinkFile>();
		}

		private async Task SaveLinkItem(Guid linkId, LinkPage linkItem)
		{
			LinkPage existingItem = await this.Context.Set<LinkPage>()
				.Where(existing => EF.Property<Guid>(existing, "LinkId") == linkId)
				.FirstOrDefaultAsync();

			if (existingItem == null)
			{
				this.Context.Add(linkItem);
				this.Context.Entry(linkItem).Property("LinkId").CurrentValue = linkId;
			}
			else
			{
				existingItem.Page = linkItem.Page;
			}

			await this.Context.SaveChangesAsync<LinkPage>();
		}

	}
}

