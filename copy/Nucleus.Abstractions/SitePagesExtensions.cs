using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Abstractions
{
	public static class SitePagesExtensions
	{
		/// <summary>
		/// Sets <see cref="Site.SiteSettings"/> based on a <see cref="SitePages"/> object.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="sitePages"></param>
		public static void SetSitePages(this Site site, SitePages sitePages)
		{
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_ERROR, sitePages.ErrorPageId);
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_LOGIN, sitePages.LoginPageId);
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_NOTFOUND, sitePages.NotFoundPageId);

			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_PRIVACY, sitePages.PrivacyPageId);
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_USERREGISTER, sitePages.UserRegisterPageId);
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_TERMS, sitePages.TermsPageId);
			site.SiteSettings.TrySetValue(Site.SitePagesKeys.SITEPAGE_USERPROFILE, sitePages.UserProfilePageId);
		}
		
		/// <summary>
		/// Populate and return a <see cref="SitePages"/> object from <see cref="Site.SiteSettings"/>
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public static SitePages GetSitePages(this Site site)
		{
			SitePages result = new();
			Guid id;

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_ERROR), out id))
			{
				result.ErrorPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_LOGIN), out id))
			{
				result.LoginPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_NOTFOUND), out id))
			{
				result.NotFoundPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_PRIVACY), out id))
			{
				result.PrivacyPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_USERREGISTER), out id))
			{
				result.UserRegisterPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_TERMS), out id))
			{
				result.TermsPageId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SitePagesKeys.SITEPAGE_USERPROFILE), out id))
			{
				result.UserProfilePageId = id;
			}

			return result;
		}


		
	}
}
