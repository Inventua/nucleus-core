using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class SiteEditor 
	{		
		public Boolean IsCurrentSiteEditor { get; set; }

		public Boolean IsCurrentSite { get; set; }
		public Site Site { get; set; }

		public Boolean EditHomeDirectory { get; set; }

		/// These properties are used to set the Site.
		public Boolean AllowPublicRegistration { get;set; }
		public Boolean RequireEmailVerification { get; set; }
		public Boolean RequireApproval { get; set; }


		public IEnumerable<LayoutDefinition> Layouts { get; set; }
		public IEnumerable<ContainerDefinition> Containers { get; set; }
		public IEnumerable<Role> Roles { get; set; }
		public IEnumerable<MailTemplate> MailTemplates { get; set; }
		public SiteAlias Alias { get; set; }
		public UserProfileProperty Property { get; set; }

		public List<SiteGroup> SiteGroups { get; set; }

		public MailSettings MailSettings { get; set; }
		public SiteTemplateSelections SiteTemplateSelections { get; set; }
		public SitePages SitePages { get; set; }
		public PageMenu PageMenu { get; set; }

		public IEnumerable<Page> Pages { get; set; }

		// favicon
		public File SelectedIconFile { get; set; }
		public File SelectedLogoFile { get; set; }
		public File SelectedCssFile { get; set; }

	}
}
