using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Authorization;

public class PageEditPermissionAuthorizationHandler : AuthorizationHandler<PageEditPermissionAuthorizationRequirement>
{

  private Nucleus.Abstractions.Models.Context CurrentContext { get; }
  private ILogger<PageEditPermissionAuthorizationHandler> Logger { get; }

  public PageEditPermissionAuthorizationHandler(Nucleus.Abstractions.Models.Context context, ILogger<PageEditPermissionAuthorizationHandler> logger)
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
  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PageEditPermissionAuthorizationRequirement requirement)
  {
    if (context.User.IsSystemAdministrator())
    {
      Logger.LogTrace("User {userid}: Access granted (System administrator).", context.User.GetUserId());
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    if (context.User.IsSiteAdmin(this.CurrentContext.Site))
    {
      Logger.LogTrace("User {userid}: Access granted (Site administrator).", context.User.GetUserId());
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    // match user roles to this.CurrentContext.Page.Permissions.  This class checks Edit permissions only.
    if (this.CurrentContext.Page != null)
    {
      Logger.LogTrace("Checking permissions for page {pageid}.", this.CurrentContext.Page.Id);

      foreach (Permission permission in this.CurrentContext.Page.Permissions)
      {
        if (permission.IsPageEditPermission())
        {
          if (permission.IsValid(this.CurrentContext.Site, context.User))
          {
            // If any of the user's roles have permission, the user has permission
            Logger.LogTrace("User {user}: Access granted to page {pageid} using role {roleid}[{rolename}].", context.User.Identity.Name, this.CurrentContext.Page.Id, permission.Role.Id, permission.Role.Name);
            context.Succeed(requirement);
            break;
          }
        }
      }

      if (!context.HasSucceeded)
      {
        Logger.LogTrace("User {user}: Access denied to page {pageid}.", context.User.Identity.Name, this.CurrentContext.Page.Id);
        context.Fail();
      }
    }

    if (this.CurrentContext.Page == null && !context.HasSucceeded)
    {
      // the user can't have Page edit permission if no page was specified
      Logger.LogTrace("User {user}: Access denied, no page specified.", context.User);
      context.Fail();
    }
    return Task.CompletedTask;
  }
}
