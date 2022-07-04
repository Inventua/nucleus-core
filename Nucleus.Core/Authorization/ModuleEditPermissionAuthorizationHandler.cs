using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Core.Authorization
{
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
              Logger.LogTrace("User {0}: Access granted to module {1} using role {2}[{3}] (Inheriting page permissions).", context.User, this.CurrentContext.Module.Id, permission.Role.Id, permission.Role.Name);
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
                Logger.LogTrace("User {0}: Access granted to Module {1} using role {2}[{3}].", context.User, this.CurrentContext.Module.Id, permission.Role.Id, permission.Role.Name);
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

      if (this.CurrentContext.Page == null && this.CurrentContext.Module == null && !context.HasSucceeded)
      {
        // if the PageRoutingMiddleware didn't find a page, and the ModuleRoutingMiddleware didn't find a
        // module, and we haven't otherwise "succeeded", return success, since the user isn't trying to access
        // a page or a module.  This allows:
        // (a) Extensions which manage their own routes to work and
        // (b) If no route matches, NET core can return a 404
        context.Succeed(requirement);
      }

      return Task.CompletedTask;
    }
  }
}
