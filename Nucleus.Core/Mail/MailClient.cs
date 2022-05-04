using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Extensions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using MailKit;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Razor;

namespace Nucleus.Core.Mail
{
	/// <summary>
	/// Allows Nucleus core and extensions to sent email using SMTP.
	/// </summary>
	public class MailClient : IMailClient, IDisposable
	{
		private ILogger<MailClient> Logger { get; }

		public MailSettings MailSettings { get; private set; }
		
		public MailClient(Site site, IOptions<SmtpMailOptions> smtpMailOptions, ILogger<MailClient> logger)
		{
			if (site == null)
			{
				throw new InvalidOperationException($"{nameof(site)} cannot be null");
			}

			// Apply site settings
			this.MailSettings = site.GetSiteMailSettings(smtpMailOptions.Value);
			
			this.Logger = logger;
		}

		public void Send(MailTemplate template, MailArgs args, string to)
		{
			System.Dynamic.ExpandoObject model = new();

			foreach (var value in args)
			{
				model.TryAdd(value.Key, value.Value);
			}

			Send(template, model, to);			
		}

		public void Send(MailTemplate template, Object model, string to) 
		{
			Send<MailRazorTemplateBase>(template, model, to);
		}

		/// <summary>
		/// Parse the specified template, and send the resulting email to the specified 'to' address. The 'to' address can be a list of email addresses
		/// separated by commas or semicolons.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="args"></param>
		/// <param name="to"></param>
		public void Send<TRazorTemplateBase>(MailTemplate template, Object model, string to)
			where TRazorTemplateBase : MailRazorTemplateBase
		{
			MimeKit.MimeMessage message = new();
			MimeKit.BodyBuilder builder = new();
			
			message.From.Add(MimeKit.MailboxAddress.Parse(this.MailSettings.Sender));

			foreach (string address in to.Split(new char[] { ',' , ';' }, StringSplitOptions.RemoveEmptyEntries))
      {
				message.To.Add(MimeKit.MailboxAddress.Parse(address));
			}
			
			message.Subject = template.Subject.ParseTemplate<TRazorTemplateBase>(model);// ParseTemplate<TRazorTemplateBase>(template.Subject, model);
			builder.HtmlBody = template.Body.ParseTemplate<TRazorTemplateBase>(model); //ParseTemplate<TRazorTemplateBase>(template.Body, model);
			
			message.Body = builder.ToMessageBody();

			if (this.MailSettings.DeliveryMethod == System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory)
			{
				message.WriteTo(System.IO.Path.Combine(this.MailSettings.PickupDirectoryLocation, $"{Guid.NewGuid()}.eml"));
			}
			else
			{
				using (MailKit.Net.Smtp.SmtpClient client = new())
				{
					client.Connect(this.MailSettings.HostName, this.MailSettings.Port, MailKit.Security.SecureSocketOptions.Auto);
					client.Authenticate(this.MailSettings.UserName, this.MailSettings.Password);

					client.Send(message);

					client.Disconnect(true);
				}
			}
		}


		public void Dispose()
		{
			this.MailSettings = null;
			GC.SuppressFinalize(this);
		}

	}
}
