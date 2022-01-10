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
  public class Settings
  {
    //public Guid ModuleId { get; set; }
    public IList<Link> Links { get; set; }
    //public string Message { get; set; }
    public Boolean NewWindow { get; set; }
    public IList<List> Lists { get; set; }

    public List CategoryList { get; set; }

    public List<string> Layouts { get; set; }
    public string Layout { get; set; }
  }
}
