## Advanced SiteMap Extension
The Advanced SiteMap extension replaces the built-in Nucleus component which generates a `sitemap.xml` file for use by external search 
engines.  It implements the Nucleus search index manager interface so that the generated site map can include additional entries from 
extensions which submit meta-data to the search system, like the Documents, Forums and Publish modules.

> The standard sitemap generator only generates entries for pages.  The The Advanced SiteMap extension generates entries for pages, files
and all extensions which provide meta-data to the search system.

> You must configure a search feeder scheduled task so that the Advanced SiteMap Extension is sent data from search content providers.