using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using System.Collections.Generic;

namespace Nucleus.Modules.ContactUs.ViewModels;

public class Settings : Models.Settings
{
	public const string DUMMY_PASSWORD = "!@#NOT_CHANGED^&*";

	public IEnumerable<List> Lists { get; set; }
	
	public IEnumerable<MailTemplate> MailTemplates { get; set; }

	public string RecaptchaSecretKey { get; set; }

}
