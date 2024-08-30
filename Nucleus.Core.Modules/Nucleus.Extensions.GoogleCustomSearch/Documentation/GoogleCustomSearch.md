## Google Custom Search Extension
The Google Custom Search extension provides a search provider using the Custom Search API to retrieve search results.  

The Google Custom Search extension uses the Google Custom Search API to deliver search results for your site. To set it up, you'll need to configure 
the Programmable Search Engine, which will provide you with a Search Engine ID and require a valid API Key to interact with the Custom Search API.

The Google Custom Search extension settings are accessed in the `Manage` control panel.

![Google Custom Search Settings](googlecustomsearchsettings.png)

## Settings

{.table-25-75}
|                               |                                                                                      |
|-------------------------------|--------------------------------------------------------------------------------------|
| API Key                       | Enter the Google API Key via the [Cloud console](https://console.cloud.google.com/apis/credentials). The key can call enabled Google APIs or restricted to selected APIs. |
| Programmable Search Engine ID | Visit [Programmable Search Engine site](https://programmablesearchengine.google.com/controlpanel/all) and add search engine details to generate a Search engine ID. |
| Safe Search                   | Enable SafeSearch filtering for your search engine. |

## API Key
You need to create credentials in the form of an API key to access the Custom Search API. The API key can be create via the [Cloud console](https://console.cloud.google.com/apis/credentials)
and for security can be set limited access using application restriction and/or API restriction.  You can read more on the recommendations for [securing an API Key](https://cloud.google.com/docs/authentication/api-keys?_gl=1*utft3f*_ga*ODMxOTc0NzQ2LjE3MTM0MTkzOTc.*_ga_WH2QY8WWF5*MTcyNDczMTQ1NS4zNi4xLjE3MjQ3MzI5OTAuMTYuMC4w#securing).

### Application Restrictions
Options for application restrictions on an API key are `'no restrictions'`, `'specific websites'`, `'IP Addresses'`, `'Android'` or `'IOS'` apps. Only one application restriction is applied per key.

### API Restrictions
The API key can also restrict access to specified APIs that have been enabled.

## Programmable Search Engine 
Google Programmable Search Engine allows the user to use the Custom Search API to search for information on your sites. The search engine can be customized for 
more relevant results.

### Search Features
Google Custom Search can be used to match specific areas of your site. Visit [Update sites in your search engine](https://support.google.com/programmable-search/answer/12397162) to 
view the possibilities of including and excluding pages or sites in your search engine.



