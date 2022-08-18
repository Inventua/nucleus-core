A Nucleus extension is installed by installing a **package** using the `Extensions` page.

An extension package is a zip file which contains all of the files needed for your extension, along with a Extension Packaging manifest `package.xml` file with instructions for Nucleus
on how to install your components.  If you use one of the Nucleus Visual Studio project templates, your project file will contain MSBuild commands which will automatically create a 
package (zip) file when you build your project using `Release` configuration.

> **_Tip:_**  Set the build action of files in your extension project to `Content` in order to have them included in the package automatically. 
Also, any assemblies which are listed within a `<folder path="bin">` element in your package file are automatically included in the package zip. 

> Your extension is installed into the extensions\ folder, in the sub-directory specified by the folderName attribute of your `<component>` element.  

### Sample package.xml
Replace `{generate-guid}` with a guid that you have generated yourself.  There are many web sites including [https://www.guidgen.com/](https://www.guidgen.com/) which can generate Guids 
for you, as well as [Visual Studio extensions](https://marketplace.visualstudio.com/search?term=insert%20guid&target=VS&category=Tools&vsVersion=&subCategory=All&sortBy=Relevance), and the `uuidgen` command line tool available from the developer command prompt in Visual Studio.

``` 
<?xml version="1.0" encoding="utf-8" ?>
<package id="{generate-guid}" xmlns="urn:nucleus/schemas/package/1.0">
  <name>My Sample Extension</name>
  <version>1.0.0</version>
  <publisher name="your-company" url="http://your-site.com" email="support@your-domain" />
  <description>
    Sample Description.
  </description>
  <compatibility minVersion="1.0.0.0" maxVersion="1.*" />
  
  <components>
    <component folderName="MyExtension" optional="false">
      <file name="myextension.css" />
      
      <folder name="Bin">
        <file name="MyExtension.dll" />        
      </folder>
      
      <folder name="Views">
        <file name="Viewer.cshtml" />
        <file name="Settings.cshtml" />
      </folder>      
    </component>
  </components>  
</package>
```


### Manifest (package.xml)

## `<package>`
The root element of a manifest file is the `<package>` element.  It must have a unique id (guid) and must include the `xmlns="urn:nucleus/schemas/package/1.0"` attribute.

The package element contains:

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| id               | Yes       | (attribute) A unique id (guid) to identify your installation package. |
| name             | Yes       | (element)   Display name for your extension. |
| publisher        | Yes       | (element)   Specifies publisher/support information for your extension.  This is displayed to users during the installation process and in the extensions page. |
| description      | Yes       | (element)   Description for your package.  This is displayed to users during the installation process and in the extensions page. |
| compatibility    | No        | (element)   The compatibility element defines which Nucleus versions that the extension is compatible with. |
| components       | Yes       | (element)   The components element contains one or more `<component>` elements which provide installation instructions to Nucleus.  |

## `<publisher>` 

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| name             | No        | (attribute) Your company name. |
| url              | No        | (attribute) Your website url, or the Url for product information for your extension. |
| email            | No        | (attribute) A support email address for your extension.   |

## `<compatibility>` 

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| minVersion       | Yes       | (attribute) The minimum version of Nucleus supported by your extension in .NET System.Version format. |
| maxVersion       | No        | (attribute) The maximum version of Nucleus supported by your extension in .NET System.Version format.  |

The `maxVersion` attribute is not required, but is recommended.  You can use '*' in place of any parts of the version.


## `<components>` 
The `<components>` element wraps one or more `<component>` elements.

## `<component>` 

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| folderName       | Yes       | (attribute) Specifies the folder within the /extensions folder that your files will be installed to. |
| optional         | Yes       | (attribute) Specifies whether the user can choose whether to install the component. This feature is not yet implemented.  |

The `<components>` element can contain any combination of:  moduleDefinition, layoutDefinition, containerDefinition, controlPanelExtensionDefinition, 
file, folder, cleanup.

## `<moduleDefinition>` 
Modules must specify additional information for use by Nucleus.

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| id               | Yes       | (attribute) A unique id (guid) for your module. |
| friendlyName     | Yes       | (element)   The display name for your module.  This is displayed in the Nucleus user interface. |
| classTypeName    | Yes       | (element)   The assembly/class .NET class name for your controller class in the form classname,assembly.  |
| viewAction       | Yes       | (element)   The action in your controller class to call to display the end-user user interface.  |
| editAction       | No        | (element)   The action in your controller class to call to display the administrative/settings user interface.  |
| categories       | No        | (element)   A comma-separated list of module categories.  In the Nucleus add module page, modules are listed beneath each category that they are assigned to.  |

> The classTypeName is the assembly-qualified name for your controller class.  You need to include the class name (including namespace) followed by a comma, then the assembly name (without .dll).  You
do not need to include version, culture or the public key token.

## `<layoutDefinition>` 
Layouts must specify additional information for use by Nucleus.

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| id               | Yes       | (attribute) A unique id (guid) for your layout. |
| friendlyName     | Yes       | (element)   The display name for your layout.  This is displayed in the Nucleus user interface. |
| relativePath     | Yes       | (element)   The path to the layout view (razor page), relative to the root of your extension.  |

## `<containerDefinition>` 
Containers must specify additional information for use by Nucleus.

{.table-0-0-75}
| Name             | Required? | Description                                                                          |
|------------------|-----------|--------------------------------------------------------------------------------------|
| id               | Yes       | (attribute) A unique id (guid) for your container. |
| friendlyName     | Yes       | (element)   The display name for your container.  This is displayed in the Nucleus user interface. |
| relativePath     | Yes       | (element)   The path to the container view (razor page), relative to the root of your extension.  |

## `<controlPanelExtensionDefinition>` 
Control panel extensions must specify additional information for use by Nucleus.

{.table-0-0-75}
| Name             | Required? | Description                                                              |
|------------------|-----------|--------------------------------------------------------------------------|
| id               | Yes       | (attribute) Unique id (guid) for your control panel extension. |
| FriendlyName     | Yes       | (element)  Display name for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| Description      | Yes       | (element)  Description for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| ControllerName   | Yes       | (element)  The type name of your controller class.  You can omit the root namespace and `Controller` suffix. For example, if your class name is `MyExtension.MyControlPanelController`, you can just specify `MyControlPanel`. |
| ExtensionName    | Yes       | (element)  The name of the extension that the control panel extension belongs to.  This value must match the value of the `Extension()` attribute in your controller class and is used for MVC routing. |
| Scope            | Yes       | (element)  Specifies whether the control panel extension is added to the `Manage` or `Settings` control panel.  The allowed values are `Site` or `Global`.  If your control panel extension saves settings for the current site, choose `Site`.  If your settings are for the entire Nucleus instance, choose `Global`. |
| EditAction       | Yes       | (element)  The name of the Controller Action to run when the user selects the control panel extension.  This action should render a View. |

## `<file>` 
Specifies a file to copy during installation.  The file must be present in the package, in a folder location that matches the structure 
represented by the manifest's file/folder elements.  The file is copied to /Extensions/extension-name/folder, where folder is the root when 
the file element is a child of the `<components>`, or a sub-folder if the `<file>` element is a child of a `<folder>` element.

{.table-0-0-75}
| Name             | Required? | Description                                                              |
|------------------|-----------|--------------------------------------------------------------------------|
| name             | Yes       | (attribute) File name. |

## `<folder>` 
Specifies a folder.  `<folder>` elements can contain `<file>` elements or nested `<folder>` elements to represent the target folder structure within the /extensions/[folder-name] folder.

{.table-0-0-75}
| Name             | Required? | Description                                                              |
|------------------|-----------|--------------------------------------------------------------------------|
| name             | Yes       | (attribute) Folder name, relative to the component folder name. |

## `<cleanup>` 
Use the `<cleanup>` element to remove components during an upgrade when they are no longer needed.

The `<cleanup>` element can contain any number of moduleDefinition, layoutDefinition, containerDefinition, controlPanelExtensionDefinition, 
file or folder elements.  Items within the `<cleanup>` element are removed during installation.
