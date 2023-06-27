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

  public override async Task Migrate(List<RoleGroup> items)
  {
    this.Start(items.Where(roleGroup => roleGroup.CanSelect && roleGroup.IsSelected).ToList());

    foreach (int roleGroupId in this.Items.Select(roleGroup => roleGroup.RoleGroupId))
    {
      RoleGroup roleGroup = await this.MigrationManager.GetDNNRoleGroup(roleGroupId);

      try
      {
        Nucleus.Abstractions.Models.RoleGroup newRoleGroup = await this.RoleGroupManager.CreateNew();

        newRoleGroup.Name = roleGroup.RoleGroupName;
        newRoleGroup.Description = roleGroup.Description;
      
        await this.RoleGroupManager.Save(this.Context.Site, newRoleGroup);
      }
      catch (Exception ex)
      {
        this.AddError(roleGroup.RoleGroupId, $"Error importing role group '{roleGroup.RoleGroupName}': {ex.Message}.");
      }

      this.Progress();
    }
  }

  public override Task Validate(List<RoleGroup> items)
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
