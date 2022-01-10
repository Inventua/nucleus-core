using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="MailTemplate"/>s.
	/// </summary>
	public class MailTemplateManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }

		public MailTemplateManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="MailTemplate"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="MailTemplate"/> is not saved to the database until you call <see cref="Save(Site, MailTemplate)"/>.
		/// </remarks>
		public MailTemplate CreateNew()
		{
			return new MailTemplate();
		}

		/// <summary>
		/// Retrieve an existing <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public MailTemplate Get(Guid id)
		{
			return this.CacheManager.MailTemplateCache.Get(id, id =>
			{
				using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
				{
					return provider.GetMailTemplate(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="MailTemplate"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<MailTemplate> List(Site site)
		{
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				return provider.ListMailTemplates(site);
			}
		}

		/// <summary>
		/// Create or update a <see cref="MailTemplate"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="mailTemplate"></param>
		public void Save(Site site, MailTemplate mailTemplate)
		{			
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				provider.SaveMailTemplate(site, mailTemplate);
			}
			
			this.CacheManager.MailTemplateCache.Remove(mailTemplate.Id);			
		}

		/// <summary>
		/// Delete the specified <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="mailTemplate"></param>
		public void Delete(MailTemplate mailTemplate)
		{
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				provider.DeleteMailTemplate(mailTemplate);
				this.CacheManager.MailTemplateCache.Remove(mailTemplate.Id);
			}
		}

	}
}
