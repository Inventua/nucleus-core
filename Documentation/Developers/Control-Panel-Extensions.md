Some extensions do not have a user interface, but need to provide a way for site administrators to set configuration settings.  Use a control panel extension to add an item to the 
Manage or Settings control panel.

A Control Panel extension is an MVC Controller/ViewModel/View, just like a module.  A `<controlPanelExtensionDefinition>` entry in the manifest registers your control panel extension 
with Nucleus.  A simple example of a control panel extensions is the one that is part of the [Nucleus Google Analytics extension](/other-extensions/google-analytics/), which is used to 
manage your site's Google Analytics ID.

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

#### Control Panel Extension Definition Values
|                  |                                                                                      |
|------------------|--------------------------------------------------------------------------------------|
| id               | Unique id (guid) for your control panel extension. |
| FriendlyName     | Display name for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| Description      | Description for your control panel extension.  This is shown on-screen in the `Manage` or `Settings` control panel. |
| ControllerName   | The name of your controller class.  You can omit the root namespace and `Controller` suffix. For example, if your class name is `MyExtension.MyControlPanelController`, you can just specify `MyControlPanel`. |
| ExtensionName    | The name of the extension that the control panel extension belongs to.  This value must match the value of the `Extension()` attribute in your controller class and is used for MVC routing. |
| Scope            | Specifies whether the control panel extension is added to the `Manage` or `Settings` control panel.  The allowed values are `Site` or `Global`.  If your control panel extension saves settings for the current site, choose `Site`.  If your settings are for the entire Nucleus instance, choose `Global`. |
| EditAction       | The name of the Controller Action to run when the user selects the control panel extension.  This action should render a View. |

> If your control panel extension settings view has a header element with a `class="nucleus-control-panel-heading"`, at run-time, Nucleus will automatically set the control panel editing area heading - which is in the same line
as the panel close and black controls - to the value of your header element and will remove the original header element.  This is recommended, as it increases the vertical space available for your settings view contents.

> Control Panel Extensions with a `Site` scope can save simple site-related settings using `Nucleus.Abstractions.Models.Context.Site.SiteSettings` - get a reference to 
the current `Nucleus.Abstractions.Models.Context` by including it as a parameter in your controller constructor . Control Panel Extensions with a `Global` scope would need to 
implement a data provider to save settings to extension-specific database tables (control panel extensions with a `Site` scope can do this too, if required).  