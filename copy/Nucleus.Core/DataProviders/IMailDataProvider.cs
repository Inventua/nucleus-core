using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for the <see cref="MailTemplate"/> class.
	/// </summary>
	internal interface IMailDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract IEnumerable<MailTemplate> ListMailTemplates(Site site);
		abstract void SaveMailTemplate(Site site, MailTemplate template);
		abstract MailTemplate GetMailTemplate(Guid templateId);
		abstract void DeleteMailTemplate(MailTemplate template);
	}
}
