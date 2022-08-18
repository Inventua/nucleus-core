Other types of extensions include Client-side libraries, [Event handlers](/developers/event-handlers), 
[File System Providers](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.FileSystemProviders.FileSystemProvider/), 
[Search Index Managers](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Search.ISearchIndexManager/) and 
[Search Providers](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Search.ISearchProvider/).  You could also 
create custom [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write), .

> __Note__: In a Startup class, you aren't limited to adding Nucleus services to dependency injection, you can do anything you like. But 
be careful, the actions that you execute in a startup class affect all of Nucleus, so you could potentially prevent Nucleus from 
successfully starting.
