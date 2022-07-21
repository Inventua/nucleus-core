using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Nucleus.Abstractions.Mail
{
	/// <summary>
	/// Allows Nucleus core and extensions to sent email using SMTP.
	/// </summary>
	/// <remarks>
	/// Use <see cref="IMailClientFactory.Create(Site)"/> to create an instance of this class.
	/// </remarks>
	public interface IMailClient : IDisposable
	{		
		/// <summary>
		/// Parse the specified template, and send the resulting email to the specified to address.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="model"></param>
		/// <param name="to"></param>
		public Task Send<TModel>(MailTemplate template, TModel model, string to)
			where TModel : class;
	}
}
