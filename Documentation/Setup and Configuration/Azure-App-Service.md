# Hosting in Azure App Service 
> **_NOTE:_**   This guide assumes that you are using Azure Sql Server as your database, and Azure App Storage as your file system.  

1.  If you don't already have one, [create an Azure free account](https://azure.microsoft.com/en-au/free). The free App Service tier isn't 
appropriate for a production site, but you can use it to get started, and upgrade your App Service tier later.

2.  Sign in to the [Azure portal](https://portal.azure.com/), click "App Services" and create an App service.

3.  Identify your app service IP address.  In the Azure portal App services page, click your App Service, then choose the 
"Networking" option under "Settings".  The IP address is displayed as "Inbound Address".  Copy the IP address, you will need this 
value for step 6.

4.  Select the "Configuration" menu item for your App Service, then click the "Default documents" link at the top of the 
page.  Remove all of the default documents, and add a single "/" default document.  Click Save at the top of the page.

5.  Return to the Azure portal home page, click "SQL Databases" and create a database.

6.  In the Azure portal SQL databases page, select your database, and click the "Set server firewall" link at the top of the 
page.  Add the IP address that you collected in step 3, then click "Save" at the top of the page.

7.  Get your database connection information by clicking the "Connection strings" link.  Note that the connection string 
password is not set in the displayed connection string - you must substitute the password that you entered when you created the
database.  The connection string is required for step 10.

8.  Connect to your App service using an FTP client.  You can get your FTP credentials and endpoint by using the 
"Deployment Center" option after selecting your App Service.  Click the "FTPS credentials" link at the top of the page to view 
FTP information.

9.  Un-zip the install package (zip file) locally, then upload the files to your Azure /site/wwwroot folder.

10.  Edit the `databaseSettings.json`{.file-name} file, or copy the existing `databaseSettings.json`{.file-name} file to 
`databaseSettings.Production.json`{.file-name} or `databaseSettings.Development.json`{.file-name}.  Paste your database 
connection string in to the CoreSqlServer connection string, and alter the existing Schemas connection key named 
"*" from "CoreSqlite" to "CoreSqlServer". 

11.  Create an Azure storage account.

12.  In Azure Portal, open your storage account, select "Access Keys" from the "Security+Networking" submenu and click the "Show Keys"
link.  Click the copy button next to the first connection string to copy it to the clipboard.

13.  Edit the `appSettings.json`{.file-name} file, or copy the existing `appSettings.json`{.file-name} file to 
`appSettings.Production.json`{.file-name} or `appSettings.Development.json`{.file-name}.  Locate the Nucleus/FileSystems/Providers 
section, remove the default provider and add your Azure storage account.  
```json
"FileSystems": {
  "Providers": [
    // File providers have a key, name, provider type and root folder.  You can specify multiple file providers, and the user
    // will be presented with a list.  The "Name" property is shown to the user.  Each entry has a key which uniquely identifies 
    // the provider entry.
    {
      "Key": "My-Azure-Storage",
      "Name": "My-Azure-Storage",
      "ProviderType": "Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider,Nucleus.Extensions.AzureBlobStorageFileSystemProvider",
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your-account-name;AccountKey=your-account-key;EndpointSuffix=core.windows.net",
      "RootPath": "my-container/my-nucleus-root-folder"
    }
  ]
```

> If you want to use a specific container, or container/sub-folder as your root for Nucleus file storage, set the `RootPath` configuration 
property to your container name (and optionally, a sub-folder).  If you want Nucleus to be able to access all containers and 
folders within your Azure storage service, you can set the `RootPath` value to an empty string, or omit the setting. 

14.  Nucleus writes logs, cache and other files to sub-folders of `%ProgramData%\Nucleus` by default.  In Windows, this 
is `C:\ProgramData\Nucleus`{.file-name}, which is an appropriate location for the files.  In an Azure App Service, 
`%ProgramData%\Nucleus`{.file-name} is mapped to `\local\ProgramData`{.file-name}, which is treated as temporary storage 
and is reset every time your Azure App Service is restarted.  This is not ideal for logs, so you should configure Nucleus 
to save log files within the Azure `%HOME%` folder.  
\
Edit the `appSettings.json`{.file-name} or `appSettings.Production.json`{.file-name} file.  Locate the FolderOptions section, 
which is commented out by default.  Remove the comments, and set Nucleus:FolderOptions:DataFolder to 
`{WebRootFolder}/App_Data/Nucleus`.  The `{WebRootFolder}` token is replaced automatically at run time.  

```
"Nucleus:" 
{
  "FolderOptions": {
    "DataFolder": "{WebRootFolder}/App_Data/Nucleus"
}
```  
> In an Azure App Service, `{WebRootFolder}` is replaced at run-time by `/site/wwwroot/`{.file-name}, so the logs would be written to 
`/site/wwwroot/App_Data/Nucleus/Logs`{.file-name}.  `/site/wwwroot/App_Data`{.file-name} is a safe location to use for logs and 
other Nucleus data because files stored within `/App_Data`{.file-name} are never directly served by IIS.  The 
`/site/wwwroot/`{.file-name} folder is the Azure App Service `%HOME%` directory, which Azure App Service treats as persistent 
storage.  

See Also:  [Understanding the Azure App Service file system](https://github.com/projectkudu/kudu/wiki/Understanding-the-Azure-App-Service-file-system)

15.  Get your site Url from the Azure portal by clicking "App services", then selecting your App Service.  Click the site Url to 
launch your site.

## Troubleshooting
If the site does not launch or returns an error, click the "App Service Logs" option in Azure Portal.  Enable 
Application Logging(Filesystem) and Web Server logging, then click "Log Stream" (in the left-hand menu).  Try to open your site
again, and error details will be shown in the Log Stream page.