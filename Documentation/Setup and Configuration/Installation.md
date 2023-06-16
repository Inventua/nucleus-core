# Installing Nucleus
Nucleus supports multiple hosting environments, database providers and file system providers.  The default configuration is set up to run
in Windows/Internet Information Services using the Sqlite database provider and the local file system provider.

See also:  [Hosting in Linux](/manage/hosting/linux/)

## Basic Setup 
1. Download the install set (zip format) from the [downloads](/downloads) page.  For a new installation, you will need to download the 
Nucleus.[version].Install.zip file.

2. Create an installation folder, and un-zip the install set to that folder.
3. Install the [.Net Core Hosting Bundle](https://download.visualstudio.microsoft.com/download/pr/b84943df-6c61-4af8-91fc-a3981cd04eb8/69663d1ee22625a25d9f528b9dde0225/dotnet-hosting-6.0.18-win.exe).  You 
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
<kbd>ICACLS "C:\ProgramData\Nucleus" /grant "IIS AppPool\NucleusAppPool:(OI)(CI)F"</kbd>
7.  Assign 'Read' permissions to your installation folder for the `IIS AppPool\[AppPool Name]` user. 
You can use the command-line command:  
<kbd>ICACLS "[your-installation-folder]" /grant "IIS AppPool\NucleusAppPool:(OI)(CI)RX"</kbd>
8. If you want to use a different database or file system provider, refer to the sections below.
9. Browse to your web site address.  The new site wizard will appear, prompting you to set your site properties and create administrator users.

## Using a different database provider
1. Create a new empty database in your database server.
2. In your installation folder, create a new database configuration file.  If you are setting up a production environment, the file should be named 
`databaseSettings.Production.json`{.file-name}.  If you are setting up a development environment, name your file `databaseSettings.Development.json`{.file-name}. 
Refer to the [Configuration Files](https://www.nucleus-cms.com/configuration-files/) page for more information.
3. Edit your new file and add the following, substituting your SQL server name, database name and credentials:
```json
{
  "Nucleus": {
    "Database": {
      "Connections": [
        // Database connections are available to the core and extensions, but must be configured in the Schemas 
        // section in order to be used.
        {
          "Key": "nucleus",
          "Type": "SqlServer",
          "ConnectionString": 
            "Data Source=DATABASE-SERVER;Initial Catalog=DATABASE-NAME;User ID=SQL-USERNAME;Password=SQL-PASSWORD"
        }
      ],
      "Schemas": [
        {
          // A name of "*" makes this schema the default.  Using the root namespace of an extension as the name will 
          // make the schema apply to that extension only.  The ConnectionKey value must match a Key from the Connections
          // section.
          "Name": "*",
          "ConnectionKey": "nucleus"
        }
      ]
    }
  }
}
```

> If you are editing an existing configuration file which already contains a ==Database== section, update the existing section. 

The process is the same if you want to use MySql, MariaDB or PostgreSQL, but the connection type (shown as "SqlServer" in the example above) 
will change, as does the format of the [connection string](https://www.connectionstrings.com/).

### Database Types and Connection Strings
{.table-0-0-75}
| Database          | Type       | Connection String                                                                                                                          |
| ---------         | ---------- | ----------------------------                                                                                                               |
| Sql Server        | SqlServer  | Data Source=DATABASE-SERVER;Initial Catalog=DATABASE-NAME;User ID=DATABASE-USERNAME;Password=DATABASE-PASSWORD                             |
| MySql             | MySql      | Server=DATABASE-SERVER;Database=DATABASE-NAME;uid=DATABASE-USERNAME;pwd=DATABASE-PASSWORD                                                  |
| MariaDB           | MySql      | Server=DATABASE-SERVER;Database=DATABASE-NAME;uid=DATABASE-USERNAME;pwd=DATABASE-PASSWORD                                                  |
| PostgreSQL        | PostgreSql | Server=DATABASE-SERVER;Database=DATABASE-NAME;User Id=DATABASE-USERNAME;Password=DATABASE-PASSWORD;                                        |
| Azure Sql Server  | SqlServer  | Use the connection string from [Azure Portal](https://portal.azure.com).  Select your database, and click "Show database connection strings" in the overview page.     |

> The database type for MariaDb is 'MySql'.  MariaDb is [based on MySql](https://en.wikipedia.org/wiki/MariaDB) and uses the same database provider.

> If your database administrator provides a connection string in a different format, you should use the format that they provide - the connection strings above are just examples.  

## Using a different File System provider
In Windows and Linux, if you want to use the Azure Blob Storage or Amazon S3 file system provider, you can either add it, so that both the cloud (Azure/S3) storage and Local File System providers 
are available, or replace the existing setting so that only the Azure Blob Storage or Amazon S3 file system provider is available.  If you are hosting in an Azure App Service, you should 
not use the Local File System provider.  You can add additional storage providers after you have set up the site.

To configure your file system providers:
1. In your installation folder, edit your environment application configuration file, or create one if it does not exist.  If you are setting up a production environment, 
the file should be named `appSettings.Production.json`{.file-name}.  If you are setting up a development environment, name your file `appSettings.Development.json`{.file-name}.
2. Edit your new file and add a configuration section for your file system provider (to the Nucleus section).  You can remove or comment out the 
default ('local') file system provider if you don't want to use local file storage.

```json
"Nucleus": 
[
  "FileSystems": 
  {
    "Providers": 
    [
      // File providers have a key, name and provider type.  You can specify multiple file providers, 
      // and the user will be presented with a list.  The "Name" property is shown to the user.  Each entry has a 
      // key which uniquely identifies the provider entry.  You should not change provider keys after you have 
      // created folders and files, because it is part of the path identifier that is saved in the database, but you can
      // change a provider Name property because its value is only used for on-screen display.
      {
        "Key": "local",
        "Name": "Local",
        "ProviderType": "Nucleus.Core.FileSystemProviders.LocalFileSystemProvider,Nucleus.Core",
        "RootPath": "{DataFolder}//Content"
      },
      // You should only include this section if you are using Azure storage
      {
        "Key": "Azure",
        "Name": "Azure",
        "ProviderType": 
          "Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider,Nucleus.Extensions.AzureBlobStorageFileSystemProvider",
        "ConnectionString": "STORAGE_ACCOUNT_CONNECTIONSTRING"
      },
      // You should only include this section if you are using Amazon S3
      {
        "Key": "AmazonS3",
        "Name": "Amazon S3",
        "ProviderType": "Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider",
        "AccessKey": "YOUR-ACCESS-KEY",
        "Secret": "YOUR-SECRET",
        "ServiceUrl": "AMAZON-S3-REGION-URL",
        "RootPath": "YOUR-BUCKET-NAME"
      }
    ]
  }
]
```

If you are using [Azure storage](https://azure.microsoft.com/en-us/products/storage/blobs/), replace the ==STORAGE_ACCOUNT_CONNECTIONSTRING== value for the Azure Blob Storage connection 
string with the value from Azure Portal.  In Azure Portal, navigate to Settings > Access keys in your storage account's menu blade 
to see connection strings for both primary and secondary access keys (click the "Show Keys" button).

If you are using [Amazon S3](https://aws.amazon.com/s3/), replace the ==YOUR-ACCESS-KEY==, ==YOUR-SECRET==, ==AMAZON-S3-REGION-URL== and ==YOUR-BUCKET-NAME== 
with values from the AWS console.  Log in to the console, click `Services`, scroll down and click the `Storage` menu item on 
the left, and select `S3` from the menu.  Once you have set up an S3 service and created a bucket, you can click `Access Points` 
in the S3 menu to view your service settings.  You will also need to use the Amazon AWS IAM dashboard to 
[create your access key](https://docs.aws.amazon.com/general/latest/gr/aws-sec-cred-types.html#access-keys-and-secret-access-keys) 
and shared secret.

> For Azure storage and Amazon S3, you must install the relevant file system provider extension (that is, the 
Nucleus [Azure storage](https://www.nucleus-cms.com/other-extensions/azure-blob-storage/) file system provider, 
or the Nucleus [Amazon S3](https://www.nucleus-cms.com/other-extensions/amazon-s3/) provider) before adding settings to your 
configuration files.

The `RootPath` setting can be used to set the base path for Nucleus file storage.  This setting allows you to configure Nucleus 
to use a sub-folder within the local file system, [Azure storage](https://azure.microsoft.com/en-us/products/storage/blobs/) 
or [Amazon S3](https://aws.amazon.com/s3/).  Sites also use their individual [home directory](https://www.nucleus-cms.com/manage/site-settings#properties) 
within the `RootPath` that you have specified.
- If you are using Amazon S3, you should always specify a bucket name, and can also include a sub-folder path within the specified bucket.  The 
S3 file system provider can't create S3 buckets, only files and folders within the specified bucket.  An alternative would be to 
leave the RootPath empty, and specify a bucket name for each site's home directory.
- If you are using Azure storage, the root folder setting is optional.  The Azure storage file system provider can create and navigate 
Azure storage containers.
- The RootFolder setting for the local file system provider is optional, if it is not set, it uses the 
[DataFolder](https://www.nucleus-cms.com/configuration-reference#nucleusfolderoptions)/Content path as the root for file 
storage.  In Windows, the default Nucleus data folder is `C:\ProgramData\Nucleus`{.file-name}, so the default file system provider root path 
is `C:/ProgramData/Nucleus/Content`{.file-name}.
