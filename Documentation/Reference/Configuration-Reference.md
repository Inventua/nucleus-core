# Configuration Reference
Nucleus configuration settings may be split across multiple .json files and can be stored in environment variables or 
submitted as command-line arguments.  Refer to the [Configuration](/Configuration/) page for information on how Nucleus 
loads configuration settings.

> The default `appSettings.json` file contains a full set of available configuration settings (some are commented out), which 
you should use as a reference for the file format.

> When Nucleus configuration settings are read from .json files, they are part of a hierachy that begins with a `"Nucleus"` 
element.  In this document, configuration sections and items are specified using the `:` character to represent the hierachy 
of configuration elements.  The configuration section in the example below would be represented as `Nucleus:ResourceFileOptions` and 
the `UseMinifiedJs` property would be represented as `Nucleus:ResourceFileOptions:UseMinifiedJs`.  This is also how the .Net core
configuration system 
```
"Nucleus": { 
    "ResourceFileOptions": {
      "UseMinifiedJs": true    // We would refer to this as Nucleus:ResourceFileOptions:UseMinifiedJs
    }
  }
```

## Configuration Settings

> This page contains information on Nucleus-specific settings only, which reside within the `"Nucleus"` section of your 
configuration files.  ASP.NET Core and third party components have their own configuration settings, and they may also be 
present in your configuration files, but are not documented here.

### Nucleus
General top-level settings.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| EnableResponseCompression        | (Boolean)  Default is true. | 
| EnableForwardedHeaders           | (Boolean)  Default is true. | 
| MaxRequestSize                   | (long) Specifies the maximum [MultipartBodyLengthLimit](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.features.formoptions.multipartbodylengthlimit) and [MaxRequestBodySize](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits.maxrequestbodysize) in bytes.  This value must be set high enough for the largest file upload that you want to allow.  If not specified, the .net core defaults are used. | 

```
"Nucleus": 
{
  "EnableResponseCompression": true,
  "EnableForwardedHeaders": true,
  "MaxRequestSize": 67108864
  ...
}
```

### Nucleus:FolderOptions

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| DataFolder                       | (String) Specifies the name of the root folder that Nucleus uses to store files on the file system.  | 

```
"Nucleus": 
{
  "FolderOptions": 
  {
    "DataFolder": "%ProgramData%/Nucleus"
  }
  ...
}
```

> The DataFolder setting can contain environment variables like `%ProgramData%`, which are resolved at run time.

> File-based database providers (Sqlite) store database files in a `/Data` subfolder of the folder specified by `Nucleus:FolderOptions:DataFolder`.

### Nucleus:TextFileLoggerOptions

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Path                             | (String) Specifies the name of the folder that the Nucleus text file logger uses to store log files on the file system.  By default, this value is the `Logs` subfolder of the folder specified by `Nucleus:FolderOptions:DataFolder`. | 

```
"Nucleus": 
{
  "TextFileLoggerOptions": 
  {
  "Path": "{DataFolder}/Logs"
  }
  ...
}
```

### Nucleus:ResourceFileOptions
Controls use of minified and merged javascript and css stylesheets.  Developers may want to set these values to false in order to more easily debug client-side javascript and css. 

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| UseMinifiedJs                    | (Boolean) Specifies whether to use minified versions of .js files, when they are available.  Default value is true.  | 
| UseMinifiedCss                   | (Boolean) Specifies whether to use minified versions of .css files, when they are available.  Default value is true. | 
| MergeJs                          | (Boolean) Specifies whether to merge .js files into a single file.  Default value is true. | 
| MergeCss                         | (Boolean) Specifies whether to merge .css files into a single file.  Default value is true. | 

```
"Nucleus": 
{
  "ResourceFileOptions": 
  {
    "UseMinifiedJs": true,
    "UseMinifiedCss": true,
    "MergeJs": true,
    "MergeCss": true
  }
  ...   
}
```

### Nucleus:HtmlEditor
Specifies stylesheets and script files used by the Html editor.  More than one Html editor can be included, but only one can be selected.  This 
section is automatically present in the default appSettings.config file, and users generally do not need to edit this section.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Default                          | (String) Specifies the `Key` of the Html Editor that is in use.  This value does not have a default.  | 
| HtmlEditors                      | (array) Array of one or more HtmlEditor sections. | 

#### Nucleus:HtmlEditor[n]
Each HtmlEditor has properties:

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Key                              | (String) Unique identifier for the Html Editor configuration section.  | 
| Scripts                          | (array) Array of one or more script sections. | 

#### Nucleus:HtmlEditor[n]:Scripts[n]
Each scripts section has properties:

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Type                             | (String) javascript or stylesheet.  | 
| Path                             | (String) Application-relative path to the script or javascript file.  Start with a `~` to represent the application root. | 
| IsDynamic                        | (Boolean) If set to true, prevents the script from being merged.  This is to support Html editors that perform dynamic loading of other scripts and therefore must be served from their "real" path. | 

### Nucleus:FileSystems

### Nucleus:FileSystems:AllowedFileTypes
AllowedFileTypes are used to specify one or more file extensions and a list of signatures for each file type.  Files with extensions
do not match an entry in this list cannot be uploaded and will generate an error.  The integrity of uploaded files is validated by comparing 
the first few bytes of the file with the specified signatures.  The file bytes must match at least one of the signatures.  Signatures are 
specified as hexadecimal values, with no spaces or delimiters.  The special value "??" in a signature skips validation of the byte in the ordinal
position represented by the ?? characters.

The `Nucleus:FileSystems:AllowedFileTypes` element is an array of allowed file type elements.  This section is automatically present in the 
default appSettings.config file, and users generally do not need to edit this section, unless there is a need to support upload of additional 
file types.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| FileExtensions                   | (array of string) Array of file extensions represented by this entry, including the leading `.` character.  Not case-sensitive.  | 
| Signatures                       | (array of string) Array of file signatures which match the file extensions specified for this entry. | 
| Restricted                       | (Boolean). Specifies whether uploads of the file type are restricted to site administrators.| 

```
"Nucleus": 
{
  "FileSystems": 
  {
    "AllowedFileTypes": 
    [
      {
        "FileExtensions": [ ".jpg", ".jpeg" ],
        "Signatures": [ "FFD8FFE0", "FFD8FFE1", "FFD8FFE2", "FFD8FFE3" ]
      },
      {
        ...
      }
    ]
  }
}
```
    
### Nucleus:FileSystems:Providers
Specifies one or more file system providers.  The configuration section for each file provider has a key, name and provider type (class name).
You can specify multiple file providers, and the user can choose from a list in the user interface.  The "Name" property is shown 
to the user.  Each entry has a key which uniquely identifies the provider entry.

The `Nucleus:FileSystems:Providers` element is an array of file system provider type elements.  

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Key                              | (String) Unique identifier for the file system provider configuration entry.  | 
| Name                             | (String) Friendly name for the file system provider, which is shown to users when selecting a file system. | 
| ProviderType                     | (String). Assembly-qualified class name for the file system provider. | 

```
"Nucleus": 
{
  "FileSystems": 
  {
    "Providers": 
    [
      {
        "Key": "local",
        "Name": "Local File System",
        "ProviderType": "Nucleus.Core.FileSystemProviders.LocalFileSystemProvider,Nucleus.Core"
      }
    ]
  }
  ...
}
```

### Nucleus:FileSystems:PasswordOptions
Specifies authentication and password settings.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| FailedPasswordWindowTimeout      | (Timespan) Specifies the time span for which the system remembers that a user has had a failed password attempt.  Repeated failures within the specified file will increment the failed password attempts count, and when the FailedPasswordMaxAttempts threshold is exceeded, the user account will be locked for a period of time.  Default value is 0:15:00 (15 minutes).  | 
| FailedPasswordMaxAttempts        | (Integer) Specifies the number of failed password attempts before account lockout.  Default value is 3. | 
| FailedPasswordLockoutReset       | (Timespan). Specifies the time span after which an account lockout is automatically reset.  Default value is 0:10:00 (10 minutes). | 
| PasswordResetTokenExpiry         | (Timespan). Specifies the time span after which a password reset token expires.  Password reset tokens are sent to the user when they request a password reset.  Default value is 02:00:00 (2 hours). | 
| PasswordComplexityRules          | (array). Array of password complexity rules. | 

```
"Nucleus": 
{
  "PasswordOptions": 
  {
    "FailedPasswordWindowTimeout": "0:15:00",
    "FailedPasswordMaxAttempts": 3,
    "FailedPasswordLockoutReset": "0:10:00",
    "PasswordResetTokenExpiry": "02:00:00",   
    "PasswordComplexityRules": 
    [
      {
        "Pattern": "^(?=.*[A-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[\\\\!\\@\\#\\$\\%\\^\\&\\*\\(\\)\\[\\]])\\S{8,}$",
        "Message": "Passwords must contain at least one upper case character, at least one lower case character, at least one number, at least one symbol and must be at least 8 characters long."
      }
    ]
  }
  ...
}
```

### Nucleus:FileSystems:PasswordOptions:PasswordComplexityRules
Password complexity rules are regular expressions.  You can specify multiple password complexity rules and all of them must succeed (match) in order for a password to be valid.  

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Pattern                          | (String) Regular expression which passwords must match in order to be valid.  | 
| Message                          | (String) Error message when the pattern does not match the entered password. | 

### Nucleus:ClaimTypeOptions
Default available claim types.  User profile properties always have a claim type, specified by Uri.  If the site administrator configures a user profile property with a 
Uri value which match one of the entries below, the html input for that property will be configured with a type attribute set to the claim type's InputType.  If the 
IsSiteDefault property is true, new sites will automatically have a user profile property added for the entry.

The `Nucleus:ClaimTypeOptions` element contains a "Types" element, which is an array of file system provider type elements.  

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| DefaultName                      | (String) Default label caption for the type.  | 
| Uri                              | (String) Unique identifier for the claim type.  Use a value from [Claims.ClaimTypes](https://docs.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes) or [OASIS](http://docs.oasis-open.org/imi/identity/v1.0/os/identity-1.0-spec-os.html) if a suitable claim type is present.  If you can't find a standard claim Uri for the property you are adding, you can make up your own. | 
| InputType                        | (String) If specified, the value specified will be used for the input element [type](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#input_types) when an input control is rendered. | 
| IsSiteDefault                    | (Boolean) If set to true, new sites will automatically have a user profile property added for the claim.  Default is false. | 

```
"Nucleus": 
{
  "ClaimTypeOptions":
  {
    "Types": 
    [
      {
        "DefaultName": "First Name",
        "Uri": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"
      },
      {
        "DefaultName": "Mobile Phone",
        "Uri": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone",
        "InputType": "tel",
        "IsSiteDefault": true
      }
    ]
    }
}
```

### Nucleus:CacheOptions
Specifies cache capacity and expiry for the named cache.  Once the cache capacity threshold is reached, new cache entries will
displace the oldest cache entry.  Cache entries are automatically removed once they are older than ExpiryTime.  ExpiryTime is 
expressed as a time span (hh:mm:ss).

Nucleus core cache keys are PageCache, PageRouteCache, PageMenuCache, MailTemplateCache, PageModuleCache, RoleCache, RoleGroupCache, 
ScheduledTaskCache, SiteCache, SiteDetectCache, UserCache, FolderCache, ListCache, ContentCache, SessionCache.  Extensions may cache 
values, refer to each extension's documentation for the cache key name.  If configuration values are omitted, the default capacity is 
1000, and expiry time is 5 minutes.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Capacity                         | (Integer) Number of entities to cache in memory.  | 
| ExpiryTime                       | (Timespan) Expiry time for cache entries. | 

```
"Nucleus":
  ...
  "PageCache": {
    "Capacity": 500,
    "ExpiryTime": "00:15:00"
  },
  "PageModuleCache": {
    "Capacity": 2000,
    "ExpiryTime": "00:15:00"
  }
```