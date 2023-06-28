using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class UserMigration : MigrationEngineBase<Models.DNN.User>
{
  private IUserManager UserManager { get; }
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }

  public UserMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IUserManager UserManager) : base("Migrating Users")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.UserManager = UserManager;
  }

  public override async Task Migrate()
  {
    foreach (User user in this.Items.Where(user => user.CanSelect && user.IsSelected))
    {
      try
      {
        Nucleus.Abstractions.Models.User newUser = await this.UserManager.CreateNew(this.Context.Site);

        newUser.UserName = user.UserName;
        newUser.Verified = false;
        newUser.IsSystemAdministrator = false;

        //newUser.Roles
        //newUser.Secrets
        //newUser.Profile

        await this.UserManager.Save(this.Context.Site, newUser);
      }
      catch (Exception ex)
      {
        user.AddError($"Error importing User '{user.UserName}': {ex.Message}");
      }

      this.Progress();
    }
  }

  public override Task Validate()
  {
    foreach (User user in this.Items)
    {
      if (user.IsSuperUser)
      {
        user.AddError("Super users can not be migrated.");
      }
      if (!user.UserPortal.Authorised)
      {
        user.AddError("Unauthorized users are not migrated.");
      }
      if (user.UserPortal.IsDeleted)
      {
        user.AddError("Deleted users are not migrated.");
      }
      if (String.IsNullOrEmpty(user.Email))
      {
        user.AddWarning("User has a blank email address.");
      }  
    }

    return Task.CompletedTask;
  }
}
