using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class RoleGroupMigration : IMigrationEngine<Models.DNN.RoleGroup>
{
  public Task Migrate(List<RoleGroup> items)
  {
    foreach (RoleGroup roleGroup in items.Where(roleGroup => roleGroup.CanSelect && roleGroup.IsSelected))
    {

    }

    return Task.CompletedTask;
  }

  public Task Validate(List<RoleGroup> items)
  {
    foreach (RoleGroup roleGroup in items)
    {      
      if (roleGroup.RoleCount == 0)
      {
        roleGroup.AddWarning("There are no roles in this role group.");
      }
    }


    return Task.CompletedTask;
  }
}
