using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class RoleMigration : IMigrationEngine<Models.DNN.Role>
{
  public Task Migrate(List<Role> items)
  {
    foreach (Role role in items.Where(role => role.CanSelect && role.IsSelected))
    {
      Nucleus.Abstractions.Models.Role newRole = new() 
      { 
      };
    }
    return Task.CompletedTask;
  }

  public Task Validate(List<Role> items)
  {
    foreach (Role role in items)
    {
      string[] RESERVED_ROLES = { "Administrators", "Registered Users" };
      if (RESERVED_ROLES.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase)) 
      {
        role.AddError("This is a reserved role in DNN which will not be migrated.");
      }

      if (role.UserCount == 0)
      {
        role.AddWarning("There are no users in this role.");
      }
    }

    return Task.CompletedTask;
  }
}
