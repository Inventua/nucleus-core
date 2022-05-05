using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.ContactUs.Models
{
	public class Settings
	{
		public const string MODULESETTING_CATEGORYLIST_ID = "contactus:categorylist:id";
		public const string MODULESETTING_MAILTEMPLATE_ID = "contactus:mailtemplate:id";

		public const string MODULESETTING_SHOWNAME = "contactus:show-name";
		public const string MODULESETTING_SHOWCOMPANY = "contactus:show-company";
		public const string MODULESETTING_SHOWPHONENUMBER = "contactus:show-phonenumber";
		public const string MODULESETTING_SHOWCATEGORY = "contactus:show-category";
		public const string MODULESETTING_SHOWSUBJECT = "contactus:show-subject";
		public const string MODULESETTING_REQUIRENAME = "contactus:require-name";
		public const string MODULESETTING_REQUIRECOMPANY = "contactus:require-company";
		public const string MODULESETTING_REQUIREPHONENUMBER = "contactus:require-phonenumber";
		public const string MODULESETTING_REQUIRECATEGORY = "contactus:require-category";
		public const string MODULESETTING_REQUIRESUBJECT = "contactus:require-subject";

		public string SendTo { get; set; }

		public List CategoryList { get; set; }

		public Guid MailTemplateId { get; set; }

		public Boolean ShowName { get; set; }

		public Boolean ShowCompany { get; set; }

		public Boolean ShowPhoneNumber { get; set; }

		public Boolean ShowCategory { get; set; }

		public Boolean ShowSubject { get; set; }

		public Boolean RequireName { get; set; }

		public Boolean RequireCompany { get; set; }

		public Boolean RequirePhoneNumber { get; set; }

		public Boolean RequireCategory { get; set; }

		public Boolean RequireSubject { get; set; }

		public void ReadSettings(PageModule module)
		{
			this.MailTemplateId = module.ModuleSettings.Get(Models.Settings.MODULESETTING_MAILTEMPLATE_ID, Guid.Empty);

			this.ShowName = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWNAME, true);
			this.ShowCompany = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWCOMPANY, true);
			this.ShowPhoneNumber = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWPHONENUMBER, true);
			this.ShowCategory = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWCATEGORY, true);
			this.ShowSubject = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWSUBJECT, true);

			this.RequireName = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRENAME, true);
			this.RequireCompany = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRECOMPANY, true);
			this.RequirePhoneNumber = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIREPHONENUMBER, true);
			this.RequireCategory = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRECATEGORY, true);
			this.RequireSubject = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRESUBJECT, true);

		}
	}
}
