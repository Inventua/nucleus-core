using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;

namespace Nucleus.Modules.Search.ViewModels;

public class Viewer : Models.Settings
{
  public Guid ModuleId { get; set; }
  public string SearchTerm { get; set; }
  public Nucleus.Abstractions.Models.Paging.PagingSettings PagingSettings { get; set; } = new();

  public SearchResults SearchResults { get; set; }

  public string ResultsUrl { get; set; }
}
