using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Extensions;

namespace Nucleus.OAuth.ViewModels
{
	public class Settings
	{
		internal const string SETTING_MATCH_BY_NAME = "oauth:match-by-name";
		internal const string SETTING_MATCH_BY_EMAIL = "oauth:match-by-email";
		internal const string SETTING_CREATE_USERS = "oauth:create-users";
		internal const string SETTING_AUTO_VERIFY = "oauth:auto-verify";
		internal const string SETTING_AUTO_APPROVE = "oauth:auto-approve";

		public Boolean MatchByName { get; set; }
		public Boolean MatchByEmail { get; set; }

		public Boolean CreateUsers { get; set; }
		public Boolean AutomaticallyApproveNewUsers { get; set; }
		public Boolean AutomaticallyVerifyNewUsers { get; set; }

		public void ReadSettings(Site site)
		{
			if (site.SiteSettings.TryGetValue(SETTING_MATCH_BY_NAME, out Boolean matchByName))
			{
				this.MatchByName = matchByName;
			}

			if (site.SiteSettings.TryGetValue(SETTING_MATCH_BY_EMAIL, out Boolean matchByEmail))
			{
				this.MatchByEmail = matchByEmail;
			}

			if (site.SiteSettings.TryGetValue(SETTING_CREATE_USERS, out Boolean createUsers))
			{
				this.CreateUsers = createUsers;
			}

			if (site.SiteSettings.TryGetValue(SETTING_AUTO_VERIFY, out Boolean automaticallyVerifyNewUsers))
			{
				this.AutomaticallyVerifyNewUsers = automaticallyVerifyNewUsers;
			}

			if (site.SiteSettings.TryGetValue(SETTING_AUTO_APPROVE, out Boolean automaticallyApproveNewUsers))
			{
				this.AutomaticallyApproveNewUsers = automaticallyApproveNewUsers;
			}

		}
	}
}
