using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.ContactUs.ViewModels
{
	public class Settings : Models.Settings
	{
		public IEnumerable<List> Lists { get; set; }
		
		public IEnumerable<MailTemplate> MailTemplates { get; set; }



	}
}
