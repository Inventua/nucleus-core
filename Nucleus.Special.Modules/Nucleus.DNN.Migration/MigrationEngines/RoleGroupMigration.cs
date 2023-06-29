using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class RoleGroupMigration : MigrationEngineBase<Models.DNN.RoleGroup>
{
  private IRoleGroupManager RoleGroupManager { get; }
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }

  public RoleGroupMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IRoleGroupManager roleGroupManager) : base("Role Groups")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.RoleGroupManager = roleGroupManager;
  }

  public override async Task Migrate()
  {    
    foreach (RoleGroup roleGroup in this.Items.Where(roleGroup => roleGroup.CanSelect && roleGroup.IsSelected))
    {
      try
      {
        Nucleus.Abstractions.Models.RoleGroup newRoleGroup = await this.RoleGroupManager.CreateNew();

        newRoleGroup.Name = roleGroup.RoleGroupName;
        newRoleGroup.Description = roleGroup.Description;
      
        await this.RoleGroupManager.Save(this.Context.Site, newRoleGroup);
      }
      catch (Exception ex)
      {
        roleGroup.AddError($"Error importing role group '{roleGroup.RoleGroupName}': {ex.Message}");
      }

      this.Progress();
    }
  }

  public override Task Validate()
  {
    foreach (RoleGroup roleGroup in Items)
    {      
      if (roleGroup.RoleCount == 0)
      {
        roleGroup.AddWarning("There are no roles in this role group.");
      }
    }


    return Task.CompletedTask;
  }
}
