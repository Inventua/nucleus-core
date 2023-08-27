using static Nucleus.Web.ViewModels.Admin.Extensions;
using System.Collections.Generic;
using System;

namespace Nucleus.Web.ViewModels.Admin;

public class ExtensionsUsage
{
  public Guid Id { get; set; }
  public Abstractions.Models.Extensions.package Package { get; set; }
  public ExtensionComponents ExtensionComponents { get; set; } = new();

  public string SiteDefaultLayout { get; set; }
  public string SiteDefaultContainer { get; set; }

}
