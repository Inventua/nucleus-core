using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class RoleMigration : MigrationEngineBase<Models.DNN.Role>
{
  private IRoleManager RoleManager { get; }
  private IRoleGroupManager RoleGroupManager { get; }
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }

  public RoleMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IRoleManager roleManager, IRoleGroupManager roleGroupManager) : base("Migrating Roles")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.RoleManager = roleManager; 
    this.RoleGroupManager = roleGroupManager;
  }

  public override async Task Migrate(Boolean updateExisting)
  {    
    foreach (Role role in this.Items)
    {
      if (role.CanSelect && role.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.Role newRole = null;
          
          if (updateExisting)
          {
            newRole = await this.RoleManager.GetByName(this.Context.Site, role.RoleName);
          }

          if (newRole == null)
          {
            newRole = await this.RoleManager.CreateNew();
          }

          newRole.Name = role.RoleName;
          newRole.Type = role.AutoAssignment ? Abstractions.Models.Role.RoleType.AutoAssign : Abstractions.Models.Role.RoleType.Normal;
          newRole.Description = role.Description;
          if (role.RoleGroup != null)
          {
            newRole.RoleGroup = await this.RoleGroupManager.GetByName(this.Context.Site, role.RoleGroup.RoleGroupName);
          }
          await this.RoleManager.Save(this.Context.Site, newRole);
        }
        catch (Exception ex)
        {
          role.AddError($"Error importing role '{role.RoleName}': {ex.Message}");
        }
        this.Progress();
      }
      else
      {
        role.AddWarning($"Role '{role.RoleName}' was not selected for import.");
      }
    }

    this.SignalCompleted();
  }

  public override Task Validate()
  {
    foreach (Role role in this.Items)
    {
      string[] RESERVED_ROLES = { "Administrators", "Registered Users", "Unverified Users" };
      if (RESERVED_ROLES.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase)) 
      {
        role.AddError($"'{role.RoleName}' is a reserved/special role in DNN and will not be migrated.");
      }

      // roles which are un-selected by default
      if (role.RoleName.Equals("Subscribers", StringComparison.OrdinalIgnoreCase) || role.RoleName.StartsWith("Translator", StringComparison.OrdinalIgnoreCase))
      {
        role.IsSelected = false;
        role.AddWarning($"The '{role.RoleName}' role has been un-selected by default, but you can choose to migrate it.");
      }

      if (role.UserCount == 0)
      {
        role.IsSelected = false;
        role.AddWarning($"There are no users in the '{role.RoleName}' role.  The role has been un-selected by default, but you can choose to migrate it.");
      }
    }

    return Task.CompletedTask;
  }
}
