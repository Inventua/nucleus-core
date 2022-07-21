using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="MailTemplate"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IMailTemplateManager
	{
		/// <summary>
		/// Create a new <see cref="MailTemplate"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="MailTemplate"/> is not saved to the database until you call <see cref="Save(Site, MailTemplate)"/>.
		/// </remarks>
		public Task<MailTemplate> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<MailTemplate> Get(Guid id);

		/// <summary>
		/// List all <see cref="MailTemplate"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IEnumerable<MailTemplate>> List(Site site);

		/// <summary>
		/// List paged <see cref="MailTemplate"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update a <see cref="MailTemplate"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="mailTemplate"></param>
		public Task Save(Site site, MailTemplate mailTemplate);

		/// <summary>
		/// Delete the specified <see cref="MailTemplate"/> from the database.
		/// </summary>
		/// <param name="mailTemplate"></param>
		public Task Delete(MailTemplate mailTemplate);

	}
}
