using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="MailTemplate"/>s.
	/// </summary>
	public class MailTemplateManager : IMailTemplateManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public MailTemplateManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
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
		public Task<MailTemplate> CreateNew()
		{
			return Task.FromResult(new MailTemplate());
		}

		/// <summary>
		/// Retrieve an existing <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<MailTemplate> Get(Guid id)
		{
			return await this.CacheManager.MailTemplateCache().GetAsync(id, async id =>
			{
				using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
				{
					return await provider.GetMailTemplate(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="MailTemplate"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IEnumerable<MailTemplate>> List(Site site)
		{
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				return await provider.ListMailTemplates(site);
			}
		}

    /// <summary>
		/// List <see cref="MailTemplate"/>s for the specified data model type for the specified site.
		/// </summary>
		/// <param name="site"></param>
    /// <param name="type"></param>
		/// <returns></returns>
		public async Task<IEnumerable<MailTemplate>> List(Site site, System.Type type)
    {
      IEnumerable<MailTemplate> templates = await this.List(site);
      return templates.Where(template => String.IsNullOrEmpty(template.DataModelTypeName) || template.DataModelTypeName == $"{type.FullName},{type.Assembly.GetName().Name}");
    }

    /// <summary>
    /// List paged <see cref="MailTemplate"/>s for the specified site.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				return await provider.ListMailTemplates(site, pagingSettings);
			}
		}

		/// <summary>
		/// Create or update a <see cref="MailTemplate"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="mailTemplate"></param>
		public async Task Save(Site site, MailTemplate mailTemplate)
		{			
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				await provider.SaveMailTemplate(site, mailTemplate);
			}
			
			this.CacheManager.MailTemplateCache().Remove(mailTemplate.Id);			
		}

		/// <summary>
		/// Delete the specified <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="mailTemplate"></param>
		public async Task Delete(MailTemplate mailTemplate)
		{
			using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
			{
				await provider.DeleteMailTemplate(mailTemplate);
				this.CacheManager.MailTemplateCache().Remove(mailTemplate.Id);
			}
		}

    /// <summary>
		/// Returns a list of available mail template data model classes.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<System.Type>> ListTemplateDataModelTypes()
    {
      return Task.FromResult(Plugins.AssemblyLoader.GetTypesWithAttribute<MailTemplateDataModelAttribute>());
    }
  }
}
