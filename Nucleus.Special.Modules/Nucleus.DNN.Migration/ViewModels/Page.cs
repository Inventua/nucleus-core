using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.ViewModels;

public class Page
{
  public int PortalId { get; set; }

  public List<Models.DNN.Page> Pages { get; set; }

  public Boolean UpdateExisting { get; set; } = true;

  public List<Models.DNN.Skin> DNNSkins { get; set; }
  public IList<Nucleus.Abstractions.Models.LayoutDefinition > Layouts { get; set; }

  public List<Models.DNN.Container> DNNContainers { get; set; }
  public IList<Nucleus.Abstractions.Models.ContainerDefinition> Containers { get; set; }
}
