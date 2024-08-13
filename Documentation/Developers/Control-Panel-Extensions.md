Some extensions do not have a user interface for end users, but need to provide a way for site administrators to set configuration settings.  Use a 
control panel extension to add an item to the `Manage` or `Settings` control panel.

A Control Panel extension is an MVC Controller/ViewModel/View, just like a module.  A `<controlPanelExtensionDefinition>` entry in the manifest (package.xml) registers your control panel extension 
with Nucleus.  A simple example of a control panel extension is in the [Nucleus Google Analytics extension](https://github.com/Inventua/nucleus-core/tree/main/Nucleus.Core.Modules/Nucleus.Extensions.GoogleAnalytics), 
which is used to manage your site's Google Analytics ID.

### Example
```
<controlPanelExtensionDefinition id="{generate-guid}">
  <friendlyName>My Control Panel Extension</friendlyName>
  <description>Sample Control Panel Extension.</description>
  <controllerName>Settings</controllerName>
  <extensionName>My Extension</extensionName>
  <scope>Site</scope>
  <editAction>Settings</editAction>
</controlPanelExtensionDefinition>
```
Replace \{generate-guid}\ with your own GUID value.

#### Control Panel Extension Definition Values
{.table-25-75}
|                  |                                                                                      |
|------------------|--------------------------------------------------------------------------------------|
| id               | Unique id (guid) for your control panel extension. |
| friendlyName     | Display name for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| description      | Description for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| controllerName   | The name of your controller class.  You can omit the root namespace and `Controller` suffix. For example, if your class name is `MyExtension.MyControlPanelController`, you can just specify `MyControlPanel`. |
| extensionName    | The name of the extension that the control panel extension belongs to.  This value must match the value of the `Extension()` attribute in your controller class and is used for MVC routing. |
| scope            | Specifies whether the control panel extension is added to the `Manage` or `Settings` control panel.  The allowed values are `Site` or `Global`.  If your control panel extension saves settings for the current site, choose `Site`.  If your settings are for the entire Nucleus instance, choose `Global`. |
| editAction       | The name of the Controller Action to run when the user selects the control panel extension.  This action should render a View. |

> Element names are case-sensitive, and must appear in the order shown above.

> If your control panel extension settings view has a header (H1...H6) element with a `class="nucleus-control-panel-heading"`, Nucleus will automatically
set the control panel or dialog heading to the value of your header element and will remove the original header element.  This is recommended, 
as it increases the vertical space available for your settings view contents.

> Control Panel Extensions with a `Site` scope can save simple site-related settings using [Context.Site.SiteSettings](/api-documentation/Nucleus.Extensions.xml/Nucleus.Extensions.SiteSettingsExtensions/) - get 
a reference to the current [Context](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.Context/) by including 
it as a parameter in your controller constructor. Control Panel Extensions with a ==Global== scope need to implement their own data provider 
to save settings to extension-specific database tables, because global settings don't belong to a specific site (control panel extensions 
with a ==Site== scope can do this too, if required). See [Saving Settings](/developers/saving-settings/#site-settings) for more information.