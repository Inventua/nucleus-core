## Google Custom Search Extension
The Google Custom Search extension provides a search provider using the Custom Search API to retrieve search results.  

The extension uses the Google Custom Search API to deliver search results for your site. To set it up, you'll need a Search Engine ID and an
API Key to interact with the Custom Search API. Refer to the Settings on how to obtain the Search Engine ID and API Key.

The Google Custom Search extension settings are accessed in the `Manage` control panel.

![Google Custom Search Settings](googlecustomsearchsettings.png)

## Settings

{.table-25-75}
|                               |                                                                                      |
|-------------------------------|--------------------------------------------------------------------------------------|
| Programmable Search Engine ID | Visit [Programmable Search Engine site](https://programmablesearchengine.google.com/controlpanel/all) to generate a Search engine ID. |
| API Key                       | Get the Google API Key via the [Cloud console](https://console.cloud.google.com/apis/credentials). Refer to the API section below for more details. Refer to the Programmable Search Engine below for more details. |
| Safe Search                   | Configure SafeSearch filtering for your search engine. |

## Programmable Search Engine 
Generate your Search Engine ID by creating a search engine.  The Google Programmable Search Engine [control panel documentation](http://support.google.com/programmable-search/) 
provides more information on customizing the Custom Search API on retrieving information from your sites. You can specify areas of your site with the possibilities of 
including and excluding pages or sites in your configuration.  It can be also be customized with additional security, UI, statistics and logging features. 

## API Key
You will need to create credentials in the form of an API key to access the Custom Search API. The API key can be create via the [Cloud console](https://console.cloud.google.com/apis/credentials)
and for security can be set limited access using application restriction and/or API restriction.  You can read more on the recommendations 
for [securing an API Key](https://cloud.google.com/docs/authentication/api-keys).




