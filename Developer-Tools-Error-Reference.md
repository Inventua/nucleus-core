# CSS Reference

## Admin

Admin CSS classes in `Resources/css/admin.css` can be used by ==Settings== views and ==Control Panel Extensions==.

| Code                 | Source             | Description                                                                                     |
|----------------------|--------------------|-------------------------------------------------------------------------------------------------|
| NUCLEUS100           | MSBuild            | Your manifest (package.xml) file has an empty Id attribute.                                     |
| NUCLEUS101           | MSBuild            | Your manifest (package.xml) file has an empty name element.                                     |
| NUCLEUS102           | MSBuild            | Your manifest (package.xml) file has an empty version element.                                  |
| NUCLEUS103           | MSBuild            | Your manifest (package.xml) file has an invalid Id attribute (value is not a guid).             |
| NUCLEUS104           | MSBuild            | Your manifest (package.xml) file has an invalid version element.  The version element must contain a version number in the format 'major.minor[.build[.revision]]', where build and revision are optional. |
| NUCLEUS105           | MSBuild            | Your manifest (package.xml) file does not have a &lt;components&gt; element and will not install anything. |
| NUCLEUS106           | MSBuild            | Your manifest (package.xml) file does not have any &lt;component&gt; elements and will not install anything. |
| NUCLEUS110           | MSBuild            | Your manifest (package.xml) file refers to a file '{file-name}' which does not exist.                |
| NUCLEUS200           | MSBuild            | Your project references version '{version}' of package '{package-name}' but your manifest (package.xml) file has a compatibility element with a minVersion of {min-version}, which is less than {version}.  Either update your minVersion to '{version}' or downgrade your reference to '{package-name}' to version '{min-version}' or less. |
| NUCLEUS300           | Analyzers          | Controller class '{controller-name}' does not have an [Extension] attribute. A Nucleus.Abstractions.Extension attribute is required to facilitate Nucleus routing. |
| NUCLEUS301           | Analyzers          | Controller class '{controller-name}', method '{method-name}' looks like a controller action which updates data, but neither the '{controller-name}' class or the '{method-name}' method have an [Authorize] attribute. This may be a security risk because you are not checking user authorization for this action. |
