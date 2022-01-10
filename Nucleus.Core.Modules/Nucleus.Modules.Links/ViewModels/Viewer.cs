using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Links.ViewModels
{
  public class Viewer
  {
    public List<Link> Links { get; set; }
    public string Layout { get; set; }
    public Boolean NewWindow { get; set; }
    public List CategoryList { get; set; }
  }
}
