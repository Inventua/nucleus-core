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
		public const int DEFAULT_SMTP_PORT = 587;

		public string HostName { get; set; }
		public int Port { get; set; }
		public Boolean UseSsl { get; set; }

		public string Sender { get; set; }

		public string UserName { get; set; }
		public string Password { get; set; }
		public string PickupDirectoryLocation { get; set; }
		public System.Net.Mail.SmtpDeliveryMethod DeliveryMethod { get; set; }
				
	}
}
