## Bing Custom Search Extension
The Bing Custom Search extension uses the [Bing Custom Search](https://www.customsearch.ai/) service to retrieve search results from Bing and 
display them in your site. To set it up, you'll need a Bing Custom Search Configuration ID and an API Key from [Azure Portal](https://portal.azure.com/).

> You should review [Bing Search Service Pricing](https://www.microsoft.com/en-us/bing/apis/pricing) before you set up the service.

The Bing Custom Search extension settings are accessed in the `Manage` control panel.

![Bing Custom Search Settings](bingcustomsearchsettings.png)

## Settings

{.table-25-75}
|                            |                                                                                      |
|----------------------------|--------------------------------------------------------------------------------------|
| API Key                    | Get your API Key from the Azure Portal. Refer to the Azure Portal (API Key) section below for more details. |
| Custom Configuration ID    | Get your Bing Custom Search Configuration ID from [https://www.customsearch.ai/](https://www.customsearch.ai/). Refer to the Custom Search Configuration section below for more details.|
| Safe Search                | Configures [SafeSearch filtering](https://support.microsoft.com/en-au/topic/turn-bing-safesearch-on-or-off-446ebfb8-becf-f035-9eea-b660e8420458). Options are: Off, Moderate and Strict. |

## Custom Search Configuration
Use the [Bing Custom Search Portal](https://www.customsearch.ai/) to set up a custom search instance, then click the `Summary` tab to get your Custom Configuration ID. 

## Azure Portal (API Key)
Create a Bing Custom Search resource in [Azure Portal](https://portal.azure.com/). Once it has been provisioned, view the service details in Azure Portal and click `Manage Keys` in 
the Overview page to get your API key.

## Supported Capabilities
The Bing Custom Search search provider is limited by the features of the Bing Custom Search service, and does not support all of the capabilities 
of the Nucleus [Search Module](/documentation/modules/search/).

{.table-sm}
| Capability                   | Supported?                                                                           |
|------------------------------|--------------------------------------------------------------------------------------|
| Search Suggestions           | No                                                 |
| Filter By Scope              | No                                                 |
| Maximum Page Size            | 250                                                |
| Meta-data Display            |                                                    |
| - Categories                 | No                                                 |
| - Result Score               | No                                                 |
| - Size                       | No                                                 |
| - Published Date             | No                                                 |
| - Resource Type              | No                                                 |
| - Matched Terms Highlighting | No                                                 |



