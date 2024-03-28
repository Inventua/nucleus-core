using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin;

public class ContainerSelector
{
  public IEnumerable<ContainerInformation> Containers { get; set; }

  public Guid? SelectedContainerId { get; set; }

  public class ContainerInformation : ContainerDefinition
  {
    public string Extension { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Description { get; set; }
  }
}
