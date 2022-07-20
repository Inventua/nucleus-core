# Configuration Files 
Nucleus configuration files are stored in your Nucleus application folder.  The default Nucleus configuration files are in .json format.

Configuration settings can be split across multiple .json files, and you can place any setting in any .json file.  If a setting is duplicated in
multiple files, the last one read takes precedence.

Nucleus automatically loads configuration in this order:

1. hosting.json
2. hosting.`{environment}`.json
3. appSettings.json
4. appSettings.`{environment}`.json
5. databaseSettings.json
6. databaseSettings.`{environment}`.json

7. All other .json files in the application folder, in alphabetical order.

8. Enviromment variables
9. Command line arguments

> **_NOTE:_**    Enviroment-specific settings are commonly used in order to run with different settings in your production, development or testing 
> environments.  Where possible, a best practise is to always leave the default `appSettings.json` and `databaseSettings.json` files as-is, and make all of your changes
> in enviroment-specific json files.
> The `{enviroment}` used in configuration filenames can be configured in a variety of ways.
> Refer to [Use multiple environments in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments) for more information.

You can organize your application settings any way you want to, the sections below refer to the default conventions used by Nucleus.

## hosting.json
Use the `hosting.json` file to configure the Urls that Nucleus listens on.  The urls setting is a standard .NET core setting.

```
{
  "urls": "http://0.0.0.0:5000",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true
  }
}
```

You can also configure IISSettings, [IISServerOptions](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.iisserveroptions) 
and [IISOptions](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.iisoptions).

> **_NOTE:_**    If you are hosting using Internet Information Services (IIS), Apache, Nginx or another reverse proxy server, the urls setting is ignored.

## appSettings.json 
The `appSettings.json` file contains most of the settings for Nucleus, and can also contain standard ASP.NET Core settings.  The default appSettings.json
file is configured with settings which should suit most users.

## databaseSettings.json
The `databaseSettings.json` file contains database connection information and schema-to-database-connection mappings.

```
{
  "$schema": "./nucleus.schema.json",
  "Nucleus": {
    "Database": {
      "Connections": [
        // Database connections are available to the core and modules, but must be configured in the 
        // Schemas section in order to be used.
        // Connection Properties:
        //   Key:               Uniquely identifies the connection.  Can be any value.  This value is used 
        //                      to match schemas to connections in the Schemas section.
        //   Type:              Must match the type string specified by the Nucleus database provider.
        //   ConnectionString:  Is a database provider specific connection string.
        {
          "Key": "CoreSqlite",
          "Type": "Sqlite",
          "ConnectionString": "Data Source={DataFolder}//Data/Nucleus.db"
        },
        {
          "Key": "CoreSqlServer",
          "Type": "SqlServer",
          "ConnectionString": "Data Source=<server>;Initial Catalog=<database>;User ID=<username>;Password=<password>"
        }
      ],
      "Schemas": [
        // In theory, different modules (which define their own schemas) can use different database 
        // connections.  In practise, most modules will use the default connection, which has the name "*".  Enterprise 
        // modules which communicate with another database may need to use their own connection.  Use the Name 
        // property to define which connection to use for each schema.  The special schema name "*" is the default 
        // connection used when there isn't a specific connection defined for a schema.  ConnectionKey must match one 
        of the Database:Connections:Key values above.
        {
          "Name": "*",
          "ConnectionKey": "CoreSqlite"
        }
      ]
    }
  }
}
```

# IIS Configuration

## web.config
If you are hosting Nucleus in Internet Information Services, you need to configure the ASP.NET core module.  IIS Configuration settings are stored in `web.config`.
More information is available on the Microsoft web site - [ASP.NET Core Module](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module).

> **_TIP:_**    A default `web.config` file is included as part of the installation, which includes the settings that you need to run Nucleus in IIS.  In
most cases, you can use the default web.config file as-is.
