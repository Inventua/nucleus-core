using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.DNN.Migration.ViewModels;

public class Folder
{
  public int PortalId { get; set; }
  
  public List<Models.DNN.Folder> Folders { get; set; }

  public Boolean UpdateExisting { get; set; } = true;

  public List<Models.DNN.PortalAlias> AvailablePortalAliases { get; set; }

  public Boolean UseSSL { get; set; } = true;

  [Required(ErrorMessage = "Please select a portal alias.")]
  public int? PortalAliasId { get; set; }

}
