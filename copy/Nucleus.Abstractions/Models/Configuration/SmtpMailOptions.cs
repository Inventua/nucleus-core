using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents the default SMTP options from configuration files.
	/// </summary>
	public class SmtpMailOptions : Nucleus.Abstractions.Models.Mail.MailSettings
	{
		public const string Section = "Nucleus:SmtpMailOptions";

	}
}
