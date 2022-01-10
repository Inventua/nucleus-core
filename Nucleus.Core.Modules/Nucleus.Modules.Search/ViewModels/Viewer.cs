using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;

namespace Nucleus.Modules.Search.ViewModels
{
  public class Viewer : SearchQuery
  {
    public SearchResults SearchResults { get; set; }
    public Settings Settings { get; set; } = new();
    
  }
}
