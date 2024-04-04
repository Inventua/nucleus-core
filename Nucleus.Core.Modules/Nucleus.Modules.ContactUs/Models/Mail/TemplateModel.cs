using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Modules.ContactUs.Models.Mail;

[MailTemplateDataModel()]
[System.ComponentModel.DisplayName("Contact Us Notifications")]
public class TemplateModel
{
	public Site Site { get; set; }
	
  public Models.Message Message { get; set; }
	
  public float UserVerificationScore { get; set; }

	public Models.Settings Settings { get; set; }
}
