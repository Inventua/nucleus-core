using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Client.ViewModels
{
	public class Settings 
	{
		public const string MODULESETTING_CAPTION = "saml:viewer:caption";
		public const string MODULESETTING_LAYOUT = "saml:viewer:layout";
		public const string MODULESETTING_AUTOLOGIN = "saml:viewer:autologin";

		public string Layout { get; set; }
		public List<string> Layouts { get; set; }

		public Boolean AutoLogin { get; set; }
		public string Caption { get; set; }


		public void ReadSettings(PageModule module)
		{
			this.Caption = module.ModuleSettings.Get(MODULESETTING_CAPTION, "Log in with");
			this.AutoLogin = module.ModuleSettings.Get(MODULESETTING_AUTOLOGIN, false);
			this.Layout = module.ModuleSettings.Get(MODULESETTING_LAYOUT, "List");
		}
	}
}
