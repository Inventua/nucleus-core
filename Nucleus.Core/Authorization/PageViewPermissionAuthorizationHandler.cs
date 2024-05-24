using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Authorization
{
  public class PageViewPermissionAuthorizationHandler : AuthorizationHandler<PageViewPermissionAuthorizationRequirement>
  {
    private Application Application{ get; }
    private Context CurrentContext { get; }
    private ISiteManager SiteManager { get; }
    private ILogger<PageViewPermissionAuthorizationHandler> Logger { get; }

    public PageViewPermissionAuthorizationHandler(Application application, Context context, ISiteManager siteManager, ILogger<PageViewPermissionAuthorizationHandler> logger)
    {
      this.Application = application;
      this.CurrentContext = context;
      this.SiteManager = siteManager;
      this.Logger = logger;
    }

    /// <summary>
    /// Makes a decision if the user is allowed to access the requested <see cref="Page"/> or <see cref="PageModule"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requirement"></param>
    /// <returns></returns>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PageViewPermissionAuthorizationRequirement requirement)
    {
      if (context.User.IsSystemAdministrator())
      {
        Logger.LogTrace("User {0}: Access granted (System administrator).", context.User.GetUserId());
        context.Succeed(requirement);
        return;
      }

      if (this.CurrentContext.Site != null)
      {
        if (context.User.IsSiteAdmin(this.CurrentContext.Site))
        {
          Logger.LogTrace("User {0}: Access granted (Site administrator).", context.User.GetUserId());
          context.Succeed(requirement);
          return;
        }
      }

      // match user roles to this.CurrentContext.Page.Permissions.  This class checks VIEW permissions only.
      if (this.CurrentContext.Page != null)
      {
        Logger.LogTrace("Checking permissions for page {pageid}.", this.CurrentContext.Page.Id);

        foreach (Permission permission in this.CurrentContext.Page.Permissions)
        {
          if (permission.IsPageViewPermission())
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

      if (this.CurrentContext.Site == null && (!this.Application.IsInstalled || await this.SiteManager.Count() == 0))
      {
        // special case.  We check whether Nucleus is install/redirect to the setup wizard in Nucleus.Web.DefaultController, which has [AuthorizeController(PAGE_VIEW_POLICY)] in order
        // to check permissions for Nucleus pages, which calls this class.  So we have to return a success case in order to get to the code in Nucleus.Web.DefaultController.Index
        context.Succeed(requirement);
      }

      return;
    }
  }
}
