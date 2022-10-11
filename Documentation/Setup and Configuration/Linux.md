# Hosting in Linux 
> **_NOTE:_**   We have done some testing with Nucleus hosted in Linux, but this work is not complete.  This documentation page is also not 
complete.  If you are an experienced Linux user, and disagree with any of the recommendations below, you can adjust Nucleus settings to suit 
your environment.

1. Refer to the [Microsoft documentation](https://docs.microsoft.com/en-us/dotnet/core/install/linux) for instructions on installing .NET core in 
your Linux environment.

2. Install Nucleus.  Depending on which Linux distribution you are using, the installation folder may vary.  Un-zip the install set and upload to 
your installation folder.  You may need to set up an Ftp service in order to do this.

3. Install and configure Apache server, or Nginx.  \
\
[Host ASP.NET Core on Linux with Nginx](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-6.0) \
[Host ASP.NET Core on Linux with Apache](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-apache?view=aspnetcore-6.0).

4. Configure Nucleus.  In your installation folder create a new application configuration file.  If you are setting up a production environment, the file should be named 
appSettings.Production.json.  If you are setting up a development environment, name your file appSettings.Development.json.  The file may already exist - if it does, edit the
existing file.

3. Edit your configuration file and add the following:  If the Nucleus section already exists, add or edit the existing section:
```json
    {
      "$schema": "./nucleus.schema.json",
      "Nucleus": {
        "FolderOptions": {
         "DataFolder": "/home/nucleus/data"
      },
    }
```
Modify the DataFolder setting as required.  This folder is where logs, temporary files and (if you are using Sqlite) the database resides.  You should 
not set the data folder to the same folder as the install folder, or to a subdirectory of the install folder.  The folder must allow the application 
full permissions.