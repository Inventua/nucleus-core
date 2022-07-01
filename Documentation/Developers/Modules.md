In Nucleus, modules add functionality to your site.  A module adds on-screen content, and is a type of [extension](/develop-extensions/). Modules 
are made up of one or more .NET Core MVC Controllers, ViewModels and Razor Views and are typically developed using Visual Studio and C#.

## Project Templates
The easiest way to develop a module is to start with one of the Visual Studio templates provided by the Nucleus [Developer Tools](/downloads).  

There are three templates:

### Nucleus Complex Extension Project
Use the Nucleus Complex Extension Project template when you are developing a module that will save data in its own database tables.

Includes default controllers, a data provider, view models and views, a cache extension and manager class, a sample manifest file, project references to the Nucleus Nuget packages, 
project file entries which link to the Nucleus MSBuild script, and a ViewImports file to import the Nucleus namespaces used by modules.  

### Nucleus Simple Extension Project
Use the Nucleus Simple Extension project template when your module will only save simple module settings to the ModuleSettings table 
using built-in Nucleus classes.

Includes default controllers, view models and views, project references to the Nucleus Nuget packages, a sample manifest file, project file entries which link to the Nucleus 
MSBuild script, and a ViewImports file to import the Nucleus namespaces used by modules, but does not include a default data provider, cache extension or manager class.  

### Nucleus Empty Extension Project
You will generally use the Nucleus Empty Extension Project template when you are creating an extension which is not a module, like 
a package which contains layouts and containers, a search provider or a file system provider.

Includes project references to the Nucleus Nuget packages, a sample manifest file, project file entries which link to the Nucleus MSBuild script, and a ViewImports 
file to import the Nucleus namespaces used by modules, but no template controllers, views, data providers or other classes.  







