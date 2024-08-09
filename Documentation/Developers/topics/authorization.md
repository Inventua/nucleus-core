## Authorization
Nucleus implements .Net Core [Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction) policies in order to control access to 
controllers and/or controller actions.

To specify an authorization policy on a controller or action, add an  [Authorize](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authorization.authorizeattribute) 
attribute to your MVC controller or action:
```
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
```

### Policies
{.table-25-75}
|                      |                                                                                      |
|----------------------|--------------------------------------------------------------------------------------|
| SYSTEM_ADMIN_POLICY  | Restricts access to system administrators. |
| SITE_ADMIN_POLICY    | Restricts access to site administrators and system administrators. |
| PAGE_EDIT_POLICY     | Restricts access to users who are members of a role which has edit permissions for the current page. |
| PAGE_VIEW_POLICY     | Restricts access to users who are members of a role which has view permissions for the current page. |
| MODULE_EDIT_POLICY   | Restricts access to users who are members of a role which has edit permissions for the current module. |
| SITE_WIZARD_POLICY   | This is a special policy used during setup.  Do not use this policy in extensions. |

> System administrators and site administrators always have view and edit rights for all pages and modules.

> Module View permissions are always checked before rendering a module.  You do not need to specify an Authorization attribute for this check to occur.