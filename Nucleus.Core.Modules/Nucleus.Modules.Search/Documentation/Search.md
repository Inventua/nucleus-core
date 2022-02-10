## Search module
The search module lists provides a user interface for searching your site.  As your users type a search term, a "quick results" display shows the titles of search terms which match your search term.  The user clicks a result 
to open the page, file or other resource from your site.  

![Search Screenshot](Search.png)

Users can click `Search` to display results in full.

![Search Screenshot](Search-Results.png)

> The search module doesn't work on its own.  You need to install a search provider for the search module to use to get search results.  The Elastic Search extension includes a search provider.

You can control the full results display in the module settings page.

![Settings](Search-Settings.png)

## Settings
|                     |                                                                                      |
|---------------------|--------------------------------------------------------------------------------------|
| Show Categories     | Toggles whether to display categories (if the search result has any) in the full results display. |
| Show Published Date | Toggles whether to display the published date (if one applies to the result) in the full results display. |
| Show Size           | Toggles whether to display the result size in the full results display. |
| Show Score          | Toggles whether to display the result score in the full results display. |

> For search providers that support it, the result score is based on how well a specific result matches your search term, relative to all of the results in the index. 