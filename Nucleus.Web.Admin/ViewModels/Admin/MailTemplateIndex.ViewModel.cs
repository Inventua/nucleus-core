using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class MailTemplateIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<MailTemplate> MailTemplates { get; set; } = new() { PageSize = 20 };
    
	}
}
