using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Modules.Publish.DataProviders;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Modules.Publish
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Publish"/>s.
	/// </summary>
	public class ArticlesManager
	{

		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }
		private FileSystemManager FileSystemManager { get; }
		private ListManager ListManager { get; }

		public ArticlesManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager, FileSystemManager fileSystemManager, ListManager listManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemManager = fileSystemManager;
			this.ListManager = listManager;

			this.CacheManager.Add<Guid, Article>(new Nucleus.Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="Article"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Article"/> is not saved to the database until you call <see cref="Save(PageModule, Article)"/>.
		/// </remarks>
		public Article CreateNew()
		{
			Article result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Article"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Article Get(Site site, Guid id)
		{
			return this.CacheManager.ArticleCache().Get(id, id =>
			{
				using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
				{
					Article article = provider.Get(id);

					if (article != null)
					{
						if (article.ImageFile != null && article.ImageFile.Id != Guid.Empty)
						{
							try
							{
								article.ImageFile = this.FileSystemManager.GetFile(site, article.ImageFile.Id);
							}
							catch (System.IO.FileNotFoundException)
							{

							}
						}

						ReadArticleAttachments(site, article);
						ReadArticleCategories(article);
					}
					return article;
				}
			});
		}

		private void ReadArticleAttachments(Site site, Article article)
		{
			foreach (Attachment attachment in article.Attachments)
			{
				try
				{
					attachment.File = this.FileSystemManager.GetFile(site, attachment.File.Id);
				}
				catch (System.IO.FileNotFoundException)
				{

				}
			}
		}

		private void ReadArticleCategories(Article article)
		{
			foreach (Category category in article.Categories)
			{
				try
				{
					category.CategoryItem = this.ListManager.GetListItem(category.CategoryItem.Id);
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
		public Article Find(Site site, PageModule module, string title)
		{
			// todo: cache?

			//return this.CacheManager.ArticleCache().Get(id, id =>
			//{
			using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
			{
				Article article = provider.Find(module, title);

				if (article != null)
				{
					if (article.ImageFile != null && article.ImageFile.Id != Guid.Empty)
					{
						try
						{
							article.ImageFile = this.FileSystemManager.GetFile(site, article.ImageFile.Id);
						}
						catch (System.IO.FileNotFoundException)
						{

						}
					}

					ReadArticleAttachments(site, article);
					ReadArticleCategories(article);
				}

				return article;
			}
		}


		/// <summary>
		/// Delete the specifed <see cref="Article"/> from the database.
		/// </summary>
		/// <param name="Article"></param>
		public void Delete(Article article)
		{
			using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
			{
				provider.Delete(article);
				this.CacheManager.ArticleCache().Remove(article.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Article"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Article> List(PageModule module)
		{
			using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
			{
				return provider.List(module);
			}
		}

		/// <summary>
		/// List all <see cref="Article"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public PagedResult<Article> List(Site site, PageModule module, PagingSettings settings)
		{
			using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
			{
				PagedResult<Article> results = provider.List(module, settings);

				foreach (Article article in results.Items)
				{
					try
					{
						if (article.ImageFile.Id != Guid.Empty)
						{
							article.ImageFile = this.FileSystemManager.GetFile(site, article.ImageFile.Id);
						}
					}
					catch (System.IO.FileNotFoundException)
					{

					}

					ReadArticleAttachments(site, article);
					ReadArticleCategories(article);
				}
				return results;
			}
		}

		/// <summary>
		/// Create or update a <see cref="Article"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Article"></param>
		public void Save(PageModule module, Article article)
		{
			using (IArticleDataProvider provider = this.DataProviderFactory.CreateProvider<IArticleDataProvider>())
			{
				provider.Save(module, article);
				this.CacheManager.ArticleCache().Remove(article.Id);
			}
		}

	}
}
