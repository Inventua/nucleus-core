using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class UserMigration : IMigrationEngine<Models.DNN.User>
{
  public Task Migrate(List<User> items)
  {
    throw new NotImplementedException();
  }

  public Task Validate(List<User> items)
  {
    foreach (User user in items)
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
        user.AddError("Users must have a non-blank email address.");
      }  
    }

    return Task.CompletedTask;
  }
}
