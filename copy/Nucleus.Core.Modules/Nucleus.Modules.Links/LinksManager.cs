using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
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
		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }
		private PageManager PageManager { get; }
		private FileSystemManager FileSystemManager { get; }

		public LinksManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager, PageManager pageManager, FileSystemManager fileSystemManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.PageManager = pageManager;
			this.FileSystemManager = fileSystemManager;

			this.CacheManager.Add<Guid, Link>(new Nucleus.Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="Links"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Links"/> is not saved to the database until you call <see cref="Save(PageModule, Links)"/>.
		/// </remarks>
		public Link CreateNew()
		{
			Link result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Links"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Link Get(Site site, Guid id)
		{
			return this.CacheManager.LinksCache().Get(id, id =>
			{
				using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
				{
					Link result = provider.Get(id);
					if (result != null)
					{
						GetLinkItem(site, result);
					}

					return result;
				}
			});
		}

		private void GetLinkItem(Site site, Link result)
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
						result.LinkFile.File = this.FileSystemManager.GetFile(site, result.LinkFile.File.Id);
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
						result.LinkPage.Page = this.PageManager.Get(result.LinkPage.Page.Id);
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
		public void Delete(Link link)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				provider.Delete(link);
				this.CacheManager.LinksCache().Remove(link.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Links"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Link> List(Site site, PageModule module)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				IList<Link> results = provider.List(module);

				foreach (Link link in results)
				{
					GetLinkItem(site, link);
				}

				return results;
			}
		}

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public void MoveDown(PageModule module, Guid id)
		{
			Link previousLink = null;

			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				IEnumerable<Link> Links = provider.List(module).Reverse();

				foreach (Link Link in Links)
				{
					if (Link.Id == id)
					{
						if (previousLink != null)
						{
							long temp = Link.SortOrder;
							Link.SortOrder = previousLink.SortOrder;
							previousLink.SortOrder = temp;

							provider.Save(module, previousLink);
							provider.Save(module, Link);

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
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public void MoveUp(PageModule module, Guid id)
		{
			Link previousLink = null;

			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				IList<Link> Links = provider.List(module);

				foreach (Link Link in Links)
				{
					if (Link.Id == id)
					{
						if (previousLink != null)
						{
							long temp = Link.SortOrder;
							Link.SortOrder = previousLink.SortOrder;
							previousLink.SortOrder = temp;

							provider.Save(module, previousLink);
							provider.Save(module, Link);

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
		public void Save(PageModule module, Link link)
		{
			using (ILinksDataProvider provider = this.DataProviderFactory.CreateProvider<ILinksDataProvider>())
			{
				provider.Save(module, link);
				this.CacheManager.LinksCache().Remove(link.Id);
			}
		}

	}
}
