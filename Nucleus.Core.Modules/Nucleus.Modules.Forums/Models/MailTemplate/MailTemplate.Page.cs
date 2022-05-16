using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models.MailTemplate
{
	public class Page : Nucleus.Abstractions.Models.Page
	{
		public string ManageSubscriptionsRelativeUrl { get; set; }

		private Page()
		{

		}

		public static Page Create(Nucleus.Abstractions.Models.Page page, string manageSubscriptionsRelativeUrl)
		{
			Page copiedPage = new();

			page.CopyTo(copiedPage);
			copiedPage.ManageSubscriptionsRelativeUrl = manageSubscriptionsRelativeUrl;
			copiedPage.Modules = new();
			copiedPage.Permissions = new();

			return copiedPage;
		}
	}
}
