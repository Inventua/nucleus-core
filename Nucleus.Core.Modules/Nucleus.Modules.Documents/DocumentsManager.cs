using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Documents.DataProviders;
using Nucleus.Modules.Documents.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Data.Common;

namespace Nucleus.Modules.Documents
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Document"/>s.
	/// </summary>
	public class DocumentsManager
	{

		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public DocumentsManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IFileSystemManager fileSystemManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemManager = fileSystemManager;

		}

		/// <summary>
		/// Create a new <see cref="Document"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Document"/> is not saved to the database until you call <see cref="Save(Site, Document)"/>.
		/// </remarks>
		public Task<Document> CreateNew()
		{
			Document result = new();

			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="Document"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Document> Get(Site site, Guid id)
		{
			return await this.CacheManager.DocumentCache().GetAsync(id, async id =>
			{
				using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
				{
					Document document = await provider.Get(id);

					if (document != null)
					{
						try
						{
							document.File = await this.FileSystemManager.GetFile(site, document.File.Id);
						}
						catch (Exception)
						{
							document.File = null;
						}
					}

					return document;
				}
			});
		}

		/// <summary>
		/// Delete the specifed <see cref="Document"/> from the database.
		/// </summary>
		/// <param name="Document"></param>
		public async Task Delete(Document Document)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				await provider.Delete(Document);
				this.CacheManager.DocumentCache().Remove(Document.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Document"/>s for the specified module.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="module"></param>		
		/// <returns></returns>
		public async Task<IList<Document>> List(Site site, PageModule module)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IList<Document> documents = await provider.List(module);

				foreach (Document document in documents)
				{
					if (document != null)
					{
						try
						{
							document.File = await this.FileSystemManager.GetFile(site, document.File.Id);
						}
						catch (Exception)
						{
							document.File = null;
						}
					}
				}

				return documents;
			}
		}

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public async Task MoveDown(PageModule module, Guid id)
		{
			Document previousDocument = null;

			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IEnumerable<Document> documents = (await provider.List(module)).Reverse();

				foreach (Document document in documents)
				{
					if (document.Id == id)
					{
						if (previousDocument != null)
						{
							int temp = document.SortOrder;
							document.SortOrder = previousDocument.SortOrder;
							previousDocument.SortOrder = temp;

							await provider .Save(module, previousDocument);
							await provider .Save(module, document);

							this.CacheManager.DocumentCache().Remove(id);
							this.CacheManager.DocumentCache().Remove(previousDocument.Id);

							break;
						}
					}
					else
					{
						previousDocument = document;
					}
				}
			}
		}

		/// <summary>
		/// Update the <see cref="PageModule.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="PageModule.SortOrder"/>.
		/// </summary>
		/// <param name="id"></param>
		public async Task MoveUp(PageModule module, Guid id)
		{
			Document previousDocument = null;

			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IList<Document> documents = await provider.List(module);

				foreach (Document document in documents)
				{
					if (document.Id == id)
					{
						if (previousDocument != null)
						{
							int temp = document.SortOrder;
							document.SortOrder = previousDocument.SortOrder;
							previousDocument.SortOrder = temp;

							await provider.Save(module, previousDocument);
							await provider.Save(module, document);

							this.CacheManager.DocumentCache().Remove(id);
							this.CacheManager.DocumentCache().Remove(previousDocument.Id);
							break;
						}
					}
					else
					{
						previousDocument = document;
					}
				}
			}
		}

		/// <summary>
		/// Create or update a <see cref="Document"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Document"></param>
		public async Task Save(PageModule module, Document Document)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				await provider.Save(module, Document);
				this.CacheManager.DocumentCache().Remove(Document.Id);
			}
		}

	}
}
