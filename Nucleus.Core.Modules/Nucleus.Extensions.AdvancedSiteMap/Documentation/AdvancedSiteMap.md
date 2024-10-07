## Advanced SiteMap Extension
The Advanced SiteMap extension replaces the built-in Nucleus component which generates a `sitemap.xml` file for use by external search 
engines like Google and Bing.  

The built-in sitemap generator only generates entries for pages.  The Advanced SiteMap extension generates entries for pages, files
and all extensions which provide meta-data to the search system. The Advanced SiteMap extension is not a search provider, but it implements the 
Nucleus search index manager interface so that the generated site map can include additional entries from extensions which submit 
meta-data to the search system, like the Documents, Forums and Publish modules.

The Advanced SiteMap extension doesn't have any settings, it works automatically after it has been installed.

> You must configure a search feeder scheduled task so that the Advanced SiteMap Extension receives data from search content providers. The scheduled 
task settings are the same as for the [Elastic Search](/other-extensions/elastic-search/#search-feeder-scheduled-task) extension.