using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin;

public class LayoutSelector
{
  public IEnumerable<LayoutInformation> Layouts { get; set; }

  public Guid? SelectedLayoutId { get; set; }

  public class LayoutInformation : LayoutDefinition
  {
    public string Extension { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Description { get; set; }
  }
}
