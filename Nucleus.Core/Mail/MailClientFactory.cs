using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Extensions;

namespace Nucleus.Core.Mail
{
	public class MailClientFactory : IMailClientFactory
	{
    private IEnumerable<IMailClient> MailClients { get; }

    public MailClientFactory(IEnumerable<IMailClient> mailClients) 
    {
      this.MailClients = mailClients;
    }

		/// <summary>
		/// Create a new instance of the <see cref="IMailClient"/> which is configured for the specifed site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>

		public IMailClient Create(Site site)
		{      
      IMailClient mailClient = this.MailClients
        .Where(client => client.GetType().ShortTypeName() == site.GetMailClientType())
        .FirstOrDefault();

      if (mailClient == null)
      {
        throw new InvalidOperationException("No mail client is configured for this site.");
      }
        
      mailClient.Site = site;
      return mailClient;
		}
	}
}
