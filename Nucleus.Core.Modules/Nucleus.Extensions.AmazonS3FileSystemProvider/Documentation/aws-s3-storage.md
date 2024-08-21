## Amazon S3 File System Provider Extension
The Amazon S3 File System Provider extension is a file system provider which allows you to use the [Amazon S3 storage service](https://aws.amazon.com/s3/) with Nucleus.

The easiest way to set up the Amazon S3 File System Provider extension is during setup,  using the [Setup Wizard](/getting-started/#setup-wizard). If you use the Setup Wizard, 
you don't need to follow the steps below.

To use Amazon S3 Storage, install the Amazon S3 File System Provider extension, then add a configuration section for the Amazon S3 file system 
provider.  The provider type name for the Azure Blob Storage file system provider is 
`Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider`.

### Limitations and Features:
- **The Amazon S3 File System Provider extension does not support creating buckets.  Create a bucket in the AWS console.** 
- If your AWS S3 service only has one bucket, the Amazon S3 File System Provider extension automatically navigates to it and does not show the top level (list of buckets).
- Amazon S3 is case-sensitive.  To avoid confusion, the Amazon S3 File System Provider extension always converts new folder names and uploaded file names to lower 
case.  Any pre-existing or manually-updated folders or files in your S3 bucket are not changed.
- The Amazon S3 service does not support objects (other than "buckets") at the "top level", you must navigate within a bucket to create folders and upload files.
- The Amazon S3 service does not support folder or file rename operations.
- The Amazon S3 service does not support the last-modified date for folders, so the last-modified column values will be blank in the file manager for folders.

{.file-name}
### Nucleus:FileSystems:Providers
```
{
  "Key": "AmazonS3",
  "Name": "Amazon S3",
  "ProviderType": 
    "Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider",
  "AccessKey": "YOUR-ACCESS-KEY",
  "Secret": "YOUR-SECRET",
  "ServiceUrl": "http://s3.[region].amazonaws.com",
  "RootPath": "YOUR-BUCKET-NAME"
}
```

Replace the ==YOUR-ACCESS-KEY==, ==YOUR-SECRET==, the service Url value and ==YOUR-BUCKET-NAME== 
with values from the AWS console.  Log in to the AWS console, click `Services`, scroll down and click the `Storage` menu item on 
the left, and select `S3` from the menu.  Once you have set up an S3 service and created a bucket, you can click `Access Points` 
in the S3 menu to view your service settings.  You will also need to use the Amazon AWS IAM dashboard to 
[create your access key](https://docs.aws.amazon.com/general/latest/gr/aws-sec-cred-types.html#access-keys-and-secret-access-keys) 
and shared secret.

For the Amazon S3 file system provider, you should generally set the `RootPath` setting.  You can set it to a bucket name, and you can also 
include a sub-folder path within the specified bucket.  The S3 file system provider can't create S3 buckets, only files and 
folders within the specified bucket, so the bucket must be created using the AWS console.  

An alternative configuration is to leave the RootPath empty, and make sure that you specify a bucket name for each site's 
[home directory](https://www.nucleus-cms.com/manage/site-settings#properties).

