using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Nucleus.Core;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Nucleus.Modules.Documents.DataProviders;
using Nucleus.Modules.Documents.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Modules.Documents
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Document"/>s.
	/// </summary>
	public class DocumentsManager
	{

		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }
		private FileSystemManager FileSystemManager { get; }

		public DocumentsManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager, FileSystemManager fileSystemManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemManager = fileSystemManager;

			// todo: get cacheoption settings from config
			this.CacheManager.Add<Guid, Document>(new Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="Document"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Document"/> is not saved to the database until you call <see cref="Save(Site, Document)"/>.
		/// </remarks>
		public Document CreateNew()
		{
			Document result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Document"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Document Get(Site site, Guid id)
		{
			return this.CacheManager.DocumentCache().Get(id, id =>
			{
				using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
				{
					Document document = provider.Get(id);

					if (document != null)
					{
						try
						{
							document.File = this.FileSystemManager.GetFile(site, document.File.Id);
						}
						catch (System.IO.FileNotFoundException)
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
		public void Delete(Document Document)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				provider.Delete(Document);
				this.CacheManager.DocumentCache().Remove(Document.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Document"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Document> List(Site site, PageModule module)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IList<Document> documents = provider.List(module);

				foreach (Document document in documents)
				{
					if (document != null)
					{
						try
						{
							document.File = this.FileSystemManager.GetFile(site, document.File.Id);
						}
						catch (System.IO.FileNotFoundException)
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
		public void MoveDown(PageModule module, Guid id)
		{
			Document previousDocument = null;

			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IEnumerable<Document> documents = provider.List(module).Reverse();

				foreach (Document document in documents)
				{
					if (document.Id == id)
					{
						if (previousDocument != null)
						{
							long temp = document.SortOrder;
							document.SortOrder = previousDocument.SortOrder;
							previousDocument.SortOrder = temp;

							provider.Save(module, previousDocument);
							provider.Save(module, document);

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
		public void MoveUp(PageModule module, Guid id)
		{
			Document previousDocument = null;

			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				IList<Document> documents = provider.List(module);

				foreach (Document document in documents)
				{
					if (document.Id == id)
					{
						if (previousDocument != null)
						{
							long temp = document.SortOrder;
							document.SortOrder = previousDocument.SortOrder;
							previousDocument.SortOrder = temp;

							provider.Save(module, previousDocument);
							provider.Save(module, document);

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
		public void Save(PageModule module, Document Document)
		{
			using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
			{
				provider.Save(module, Document);
				this.CacheManager.DocumentCache().Remove(Document.Id);
			}
		}

	}
}
