using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.PageLinks.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.PageLinks.ViewModels
{
	public class Viewer : Models.Settings
	{
    public string EnabledHeaders { get; set; }
    public IEnumerable<PageLink> PageLinks { get; set; } = [];

    public string DirectionClass
    {
      get 
      {
        switch (Direction)
        {
          case Directions.Horizontal:
            return "page-links-horizontal";
          case Directions.Grid:
            return "page-links-grid";
          default:
            return "";
        }
      }
    }
  }
}
