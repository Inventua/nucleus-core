using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Options;

namespace Nucleus.Core
{
	public class MailClientFactory
	{
		private Context Context { get; }
		private IOptions<SmtpMailOptions> SmtpMailOptions { get; }

		public MailClientFactory(Context context, IOptions<SmtpMailOptions> smtpMailOptions)
		{
			this.Context = context;
			this.SmtpMailOptions = smtpMailOptions;
		}

		/// <summary>
		/// Create a new instance of the <see cref="MailClient"/> class for the site in the current context.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This constructor is for use by code which is running in the context of a Http request.
		/// </remarks>
		public MailClient Create()
		{
			if (this.Context == null)
			{
				throw new InvalidOperationException($"{nameof(Context)} cannot be null");
			}

			if (this.Context.Site == null)
			{
				throw new InvalidOperationException($"{nameof(Context)}.{nameof(Context.Site)} cannot be null");
			}

			return new MailClient(this.Context, this.SmtpMailOptions);
		}

		/// <summary>
		/// Create a new instance of the <see cref="MailClient"/> class for the specifed site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// This constructor is for use by code which is not running in the context of a Http request, such as a scheduled task.
		/// </remarks>

		public MailClient Create(Site site)
		{
			return new MailClient(site, this.SmtpMailOptions);
		}
	}
}
