using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using Nucleus.Data.Common;
using Nucleus.Modules.Links.DataProviders;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Links"/>s.
	/// </summary>
	public class LinksManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }
		private IPageManager PageManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public LinksManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IPageManager pageManager, IFileSystemManager fileSystemManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.PageManager = pageManager;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Create a new <see cref="Links"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Links"/> is not saved to the database until you call <see cref="Save(PageModule, Links)"/>.
		/// </remarks>
		public Task<Link> CreateNew()
		{
			Link result = new();

			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="Links"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Link> Get(Site site, Guid id)
		{
			return await this.CacheManager.LinksCache().GetAsync(id, async id =>
			{
				using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
				{
					Link result = await provider.Get(id);
					if (result != null)
					{
						await GetLinkItem(site, result);
					}

					return result;
				}
			});
		}

		private async Task GetLinkItem(Site site, Link result)
		{
			switch (result.LinkType)
			{
				case LinkTypes.File:
					if (result.LinkFile == null)
					{
						result.LinkFile = new();
					}

					if (result.LinkFile.File != null)
					{
						result.LinkFile.File = await this.FileSystemManager.GetFile(site, result.LinkFile.File.Id);
						result.LinkFile.File.Parent.Permissions = await this.FileSystemManager.ListPermissions(result.LinkFile.File.Parent);
					}
					else
					{
						result.LinkFile.File = new();
					}

					break;
				case LinkTypes.Page:
					if (result.LinkPage == null)
					{
						result.LinkPage = new();
					}
					if (result.LinkPage.Page != null)
					{
						result.LinkPage.Page = await this.PageManager.Get(result.LinkPage.Page.Id);
					}
					else
					{
						result.LinkPage.Page = new();
					}
					break;

				case LinkTypes.Url:
					if (result.LinkUrl == null)
					{
						result.LinkUrl = new();
					}
					break;
			}
		}
		/// <summary>
		/// Delete the specified <see cref="Links"/> from the database.
		/// </summary>
		/// <param name="Links"></param>
		public async Task Delete(Link link)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				await provider.Delete(link);
				this.CacheManager.LinksCache().Remove(link.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Links"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<Link>> List(Site site, PageModule module)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				IList<Link> results = await provider.List(module);

				foreach (Link link in results)
				{
					await GetLinkItem(site, link);
				}

				return results;
			}
		}

		/// <summary>
		/// Update the <see cref="Models.Link.SortOrder"/> of the link specifed by id by swapping it with the next-highest <see cref="Models.Link.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public async Task MoveDown(PageModule module, Guid id)
		{
			Link previousLink = null;

			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				List<Link> links = await provider.List(module);
				links.Reverse();

				foreach (Link link in links)
				{
					if (link.Id == id)
					{
						if (previousLink != null)
						{
							int temp = link.SortOrder;
							link.SortOrder = previousLink.SortOrder;
							previousLink.SortOrder = temp;

							await provider.Save(module, previousLink);
							await provider.Save(module, link);

							this.CacheManager.LinksCache().Remove(id);
							this.CacheManager.LinksCache().Remove(previousLink.Id);

							break;
						}
					}
					else
					{
						previousLink = link;
					}
				}
			}
		}

		/// <summary>
		/// Update the <see cref="Models.Link.SortOrder"/> of the link specifed by id by swapping it with the previous <see cref="Models.Link.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public async Task MoveUp(PageModule module, Guid id)
		{
			Link previousLink = null;

			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				List<Link> links = await provider.List(module);

				foreach (Link Link in links)
				{
					if (Link.Id == id)
					{
						if (previousLink != null)
						{
							int temp = Link.SortOrder;
							Link.SortOrder = previousLink.SortOrder;
							previousLink.SortOrder = temp;

							await provider.Save(module, previousLink);
							await provider.Save(module, Link);

							this.CacheManager.LinksCache().Remove(id);
							this.CacheManager.LinksCache().Remove(previousLink.Id);
							break;
						}
					}
					else
					{
						previousLink = Link;
					}
				}
			}
		}

		/// <summary>
		/// Create or update a <see cref="Link"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="link"></param>
		public async Task Save(PageModule module, Link link)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				await provider.Save(module, link);
				this.CacheManager.LinksCache().Remove(link.Id);
			}
		}

	}
}
