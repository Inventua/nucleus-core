using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.ViewModels
{
  public class Editor
  {
    public Link Link { get; set; }
    public string Message { get; set; }

    public List CategoryList { get; set; }
    public IList<Page> Pages { get; set; }

    public Dictionary<string, string> LinkTypes { get; set; }
  }
}
