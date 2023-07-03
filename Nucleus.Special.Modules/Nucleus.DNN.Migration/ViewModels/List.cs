using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.ViewModels;

public class List
{
  public int PortalId { get; set; }

  public List<Models.DNN.List> Lists { get; set; }
  public Boolean UpdateExisting { get; set; } = true;

}
