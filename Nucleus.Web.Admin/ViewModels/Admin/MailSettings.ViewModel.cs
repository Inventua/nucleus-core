using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nucleus.Abstractions.Models;
using static Nucleus.Web.ViewModels.Admin.ScheduledTaskEditor;

namespace Nucleus.Web.ViewModels.Admin;

public class MailSettings
{  
  public string DefaultMailClientTypeName { get; set; }

  public string SendTestMailTo {  get; set; }

  public IEnumerable<ServiceType> AvailableMailClientTypes { get; set; }

  public Abstractions.Mail.IMailSettings Settings { get; set; }

  public string SettingsPath { get; set; }

  public ViewDataDictionary SettingsViewData { get; set; }
}
