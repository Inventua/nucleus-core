# Release Notes

## Version 1.0.1.0
6 September 2022

Version 1.0.1 includes administration user interface enhancements, a change to the file system provider interfaces to support the 
(coming soon) Amazon S3 file system provider, along with other enhancements and bug fixes in the core, and in core modules.

### Caching
Improved data caching to improve performance and reduce database activity.

### System Control Panel
Commit 7742f9f7
Added bootstrap table classes to system information table.

### New:  AWS S3 file system provider
Commit 47f7bdac
Created AWS S3 file system provider.

### File System Providers
Changes to support S3 file system provider:

Commit 8bb198c2
Local File System provider: Added code to set parent of top folder to NULL (and handle the null value correctly in the 
file system manager).

Commit ecb1d4a9
Made file system provider and search index manager methods async (return Task).

Commit bb8b1457: 
Implemented async interfaces for search manager, file system providers.

Commit 5e446bfb: 
Fixed issue with image selector (html editor plugin) sending null path by default.

The change to the file system provider interface required minor compatibility updates to:
- Azure File System Provider

### Search 
Commit c898c55a: 
Implemented "basic page search" provider.  Added support for multiple installed search providers & selection control in 
the settings page.  Improved behaviour when max suggestions is set to zero.

Commit 83ae08d9: 
ElasticSearch provider:  Added DisplayName attribute so that search selection controls can display a friendly name rather 
than an class name.

Commit 1970e895: 
Updates to allow provider selection in the search html helper and tag helper.

Commit 27e3e49a: 
nucleus-shared.js. Added code to prevent setting focus to the first control in a partial page response if the currently
selected control is an input[type=search].

Commit 770ecd6b: 
Added search option & implementation to clear search indexes at the start of a feed.

Commit ecb1d4a9: 
Made file system provider and search index manager methods async (return Task).

The change to the search interface required minor compatibility updates to:
- Elastic Search Provider
- Documents Module
- Static Content Module

### Forums
Commit 28a7559e: 
Added title attribute to the "list forums" view (most recent post table cell) so that the full title can be seen by hovering 
over the "most recent" column.

Commit 0e1da005: 
Bug fix:  Unsubscribe forum post in "Manage Subscriptions" was throwing an error because the action name was wrong in 
ManageSubscriptions.cshtml, fixed.  Also added "btn-sm" class to unsubscribe buttons to render them as small buttons.

### Google Analytics
Commit aa3245e0: 
Added 'anonymize_ip': true to the Google Analytics initialization script, so that Google saves user IP addresses with the 
last octet set to zero, so that the IP address cannot be used to personally identify a user.

### New: Advanced SiteMap
Created Advanced site map extension.  The Advanced SiteMap extension replaces the built-in Nucleus component which generates a `sitemap.xml`
file for use by external search engines.  It implements the Nucleus search index manager interface so that the generated site map can include 
additional entries from extensions which submit meta-data to the search system, like the Documents, Forums and Publish modules.

### Installation Wizard
Commit 0f9347aa: 
Updated table classes to improve presentation of long package descriptions.

### Developer Tools
Commit bfc11127: 
module.build.targets. Removed unused code, added version number message, added XmlPeek command to automatically update 
package.xml version to .csproj version.

### Install Wizard
Commit b5d8d27d: Added logic to check for and resolve case when more than one copy of the same module (different versions) is in 
/setup/extensions.  Added package version display to wizard.

Commit 38f5174d: Enabled enhanced tooltips, reviewed and updated tooltips, presentation enhancements.

Commit f5c1fd92: Page routing: Improved 404 handling when no site is matched.  If the request path did not match a site, and 
the response is a 404 (so it didn't match a controller route or any other component that can handle the request), and there
are no sites in the sites table, redirect to the setup wizard.  This is to handle cases where there is a 
/Setup/install-log.config file present (indicating that setup has previously completed), but the database is empty.  This is 
mostly a scenario that happens in testing, but it could also happen if a user decided to attach to a different (new) database.

### General Improvements
Admin User Interface:
Commit 757a71ca: 
Added "upload and un-zip" function to file manager, moved file validation into new AllowedFileTypeExtensions class so 
that it can be shared between FileIntegrityCheckMiddleware and zipfile downloading functions.

Commit 84800f35: File Manager: Added selected folder "breadcrumb" links to folders which are higher in the folder hierachy.

Commit 025eb8e3: File Manager: Added styling to make the files/folders display scrollable with the tools controls fixed at the 
top of the page.

Commit 3c9d0df1: File Manager: Added list of selected files and folders in delete confirmation dialog.  Added folder-empty validation.  Added 
client-side code to nucleus-shared.js to parse a BadRequest with a ProblemDetails response.  Added code to set 
content-disposition of downloaded files to attachment.

Commit cc3b52cc: 
Updated material icons font to latest version.

Commit f7dd3a6d: 
Altered markup rendered by LinkButton and SubmitButton helpers so that their output is more consistent.

Commit cac06390: 
FileSystemManager: Added exception handling (suppression) to GetImageDimensions in case a bad image file causes an 
exception.

Commit 2a4dcaa6: 
File System Manager: Added code to expire saved DirectUrl when a file is uploaded.

Commit 54d90a70: 
Added progress tag helper, fixed upload progress display bug, improved presentation of the file manager.

Commit a3983409: 
Added display of running task status to scheduled tasks editor.  Updated task scheduler to correctly update progress 
object.

Commit e087393c: 
Removed "remember me" from the "emergency" login page, fixed post-login redirect.  This is the login page which can be
used by browsing to /user/account, which is present in case the site's login page is not set, or the login page is somehow broken.  The 
intended use for the emergency login page is so site admins can log in and correct a misconfiguration of the "proper" login page.  More 
functionality will most likely be removed from the emergency login page in the future.

Commit 46664286: 
Added Nucleus scripts and stylesheets to "Well known" enum in the AddScript Html Helper and AddStyle Html Helper.

### Bug fixes
Commit 1107355c: 
Core data provider:  Added code to .DeleteList to prevent an error when deleting a list which has list items - clear the list items collection 
before calling entity framework .Remove, so that EF doesn't try to delete the items (A  database cascade-delete referential action removes 
list items).

---

## Version 1.0.0.0
August 26 2022

First release of Nucleus CMS, a .NET Core/MVC-based web application framework and content management system. Use Nucleus for your site 
or web application to take advantage of built-in and third party extensions, and extend the system by creating new layouts, modules, 
web applications and other components.
