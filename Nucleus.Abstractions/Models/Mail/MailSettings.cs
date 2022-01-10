using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Mail
{
	/// <summary>
	/// Represents settings used to communicate with a SMTP (mail) server.
	/// </summary>
	public class MailSettings
	{
		/// <summary>
		/// Default SMTP server port.
		/// </summary>
		public const int DEFAULT_SMTP_PORT = 587;

		/// <summary>
		/// SMTP (mail) server host name
		/// </summary>
		public string HostName { get; set; }

		/// <summary>
		/// SMTP (mail) server port
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Specifies whether to use SSL for connection to the mail server.
		/// </summary>
		public Boolean UseSsl { get; set; }

		/// <summary>
		/// Mail sender name (email address)
		/// </summary>
		public string Sender { get; set; }

		/// <summary>
		/// SMTP server user name
		/// </summary>
		public string UserName { get; set; }
		/// <summary>
		/// SMTP server password
		/// </summary>
		public string Password { get; set; }
		/// <summary>
		/// When DeliveryMethod is SpecifiedPickupDirectory, specifies the folder used to store outgoing emails.
		/// </summary>
		public string PickupDirectoryLocation { get; set; }
		/// <summary>
		/// Specifies mail delivery method.
		/// </summary>
		public System.Net.Mail.SmtpDeliveryMethod DeliveryMethod { get; set; }
				
	}
}
