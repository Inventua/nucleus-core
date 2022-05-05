using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Nucleus.Core.Mail
{
	public class MailClientFactory : IMailClientFactory
	{
		//private Context Context { get; }
		private IOptions<SmtpMailOptions> SmtpMailOptions { get; }
		private ILogger<MailClient> Logger { get; }

		public MailClientFactory(IOptions<SmtpMailOptions> smtpMailOptions, ILogger<MailClient> logger)
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
			return new MailClient(site, this.SmtpMailOptions, this.Logger);
		}
	}
}
