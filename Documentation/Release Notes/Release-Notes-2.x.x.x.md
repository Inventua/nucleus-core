# Release Notes

## Version 2.0.0.0
26 July 2024

- Upgraded to .NET Core 8, Entity Framework 8, Bootstrap 5.3.
 
- Setup 
  - Added Installation sets for Windows x64, Linux ARM64 and Linux x64 runtime environments.  Portable install set for all runtimes is still available, but the platform-specific install sets are smaller.
  - Powershell setup script, Linux shell script to automate setup tasks.
   
- Permissions:
  Added "Copy permissions to decendants" for folders and pages.

- User Manager:
  - Added account information display (password valid, verified status, last activity, email re-send buttons)

- Extensions: 
  Added "usage" UI - display which pages use a module, layout or container

- Mail:
  - Added generic Mail Provider interface for mail providers, moved core SMTP implementation to a new extension and added a new SendGrid mail extension.

Performance:
- Enabled Razor compile at build time for the admin UI and most extensions.
- Embedded static resources in assemblies.
- Core module renderer performance improvements.

- Developer Features:
  - Added support for Blazor, including WebAssembly in addition to MVC.

- Admin User Interface:
  - Moved Admin UI to a separate project and assembly, added plug-in model to facilitate admin UI replacements.
  - Added "dock top" option for admin sidebar, which works better on tablets.
 
- General:
  - Added inline editing for module titles, Text/Html, documents, other extensions.
  - Various UI improvements, bug fixes and performance enhancements.

You can view a full list of updates in GitHub: https://github.com/Inventua/nucleus-core/commits/main
