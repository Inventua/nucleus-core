using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Abstractions.Mail
{
  ///// <summary>
  ///// 
  ///// </summary>
  ///// <typeparam name="T"></typeparam>
  //public abstract class MailSettings<T> : MailSettings
  //  where T : MailSettings, new()
  //{
  //  /// <summary>
  //  /// 
  //  /// </summary>
  //  public T Settings { get; set; } = new();

  //  /// <summary>
  //  /// Write settings  from <paramref name="newSettings"/> into the specified <paramref name="site"/> settings.
  //  /// </summary>
  //  /// <param name="site"></param>
  //  /// <param name="newSettings"></param>
  //  public abstract void SetSettings(Site site, MailSettings<T> newSettings);

  //  ///// <summary>
  //  ///// Read settings from <paramref name="site"/> settings into this instance.
  //  ///// </summary>
  //  ///// <param name="site"></param>
  //  //public override void ReadSettings(Site site) 
  //  //{
  //  //}

  //  ///// <summary>
  //  ///// Write settings  from <paramref name="newSettings"/> into the specified <paramref name="site"/> settings.
  //  ///// </summary>
  //  ///// <param name="site"></param>
  //  ///// <param name="newSettings"></param>
  //  //public virtual void SetSettings(Site site, T newSettings) 
  //  //{
  //  //  TrySetValue(site.SiteSettings, Site.SiteMailSettingKeys.MAIL_CLIENT_TYPENAME, this.Settings.DefaultMailClientTypeName);
  //  //  TrySetValue(site.SiteSettings, Site.SiteMailSettingKeys.MAIL_SENDER, this.Settings.Sender);
  //  //}

  //  //private static Boolean TryGetValue(List<SiteSetting> settings, string key, out string result)
  //  //{
  //  //  SiteSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
  //  //  if (value != null)
  //  //  {
  //  //    result = value.SettingValue;
  //  //    return true;
  //  //  }
  //  //  else
  //  //  {
  //  //    result = null;
  //  //    return false;
  //  //  }
  //  //}

  //  //private static void TrySetValue(List<SiteSetting> settings, string key, string value)
  //  //{
  //  //  SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
  //  //  if (existing != null)
  //  //  {
  //  //    existing.SettingValue = value;
  //  //  }
  //  //  else
  //  //  {
  //  //    settings.Add(new SiteSetting() { SettingName = key, SettingValue = value });
  //  //  }
  //  //}

  //}

  /// <summary>
  /// Represents settings used to communicate with a (mail) server.
  /// </summary>
  public interface IMailSettings
  {
    

    ///// <summary>
    ///// Read settings from <paramref name="site"/> settings into the <paramref name="site"/> settings.
    ///// </summary>
    ///// <param name="site"></param>
    ////public abstract void ReadSettings(Site site);

  }
}
