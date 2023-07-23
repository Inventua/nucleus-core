using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Shadow.ViewModels;
public class Settings : Models.Settings
{
  public PageMenu PageMenu { get; set; }
  public List<PageModuleInfo> Modules { get; set; } = new();

  public class PageModuleInfo
  {
    public string Name { get; set; } 
    public Guid Id { get; set; }

    public PageModuleInfo(Guid id, string name)
    {
      this.Id = id;
      this.Name = name;
    }
  }
}
