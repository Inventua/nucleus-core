# Developer Tools Error Codes Reference

{.table-0-0-75}
| Code              | Source              | Description                                                                                     |
|-------------------|---------------------|-------------------------------------------------------------------------------------------------|
| NUCL001           | MSBuild             | Your project does not contain a manifest (package.xml) file.                                    |
| NUCL100           | MSBuild             | Your manifest (package.xml) file has an empty Id attribute.                                     |
| NUCL101           | MSBuild             | Your manifest (package.xml) file has an empty name element.                                     |
| NUCL102           | MSBuild             | Your manifest (package.xml) file has an empty version element.                                  |
| NUCL103           | MSBuild             | Your manifest (package.xml) file has an invalid Id attribute (value is not a guid).             |
| NUCL104           | MSBuild             | Your manifest (package.xml) file has an invalid version element.  The version element must contain a version number in the format 'major.minor[.build[.revision]]', where build and revision are optional. |
| NUCL105           | MSBuild             | Your manifest (package.xml) file does not have a &lt;components&gt; element and will not install anything. |
| NUCL106           | MSBuild             | Your manifest (package.xml) file does not have any &lt;component&gt; elements and will not install anything. |
| NUCL110           | MSBuild             | Your manifest (package.xml) file refers to a file '[file-name]' which does not exist.                |
| NUCL200           | MSBuild             | Your project references version '[version]' of package '[package-name]' but your manifest (package.xml) file has a compatibility element with a minVersion of [min-version], which is less than [version].  Either update your minVersion to '[version]' or downgrade your reference to '[package-name]' to version '[min-version]' or less. |
| NUCL300           | Controller Analyzer | Controller class '[controller-name]' does not have an [Extension] attribute. A Nucleus.Abstractions.Extension attribute is required to facilitate Nucleus routing. |
| NUCL301           | Controller Analyzer | Controller class '[controller-name]', method '[method-name]' looks like a controller action which updates data, but neither the '[controller-name]' class or the '[method-name]' method have an [Authorize] attribute. This may be a security risk because you are not checking user authorization for this action. |
