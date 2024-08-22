## Azure Blob Storage Extension
The Azure Blob Storage extension is a file system provider which allows you to use the [Microsoft Azure Blob storage service](https://azure.microsoft.com/en-us/services/storage/blobs/) with Nucleus.

> Your Azure Blob Storage service is configured in the Microsoft [Azure Portal](https://portal.azure.com/).

The easiest way to set up the Nucleus Azure Blob Storage extension is during setup,  using the [Setup Wizard](/getting-started/#setup-wizard). If you use the Setup Wizard, 
you don't need to follow the steps below.

To use Azure Blob Storage, install the Azure Blob Storage extension, then add a configuration section for the Azure Blob Storage file system provider to the 
Nucleus:FileSystems:Providers section of your appSettings.\{environment\}.json file (normally ==appSettings.Production.json==). After you have set up your Azure 
Blob Storage service in Azure Portal, you can get your connection string from the Azure Portal/Blob Storage/Access Keys page. The provider type name for the 
Azure Blob Storage file system provider is 
`Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider, Nucleus.Extensions.AzureBlobStorageFileSystemProvider`.

### Limitations and Features:
- In the Azure Blob Storage service, files are stored within `Containers`. The Nucleus Azure Blob Storage extension is able to create containers. Container names
can contain only letters, numbers, and dashes.  
- The Azure Blob Storage service does not support folder or file rename operations.
- The Azure Blob Storage does not support the last-modified date for folders, so the last-modified column values will be blank in the file manager for folders.

{.file-name}
### Nucleus:FileSystems:Providers
```
{
  "Key": "Azure",
  "Name": "Azure",
  "ProviderType": "Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider,Nucleus.Extensions.AzureBlobStorageFileSystemProvider",
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR-ACCOUNT-NAME;AccountKey=YOUR-ACCOUNT-KEY;EndpointSuffix=core.windows.net"
}
```