## Azure Blob Storage Extension
The Azure Blob Storage extension is a file system provider which allows you to use the [Microsoft Azure Blob storage service](https://azure.microsoft.com/en-us/services/storage/blobs/) with Nucleus.

> The Azure Blob Storage file system provider is configured in application configuration files.  The [Getting Started](https://www.nucleus-cms.com/Getting-Started/) 
guide contains instructions for configuring file system providers.

> Azure Blob Storage is configured in the Microsoft [Azure Portal](https://portal.azure.com/).

To use Azure Blob Storage, install the Azure Blob Storage extension, then add a configuration section for the Azure Blob Storage file system provider.  The 
[Getting Started](/Getting-Started/#using-a-different-file-system-provider) guide describes how to configure file system providers.  The provider type name for the Azure Blob Storage file system provider is 
`Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider, Nucleus.Extensions.AzureBlobStorageFileSystemProvider`.