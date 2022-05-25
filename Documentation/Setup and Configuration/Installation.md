# Installation 
Nucleus supports multiple hosting environments, database providers and file system providers.  The default configuration is set up to run
in Windows/Internet Information Services using the Sqlite database provider and the local file system provider.

## Basic Setup 
1. Download the install set (zip format) from the [downloads](/downloads) page.
2. Create an installation folder, and un-zip the install set to that folder.
3. Install the [.Net Core Hosting Bundle](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle).  You 
may need to install the .Net Core Hosting Bundle and do a "Repair" install if IIS returns an error message and your Windows application event log 
contains an entry with the message '... Ensure that the versions of Microsoft.NetCore.App and Microsoft.AspNetCore.App targeted by the application 
are installed.'.  
4. In Internet Information Services (IIS) manager, add an application pool for your IIS application to use.  .Net core applications
require a unique (not shared) application pool, set to use NET CLR version v4.0.30319 and integrated pipeline.  
5. In Internet Information Services (IIS) manager, add a web site or application with the path set to your installation folder, and assign
the application pool that you created.  Nucleus ships with a web.config which is pre-configured with settings to run Nucleus in IIS.  
6. Assign 'Full Control' permissions to your Nucleus data folder for the `IIS AppPool\[AppPool Name]` user.  By default, your data folder 
is `C:\ProgramData\Nucleus`.  You may need to create the folder in order to assign permissions to it. The Nucleus data folder is used for cached 
files, logs and the database file, if you are using Sqlite.
You can use the command-line command:
```
    ICACLS "C:\ProgramData\Nucleus" /grant "IIS AppPool\NucleusAppPool:(OI)(CI)F"
```
7.  Assign 'Read' permissions to your installation folder for the `IIS AppPool\[AppPool Name]` user. 
You can use the command-line command:
```
    ICACLS "[your-installation-folder]" /grant "IIS AppPool\NucleusAppPool:(OI)(CI)RX"
```

7. If you want to use a different database or file system provider, refer to the sections below.
8. Browse to your web site address.  The new site wizard will appear, prompting you to set your site properties and administrator users.

## Using a different database provider
1. Create a database in your database server.
2. In your installation folder, create a new database configuration file.  If you are setting up a production environment, the file should be named 
databaseSettings.Production.json.  If you are setting up a development environment, name your file databaseSettings.Development.json.
3. Edit your new file and add the following, substituting your SQL server name, database name and credentials:
```json
    {
      "$schema": "./nucleus.schema.json",
      "Nucleus": {
        "Database": {
          "Connections": [
            // Database connections are available to the core and modules, but must be configured in the Schemas section in order to be used.
            {
              "Key": "nucleus",
              "Type": "SqlServer",
              "ConnectionString": "Data Source=DATABASE-SERVER;Initial Catalog=DATABASE-NAME;User ID=SQL-USERNAME;Password=SQL-PASSWORD"
            }
          ],
          "Schemas": [
            {
              "Name": "*",
              "ConnectionKey": "nucleus"
            }
          ]
        }
      }
    }
```
  4.  The process is the same if you want to use MySql, MariaDB or PostgreSQL, but the connection type (shown as "SqlServer" in the example above) will change, as does
  the format of the connection string.

  | Database          | Type       | Connection String                                                                                                                          |
  | ---------         | ---------- | ----------------------------                                                                                                               |
  | Sql Server        | SqlServer  | Data Source=DATABASE-SERVER;Initial Catalog=DATABASE-NAME;User ID=DATABASE-USERNAME;Password=DATABASE-PASSWORD                             |
  | MySql             | MySql      | Server=DATABASE-SERVER;Database=DATABASE-NAME;uid=DATABASE-USERNAME;pwd=DATABASE-PASSWORD                                                  |
  | MariaDB           | MySql      | Server=DATABASE-SERVER;Database=DATABASE-NAME;uid=DATABASE-USERNAME;pwd=DATABASE-PASSWORD                                                  |
  | PostgreSQL        | PostgreSql | Server=DATABASE-SERVER;Database=DATABASE-NAME;User Id=DATABASE-USERNAME;Password=DATABASE-PASSWORD;                                        |
  | Azure Sql Server  | SqlServer  | Use the connection string from Azure Portal.  Select your database, and click "Show database connection strings" in the overview page.     |

  Note: The database type for MariaDb is 'MySql'.  MariaDb is based on MySql and uses the same database provider.

  If your database administrator provides a connection string in a different format, you should use the format that they provide - the connection strings above are just examples.  

## Using a different File System provider
If you want to use the Azure Blob Storage file system provider, you can either add it, so that both the Azure Blob Storage and Local File System providers are available, or
replace the existing setting so that only the Azure Blob Storage file system provider is available.  If you are hosting in an Azure App Service, you should not use the 
Local File System provider.  You can add another storage provider after you have set up the site.

To configure your file system providers:
1. In your installation folder, create a new application configuration file.  If you are setting up a production environment, the file should be named 
appSettings.Production.json.  If you are setting up a development environment, name your file appSettings.Development.json.
2. Edit your new file and add the following, substituting your SQL server name, database name and credentials:

```json
    "FileSystems": {
      "Providers": [
        // File providers have a key, name, provider type and root folder.  You can specify multiple file providers, and the user
        // will be presented with a list.  The "Name" property is shown to the user.  Each entry has a key which uniquely identifies 
        // the provider entry.
        {
          "Key": "Azure",
          "Name": "Azure",
          "ProviderType": "Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider,Nucleus.Extensions.AzureBlobStorageFileSystemProvider",
          "ConnectionString": "STORAGE_ACCOUNT_CONNECTIONSTRING"
        },
        {
          "Key": "local",
          "Name": "Local",
          "ProviderType": "Nucleus.Core.FileSystemProviders.LocalFileSystemProvider,Nucleus.Core",
          "RootFolder": "{DataFolder}//Content"
        }
      ]
```

Replace the STORAGE_ACCOUNT_CONNECTIONSTRING value for the Azure Blob Storage connection string with the value in Azure Portal.  In Azure Portal, navigate to Settings > Access keys 
in your storage account's menu blade to see connection strings for both primary and secondary access keys (click the "Show Keys" button).

If you are adding the Azure Blob Storage provider, you must also install the Azure Blob Storage provider extension.

If you don't want the local file system provider, remove that section, including the comma between sections.  If you are using the local file system provider, the {DataFolder} token
refers to %PROGRAMDATA%\Nucleus.  You can change this value if you need to, but it should not be set to a path within your web root (installation folder) in order to ensure that
access to files is controlled by Nucleus - otherwise IIS may serve static files without Nucleus being able to check permissions.

The "Key" value is saved in the database when you add files and folders, so you can't change it later.  The "Name" is shown on-screen, and you can change it any time.
