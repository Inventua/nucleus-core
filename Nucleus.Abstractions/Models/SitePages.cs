using System;
using System.Linq;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents special pages within a site.
/// </summary>
public class SitePages
{
  private Site Site { get; }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="site"></param>
  public SitePages(Site site)
  {
    this.Site = site;
  }

  /// <summary>
  /// If set, represents the Id of the login page.
  /// </summary>
  public Guid? LoginPageId => GetValue(Site.SitePagesKeys.SITEPAGE_LOGIN);

  /// <summary>
  /// If set, represents the Id of the user registration page.
  /// </summary>
  public Guid? UserRegisterPageId => GetValue(Site.SitePagesKeys.SITEPAGE_USERREGISTER);

  /// <summary>
  /// If set, represents the Id of the user profile page.
  /// </summary>
  public Guid? UserProfilePageId => GetValue(Site.SitePagesKeys.SITEPAGE_USERPROFILE);

  /// <summary>
  /// If set, represents the Id of the user change password page.
  /// </summary>
  public Guid? UserChangePasswordPageId => GetValue(Site.SitePagesKeys.SITEPAGE_USERCHANGEPASSWORD);

  /// <summary>
  /// If set, represents the Id of the terms page.
  /// </summary>
  public Guid? TermsPageId => GetValue(Site.SitePagesKeys.SITEPAGE_TERMS);

  /// <summary>
  /// If set, represents the Id of the privacy page.
  /// </summary>
  public Guid? PrivacyPageId => GetValue(Site.SitePagesKeys.SITEPAGE_PRIVACY);

  /// <summary>
  /// If set, represents the Id of the error page.
  /// </summary>
  public Guid? ErrorPageId => GetValue(Site.SitePagesKeys.SITEPAGE_ERROR);

  /// <summary>
  /// If set, represents the Id of the 404-not found page.
  /// </summary>
  public Guid? NotFoundPageId => GetValue(Site.SitePagesKeys.SITEPAGE_NOTFOUND);

  private Guid? GetValue(string key)
  {
    SiteSetting value = Site.SiteSettings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (value != null)
    {
      if (Guid.TryParse(value.SettingValue, out Guid result))
      {
        return result;
      }
    }

    return null;
  }
}
