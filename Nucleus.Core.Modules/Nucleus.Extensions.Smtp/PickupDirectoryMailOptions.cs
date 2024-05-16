using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Mail;
using Nucleus.Extensions;

namespace Nucleus.Extensions.Smtp;

public class PickupDirectoryMailOptions : IMailSettings
{
  public const string Section = "Nucleus:Mail:PickupDirectoryMailOptions";

  private static class PickupDirectoryMailSettingKeys
  {
    public const string PICKUP_FOLDER = "mail:pickupdirectory:folder";
    public const string PICKUP_SENDER = "mail:pickupdirectory:sender";    
  }

  /// <summary>
  /// Mail sender name (email address)
  /// </summary>
  public string Sender { get; set; }

  /// <summary>
  /// Specifies the folder used to store outgoing emails.
  /// </summary>
  public string PickupDirectoryLocation { get; set; }

  /// <summary>
  /// Read settings values from site settings
  /// </summary>
  public void GetSettings(Site site)
  {    
    if (site.SiteSettings.TryGetValue(PickupDirectoryMailSettingKeys.PICKUP_SENDER, out string sender))
    {
      this.Sender = sender;
    }

    if (site.SiteSettings.TryGetValue(PickupDirectoryMailSettingKeys.PICKUP_FOLDER, out string folder))
    {
      this.PickupDirectoryLocation = folder;
    }
  }

  /// <summary>
  /// Set site settings values from this instance
  /// </summary>
  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(PickupDirectoryMailSettingKeys.PICKUP_SENDER, this.Sender);
    site.SiteSettings.TrySetValue(PickupDirectoryMailSettingKeys.PICKUP_FOLDER, this.PickupDirectoryLocation);
  }
}
