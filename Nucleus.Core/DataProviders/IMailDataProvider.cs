using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for the <see cref="MailTemplate"/> class.
	/// </summary>
	internal interface IMailDataProvider : IDisposable
	{
		abstract Task<IEnumerable<MailTemplate>> ListMailTemplates(Site site); 
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate>> ListMailTemplates(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task SaveMailTemplate(Site site, MailTemplate template);
		abstract Task<MailTemplate> GetMailTemplate(Guid templateId);
		abstract Task DeleteMailTemplate(MailTemplate template);
	}
}
