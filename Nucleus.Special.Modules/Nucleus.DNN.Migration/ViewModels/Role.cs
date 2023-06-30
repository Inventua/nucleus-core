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

public class Role
{
  public int PortalId { get; set; }

  public List<Models.DNN.RoleGroup> RoleGroups { get; set; }
  public List<Models.DNN.Role> Roles { get; set; }

  public List<int> SelectedRoleGroups { get; set; }
  public List<int> SelectedRoles { get; set; }

  public Boolean UpdateExisting { get; set; } = true;
}
