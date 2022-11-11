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

namespace Nucleus.SAML.Client.ViewModels
{
	public class SiteClientSettings
	{
		internal const string SETTING_MATCH_BY_NAME = "saml:match-by-name";
		internal const string SETTING_MATCH_BY_EMAIL = "saml:match-by-email";

		internal const string SETTING_CREATE_USERS = "saml:create-users";
		internal const string SETTING_AUTO_VERIFY = "saml:auto-verify";
		internal const string SETTING_AUTO_APPROVE = "saml:auto-approve";

		internal const string SETTING_SYNC_ROLES = "saml:sync-roles";
		internal const string SETTING_ADD_ROLES = "saml:sync-roles-add";
		internal const string SETTING_REMOVE_ROLES = "saml:sync-roles-remove";

		internal const string SETTING_SYNC_PROFILE = "saml:sync-profile";

		public Boolean MatchByName { get; set; }=true;
		public Boolean MatchByEmail { get; set; }

		public Boolean CreateUsers { get; set; }
		public Boolean AutomaticallyApproveNewUsers { get; set; } = true;
		public Boolean AutomaticallyVerifyNewUsers { get; set; } = true;

		public Boolean SynchronizeRoles { get; set; }
		public Boolean AddToRoles { get; set; }
		public Boolean RemoveFromRoles { get; set; }

		public Boolean SynchronizeProfile { get; set; }


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

			if (site.SiteSettings.TryGetValue(SETTING_SYNC_ROLES, out Boolean syncRoles))
			{
				this.SynchronizeRoles = syncRoles;
			}

			if (site.SiteSettings.TryGetValue(SETTING_ADD_ROLES, out Boolean addRoles))
			{
				this.AddToRoles = addRoles;
			}

			if (site.SiteSettings.TryGetValue(SETTING_REMOVE_ROLES, out Boolean removeRoles))
			{
				this.RemoveFromRoles = removeRoles;
			}

			if (site.SiteSettings.TryGetValue(SETTING_SYNC_PROFILE, out Boolean syncProfile))
			{
				this.SynchronizeProfile = syncProfile;
			}

		}
	}
}
