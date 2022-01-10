using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core.Managers
{
	public class ContentManager : IContentManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public ContentManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.DataProviderFactory = dataProviderFactory;
			this.CacheManager = cacheManager;
		}

		public async Task<List<Content>> List(PageModule pageModule)
		{
			using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
			{
				return await provider.ListContent(pageModule);
			}
		}

		public async Task<Content> Get(Guid id)
		{
			return await this.CacheManager.ContentCache().GetAsync(id, async id =>
			{
				using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
				{
					return await provider.GetContent(id);
				}
			});
		}

		public async Task Save (PageModule pageModule, Content content)
		{
			using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
			{				
				await provider.SaveContent(pageModule, content);
				this.CacheManager.ContentCache().Remove(content.Id);
			}
		}

		public async Task Delete(Content content)
		{
			using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
			{
				await provider.DeleteContent(content);
				this.CacheManager.ContentCache().Remove(content.Id);
			}
		}

		public async Task MoveDown(PageModule module, Guid contentId)
		{
			Content previousContent = null;
			
			using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
			{
				List<Content> siblingContents = await provider.ListContent(module);

				siblingContents.Reverse();
				foreach (Content content in siblingContents)
				{
					if (content.Id == contentId)
					{
						if (previousContent != null)
						{
							int temp = content.SortOrder;
							content.SortOrder = previousContent.SortOrder;
							previousContent.SortOrder = temp;

							if (previousContent.SortOrder == content.SortOrder)
							{
								content.SortOrder += 10;
							}

							await provider.SaveContent(module, previousContent);
							await provider.SaveContent(module, content);

							this.CacheManager.ContentCache().Remove(previousContent.Id);
							this.CacheManager.ContentCache().Remove(content.Id);
							break;
						}
					}
					else
					{
						previousContent = content;
					}
				}
			}
		}

		public async Task MoveUp(PageModule module, Guid contentId)
		{
			Content previousContent = null;
			
			using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
			{
				List<Content> siblingContents = await provider .ListContent(module);
				
				foreach (Content content in siblingContents)
				{
					if (content.Id == contentId)
					{
						if (previousContent != null)
						{
							int temp = content.SortOrder;
							content.SortOrder = previousContent.SortOrder;
							previousContent.SortOrder = temp;

							if (previousContent.SortOrder == content.SortOrder)
							{
								previousContent.SortOrder += 10;
							}

							await provider .SaveContent(module, previousContent);
							await provider.SaveContent(module, content);

							this.CacheManager.ContentCache().Remove(previousContent.Id);
							this.CacheManager.ContentCache().Remove(content.Id);
							break;
						}
					}
					else
					{
						previousContent = content;
					}
				}
			}
		}
	}
}
