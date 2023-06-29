using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class UserMigration : MigrationEngineBase<Models.DNN.User>
{
  private IUserManager UserManager { get; }
  private IRoleManager RoleManager { get; }

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }

  public UserMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IUserManager userManager, IRoleManager roleManager) : base("Users")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.UserManager = userManager;
    this.RoleManager = roleManager;
  }

  public override async Task Migrate()
  {
    foreach (User dnnUser in this.Items)
    {
      if (dnnUser.CanSelect && dnnUser.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.User newUser = await this.UserManager.CreateNew(this.Context.Site);

          newUser.UserName = dnnUser.UserName;
          newUser.Verified = false;
          newUser.IsSystemAdministrator = false;

          foreach (Role dnnUserRole in dnnUser.Roles)
          {
            Nucleus.Abstractions.Models.Role newRole = await this.RoleManager.GetByName(this.Context.Site, dnnUserRole.RoleName);
            if (newRole == null)
            {
              dnnUser.AddWarning($"Role '{dnnUserRole.RoleName}' not added, because the role does not exist in Nucleus.");
            }
            else
            {
              newUser.Roles.Add(newRole);
            }
          }

          // all migrated users are unverified & have no password
          newUser.Verified = false;

          foreach (UserProfileProperty dnnUserProfileItem in dnnUser.ProfileProperties.Where(prop => prop.PropertyDefinition != null))
          {
            Nucleus.Abstractions.Models.UserProfileValue nucleusUserProfileValue = newUser.Profile
              .Where(prop => ReplaceIgnoredCharacters(prop.UserProfileProperty.Name).Equals(ReplaceIgnoredCharacters(dnnUserProfileItem.PropertyDefinition.PropertyName), StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();
            if (nucleusUserProfileValue == null)
            {
              dnnUser.AddWarning($"Profile property value for '{dnnUserProfileItem.PropertyDefinition.PropertyName}' not added, because a matching profile property does not exist in Nucleus.");
            }
            else
            {
              nucleusUserProfileValue.Value = dnnUserProfileItem.Value;
            }
          }

          // handle email, which is a profile property in Nucleus, but a user property in DNN
          Nucleus.Abstractions.Models.UserProfileValue nucleusUserEmailProfileValue = newUser.Profile
              .Where(prop => ReplaceIgnoredCharacters(prop.UserProfileProperty.Name).Equals("Email", StringComparison.OrdinalIgnoreCase) || ReplaceIgnoredCharacters(prop.UserProfileProperty.Name).Equals("EmailAddress", StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();
          if (nucleusUserEmailProfileValue == null)
          {
            dnnUser.AddWarning($"Profile property value for 'Email' not added, because a matching profile property does not exist in Nucleus.");
          }
          else
          {
            nucleusUserEmailProfileValue.Value = dnnUser.Email;
          }

          await this.UserManager.Save(this.Context.Site, newUser);
        }
        catch (Exception ex)
        {
          dnnUser.AddError($"Error importing User '{dnnUser.UserName}': {ex.Message}");
        }

        this.Progress();
      }
      else
      {
        dnnUser.AddWarning($"User '{dnnUser.UserName}' was not selected for import.");
      }
    }
  }

  private string ReplaceIgnoredCharacters(string value)
  {
    return System.Text.RegularExpressions.Regex.Replace(value, "[\\t\\s-]", "").Trim();
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
