using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Publish.DataProviders;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Data.Common;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Publish"/>s.
	/// </summary>
	public class ArticlesManager
	{
		private ICacheManager CacheManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IListManager ListManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public ArticlesManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IFileSystemManager fileSystemManager, IListManager listManager)
		{
			this.CacheManager = cacheManager;
			this.FileSystemManager = fileSystemManager;
			this.ListManager = listManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="Article"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Article"/> is not saved to the database until you call <see cref="Save(PageModule, Article)"/>.
		/// </remarks>
		public Task<Article> CreateNew()
		{
			Article result = new();

			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="Article"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Article> Get(Site site, Guid id)
		{
			return await this.CacheManager.ArticleCache().GetAsync(id, async id =>
			{
				using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
				{
					Article article = await provider.Get(id);

					if (article != null)
					{
						if (article.ImageFile != null && article.ImageFile.Id != Guid.Empty)
						{
							try
							{
								article.ImageFile = await this.FileSystemManager.GetFile(site, article.ImageFile.Id);
							}
							catch (System.IO.FileNotFoundException)
							{

							}
						}

						await ReadArticleAttachments(site, article);
						//ReadArticleCategories(article);
					}
					return article;
				}
			});
		}

		/// <summary>
		/// Read file properties from the file system
		/// </summary>
		/// <param name="site"></param>
		/// <param name="article"></param>
		private async Task ReadArticleAttachments(Site site, Article article)
		{
			foreach (Attachment attachment in article.Attachments)
			{
				try
				{
					if (attachment.File != null)
					{ 
						attachment.File = await this.FileSystemManager.GetFile(site, attachment.File.Id);
          }
        }
				catch (System.IO.FileNotFoundException)
				{

				}
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="Article"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Article> Find(Site site, PageModule module, string title)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{
				Guid? articleId = await provider.Find(module, title);

				if (articleId == null)
				{
					return null;
				}
				else
				{
					return await this.Get(site, articleId.Value);
				}

			}
		}

		/// <summary>
		/// Delete the specifed <see cref="Article"/> from the database.
		/// </summary>
		/// <param name="Article"></param>
		public async Task Delete(Article article)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{
				await provider.Delete(article);
				this.CacheManager.ArticleCache().Remove(article.Id);				
			}
		}

		/// <summary>
		/// List all <see cref="Article"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<Article>> List(PageModule module)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{ 
				return await provider.List(module);
			}
		}

		/// <summary>
		/// List all <see cref="Article"/>s for the specified module.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<PagedResult<Article>> List(Site site, PageModule module, PagingSettings settings)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{
				PagedResult<Article> results = await provider.List(module, settings);

				foreach (Article article in results.Items)
				{
					try
					{
						if (article.ImageFile != null && article.ImageFile.Id != Guid.Empty)
						{
							article.ImageFile = await this.FileSystemManager.GetFile(site, article.ImageFile.Id);
						}
					}
					catch (System.IO.FileNotFoundException)
					{

					}

					await ReadArticleAttachments (site, article);
					//ReadArticleCategories(article);
				}
				return results;
			}
		}

		/// <summary>
		/// List all <see cref="Article"/>s for the specified module which match the specified filter.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<PagedResult<Article>> List(Site site, PageModule module, PagingSettings settings, FilterOptions filterOptions)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{
				PagedResult<Article> results = await provider.List(module, settings, filterOptions);

				foreach (Article article in results.Items)
				{
					try
					{
						if (article.ImageFile != null && article.ImageFile.Id != Guid.Empty)
						{
							article.ImageFile = await this.FileSystemManager.GetFile(site, article.ImageFile.Id);
						}
					}
					catch (System.IO.FileNotFoundException)
					{

					}

					await ReadArticleAttachments(site, article);
				}

				return results;
			}
		}

		/// <summary>
		/// Create or update a <see cref="Article"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Article"></param>
		public async Task Save(PageModule module, Article article)
		{
			using (IArticlesDataProvider provider = this.DataProviderFactory.CreateProvider<IArticlesDataProvider>())
			{
				await provider.Save(module, article);
				this.CacheManager.ArticleCache().Remove(article.Id);
			}
		}

	}
}
