using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Options;

namespace Nucleus.Core.Mail
{
	public class MailClientFactory : IMailClientFactory
	{
		//private Context Context { get; }
		private IOptions<SmtpMailOptions> SmtpMailOptions { get; }

		public MailClientFactory(IOptions<SmtpMailOptions> smtpMailOptions)
		{
			//this.Context = context;
			this.SmtpMailOptions = smtpMailOptions;
		}

		/// <summary>
		/// Create a new instance of the <see cref="MailClient"/> class for the specifed site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>

		public IMailClient Create(Site site)
		{
			return new MailClient(site, this.SmtpMailOptions);
		}
	}
}
