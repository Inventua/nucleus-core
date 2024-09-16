using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Search.ViewModels;

public class Settings : Models.Settings
{
  public PageMenu PageMenu { get; set; }

  public Abstractions.Search.ISearchProviderCapabilities SearchProviderCapabilities { get; set; }

  public List<AvailableSearchProvider> SearchProviders { get; set; }

  public class AvailableSearchProvider
  {
    public string Name { get; set; }
    public string ClassName { get; set; }
  }
}
