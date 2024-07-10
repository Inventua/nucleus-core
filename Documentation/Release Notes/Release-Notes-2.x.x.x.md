# Release Notes

## Version 2.0.0.0
10 July 2024

- Upgraded to .NET Core 8, Entity Framework 8, Bootstrap 5.3.
 
- Setup 
  - Added Installation sets for Windows x64, Linux ARM64 and Linux x64 runtime environments.  Portable install set for all runtimes is still available, but the platform-specific install sets are smaller.
  - Added database and file system provider configuration to the setup wizard.
  - Powershell setup script, Linux shell script to automate setup tasks.
   
- Search:
  Added permissions checking on results, added a "type" property for on-screen display.  Improved performance and reliability by adding the ability to check whether a search item's source  has been updated and only re-index content if content has changed.

- Permissions:
  Added folder "browse" permission type. Added "Copy permissions to decendants" for folders and pages.

- User Manager:
  - Added filtering for user list (approved, verified, role membership).
  - Added account information display (password valid, verified status, last activity, email re-send buttons)

- Extensions: 
  Added "usage" UI - display which pages use a module, layout or container

- Authentication:
  - Added Windows Authentication.
  - Added password expiry functionality.

- DNN Migration:
  Added supporting functionality for the new DNN Migration utility.  The (free, experimental) DNN migration utility can be installed using the Nucleus store.

- Mail:
  - Added generic Mail Provider interface for mail providers, moved core SMTP implementation to a new extension and added a new SendGrid mail extension.
  
- Pages:
  Added page "link type".  Pages can have a link type: Normal, Url (redirect to specified url), Page (render a different page from within the site) or File (download a file).

- Mail Template Editor
  Implemented "Monaco" editor.

Performance:
- Enabled Razor compile at build time for the admin UI and most extensions.
- Embedded static resources in assemblies.
- Core module renderer performance improvements.

- Developer Features:
  - Added new analyzers, build tasks, code fix providers to developer tools help developers identify common problems.
  - Added support for Blazor, including WebAssembly in addition to MVC.

- Admin User Interface:
  - Moved Admin UI to a separate project and assembly, added plug-in model to facilitate admin UI replacements.
  - Added "dock top" option for admin sidebar, which works better on tablets.
 
- General:
  - Added inline editing for module titles, Text/Html, documents, other extensions.
  - Various UI improvements, bug fixes and performance enhancements.

You can view a full list of updates in GitHub: https://github.com/Inventua/nucleus-core/commits/main
