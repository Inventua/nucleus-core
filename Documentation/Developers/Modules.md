Modules add functionality to your site and are typically developed using Visual Studio and C#.  A Nucleus module is a .NET Core MVC project with Controllers, ViewModels and 
Razor Views.

## Project Templates
The easiest way to develop a module is to start with one of the Visual Studio templates provided by the Nucleus [Developer Tools](/downloads).  

There are three templates:

### Nucleus Complex Extension Project
Includes default controllers, a data provider, view models and views, a cache extension and manager class, a sample manifest file, project references to the Nucleus Nuget packages, 
project file entries which link to the Nucleus MSBuild script, and a ViewImports file to import the Nucleus namespaces used by modules.  Use the Nucleus Complex Extension Project 
template when you are developing a module that will save data in its own database tables.

### Nucleus Simple Extension Project
Includes default controllers, view models and views, project references to the Nucleus Nuget packages, a sample manifest file, project file entries which link to the Nucleus 
MSBuild script, and a ViewImports file to import the Nucleus namespaces used by modules, but does not include a default data provider, cache extension or manager class.  Use the 
Nucleus Simple Extension project template when your module will only save simple module settings using built-in Nucleus classes.

### Nucleus Empty Extension Project
Includes project references to the Nucleus Nuget packages, a sample manifest file, project file entries which link to the Nucleus MSBuild script, and a ViewImports 
file to import the Nucleus namespaces used by modules, but no template controllers, views, data providers or other classes.  You will generally use the Nucleus Empty Extension Project 
template when you are creating an extension which is not a module, like a package which contains layouts and containers, a search provider or a file system provider.








