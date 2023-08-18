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

  public UserMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IUserManager userManager, IRoleManager roleManager) : base("Migrating Users")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.UserManager = userManager;
    this.RoleManager = roleManager;
  }

  public override async Task Migrate(Boolean updateExisting)
  {
    foreach (User dnnUser in this.Items)
    {
      if (dnnUser.CanSelect && dnnUser.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.User newUser = null;

          if (updateExisting)
          {
            newUser = await this.UserManager.Get(this.Context.Site, dnnUser.UserName);
          }

          if (newUser == null) 
          {
            newUser = await this.UserManager.CreateNew(this.Context.Site);
            // all newly migrated users are unverified & have no password
            newUser.Verified = false;
          }

          newUser.UserName = dnnUser.UserName;
          newUser.Approved = dnnUser.UserPortal.Authorised;

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
              if (!newUser.Roles.Where(existingRole => existingRole.Id == newRole.Id).Any())
              {
                newUser.Roles.Add(newRole);
              }
            }
          }

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
              if (!String.IsNullOrEmpty(dnnUserProfileItem.Value))
              {
                nucleusUserProfileValue.Value = dnnUserProfileItem.Value;
              }
            }
          }

          // handle email, which is a profile property in Nucleus, but a user property in DNN
          Nucleus.Abstractions.Models.UserProfileValue nucleusUserEmailProfileValue = newUser.Profile
              .Where(prop => ReplaceIgnoredCharacters(prop.UserProfileProperty.Name).Equals("Email", StringComparison.OrdinalIgnoreCase) || ReplaceIgnoredCharacters(prop.UserProfileProperty.Name).Equals("EmailAddress", StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();
          if (nucleusUserEmailProfileValue == null)
          {
            if (!String.IsNullOrEmpty(dnnUser.Email))
            {
              dnnUser.AddWarning($"Profile property value for 'Email' not added, because a matching profile property does not exist in Nucleus.");
            }
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

    this.SignalCompleted();
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
      if (user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase) || user.Roles.Where(role => role.RoleName.Equals("Administrators", StringComparison.OrdinalIgnoreCase)).Any())
      {
        user.AddError("Administrator users can not be migrated.");
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
