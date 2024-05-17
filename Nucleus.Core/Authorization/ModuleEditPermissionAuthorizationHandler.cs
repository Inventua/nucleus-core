using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Authorization;

public class ModuleEditPermissionAuthorizationHandler : AuthorizationHandler<ModuleEditPermissionAuthorizationRequirement>
{
  private Nucleus.Abstractions.Models.Context CurrentContext { get; }
  private ILogger<ModuleEditPermissionAuthorizationHandler> Logger { get; }

  public ModuleEditPermissionAuthorizationHandler(Nucleus.Abstractions.Models.Context context, ILogger<ModuleEditPermissionAuthorizationHandler> logger)
  {
    this.CurrentContext = context;
    this.Logger = logger;
  }

  /// <summary>
  /// Makes a decision if the user is allowed to access the requested <see cref="Page"/> or <see cref="PageModule"/>.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="requirement"></param>
  /// <returns></returns>
  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuleEditPermissionAuthorizationRequirement requirement)
  {
    if (context.User.IsSystemAdministrator())
    {
      Logger.LogTrace("User {0}: Access granted (System administrator).", context.User.GetUserId());
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    if (context.User.IsSiteAdmin(this.CurrentContext.Site))
    {
      Logger.LogTrace("User {0}: Access granted (Site administrator).", context.User.GetUserId());
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    // match user roles to this.CurrentContext.Page.Permissions.  This class checks EDIT permissions only.
    if (this.CurrentContext.Module != null)
    {
      Logger.LogTrace("Checking permissions for module {0}.", this.CurrentContext.Module.Id);

      // if the user has page edit permission then they have module edit permission.
      foreach (Permission permission in this.CurrentContext.Page.Permissions)
      {
        if (permission.IsPageEditPermission())
        {
          if (permission.IsValid(this.CurrentContext.Site, context.User))
          {
            // If any of the user's roles have permission, the user has permission
            Logger.LogTrace("User {user}: Access granted to module {module} using role {roleid}[{rolename}] (Inheriting page permissions).", context.User, this.CurrentContext.Module.Id, permission.Role.Id, permission.Role.Name);
            context.Succeed(requirement);
            break;
          }
        }
      }

      // only check module edit permission if the module is not inheriting page permissions.
      if (!this.CurrentContext.Module.InheritPagePermissions)
      {
        foreach (Permission permission in this.CurrentContext.Module.Permissions)
        {
          if (permission.IsModuleEditPermission())
          {
            if (permission.IsValid(this.CurrentContext.Site, context.User))
            {
              // If any of the user's roles have permission, the user has permission
              Logger.LogTrace("User {user}: Access granted to Module {moduleid} using role {roleid}[{rolename}].", context.User, this.CurrentContext.Module.Id, permission.Role.Id, permission.Role.Name);
              context.Succeed(requirement);
              break;
            }
          }
        }
      }

      if (!context.HasSucceeded)
      {
        Logger.LogTrace("User {0}: Access denied to Module {1}.", context.User, this.CurrentContext.Module.Id);
        context.Fail();
      }
    }

    if (this.CurrentContext.Module == null && !context.HasSucceeded)
    {
      // the user can't have Module edit permission if no module was specified
      Logger.LogTrace("User {0}: Access denied, no module specified.", context.User);
      context.Fail();
    }

    return Task.CompletedTask;
  }
}
