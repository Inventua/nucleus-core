
## Module Migration Notes

### Migrating to the Nucleus Links module.  
The DNN links module settings "Control Type", "List Display Format" "Display Info Link", "Wrap Links" and "Display Icon" settings 
are not migrated.  DNN links of type "User" are not migrated.

### Migrating to the Nucleus SiteMap module.  

#### Inventua TopMenu

The css-currentitem, css-highlight, css-item, css-menubar, DisableJavascript, ExcludeAdmin, IgnoreDisabled, IgnoreHidden, 
seperator and ShowIcons settings are not supported.

Instances of TopMenu set the SiteMap "Direction" to horizontal.

The selectedTab and ShowDescriptions are used.

The "UseName" setting is not currently support, but a value is set, as this setting is likely to be supported in the future.


#### Inventua SideMenu

The AppendTabLevelToClass, css-currenthdr, css-currentitem, css-header, css-headerhighlight, css-highlight, css-item, 
DisableJavascript, IgnoreDisabled, IgnoreHidden, ShowIcons, TrailingHR, TreatTopLevelAsHeader and Seperator settings
are not supported.

The CurrentSubTreeOnly, Levels, selectedTab, ShowDescriptions are used.

The "UseName" setting is not currently support, but a value is set, as this setting is likely to be supported in the future.
