# Release Notes

## Version 3.0.0.0
7 February 2025

- Upgraded to .NET Core 9, Entity Framework 9. Upgraded most third-party component references to latest versions.
 
- Setup 
  - Updated the Linux shell script with changes for .NET 9. Added install step to set the sticky bit on the app, data and certs folders. [Service] NotifyAccess=all to the systemd service file template to prevent warnings in Ubuntu 24.04.01
   
- Scheduled Tasks:
  Added semaphore check to TruncateHistory to avoid an error when history is truncated at the same time that as scheduled task history records are being updated. 

- Admin User Interface:
  - User interface enhancements to the system information page and logs display.

- Sites:
  - Added site CSS editor.
 
- Search:
  - Added search providers for Azure Search, TypeSense, Bing, Google. 
  - Added new GetCapabilities method to ISearchProvider so that search providers can return which features they support.
  - Added IContentConverter interface which can be used by search index managers to convert various file formats to text.
  - Created a basic content converter to convert between text/markdown, text/plain and text/html formats, and to convert PDF to plain text.
  - Created a Tika Server IContentConverter implementation to call a Tika Server to perform a wider range of conversions.

- Text/HTML Module
  - Added the ability to enter and store content as plain text and markdown, and to convert between text/markdown, text/plain and text/html.


You can view a full list of updates in GitHub: https://github.com/Inventua/nucleus-core/commits/main
