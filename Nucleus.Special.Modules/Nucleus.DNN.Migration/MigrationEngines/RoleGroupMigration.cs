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

  public RoleGroupMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IRoleGroupManager roleGroupManager) : base("Migrating Role Groups")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.RoleGroupManager = roleGroupManager;
  }

  public override async Task Migrate(Boolean updateExisting)
  {    
    foreach (RoleGroup dnnRoleGroup in this.Items)
    {
      if (dnnRoleGroup.CanSelect && dnnRoleGroup.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.RoleGroup newRoleGroup = null;

          if (updateExisting)
          {
            newRoleGroup = await this.RoleGroupManager.GetByName(this.Context.Site, dnnRoleGroup.RoleGroupName);
          }

          if (newRoleGroup == null) 
          {
            newRoleGroup = await this.RoleGroupManager.CreateNew();
          }          

          newRoleGroup.Name = dnnRoleGroup.RoleGroupName;
          newRoleGroup.Description = dnnRoleGroup.Description;

          await this.RoleGroupManager.Save(this.Context.Site, newRoleGroup);
        }
        catch (Exception ex)
        {
          dnnRoleGroup.AddError($"Error importing role group '{dnnRoleGroup.RoleGroupName}': {ex.Message}");
        }

        this.Progress();
      }
      else
      {
        dnnRoleGroup.AddInformation($"Role group '{dnnRoleGroup.RoleGroupName}' was not selected for import.");
      }
    }

    this.SignalCompleted();
  }

  public override Task Validate()
  {
    foreach (RoleGroup roleGroup in Items)
    {      
      if (roleGroup.RoleCount == 0)
      {
        roleGroup.AddInformation("There are no roles in this role group.");
      }
    }


    return Task.CompletedTask;
  }
}
