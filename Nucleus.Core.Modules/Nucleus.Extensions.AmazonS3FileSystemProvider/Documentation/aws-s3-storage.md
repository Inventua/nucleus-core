## Amazon S3 File System Provider Extension
The Amazon S3 File System Provider extension is a file system provider which allows you to use the [Amazon S3 storage service](https://aws.amazon.com/s3/) with Nucleus.

> The Amazon S3 file system provider is configured in application configuration files.  

To use Amazon S3 Storage, install the Amazon S3 File System Provider extension, then add a configuration section for the Amazon S3 file system 
provider.  The provider type name for the Azure Blob Storage file system provider is 
`Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider`.

### Limitations and Features:
 -The Amazon S3 File System Provider extension does not support creating buckets.  Create a bucket in the AWS console.  
- If your AWS account only has one bucket, the Amazon S3 File System Provider extension automatically navigates to it and does not show the top level (list of buckets).
- Amazon S3 is case-sensitive.  To avoid confusion, the Amazon S3 File System Provider extension always converts new folder names and uploaded file names to lower 
case.  Any pre-existing folder or files in your S3 bucket are not changed.
- Amazon S3 does not support objects (other than "buckets") at the "top level", you must navigate within a bucket to create folders and upload files.
- Amazon S3 does not support folder or file rename operations.
- Amazon S3 does not support the last-modified date for folders, so the last-modified column values will be blank in the file manager for folders.

{.file-name}
### Nucleus:FileSystems:Providers
```
{
  "Key": "AmazonS3",
  "Name": "Amazon S3",
  "ProviderType": 
    "Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider",
  "AccessKey": "your-access-key",
  "Secret": "your-secret",
  "ServiceUrl": "http://s3.[region].amazonaws.com"
}
```
