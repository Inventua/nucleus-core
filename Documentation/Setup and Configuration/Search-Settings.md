## Search Settings
To manage search settings, click "Manage", then choose "Search Settings".

## Properties
{.table-25-75}
|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| Api Key                          | Specifies the Api key to use when retrieving page content during indexing.  Pages will be rendered using the permissions which have been assigned to the Api key.  |
| Index Public Pages Only          | Specifies whether to index all pages regardless of permissions settings, or only pages which have a 'View' permission for the site's all users role.  When non-public pages are indexed, search results are filtered by role, so users do not see pages which they cannot view. |
| Index Public Files Only          | Specifies whether to index all files regardless of permissions settings, or only files whose folders have a 'View' permission for the site's all users role.  When non-public files are indexed, search results are filtered by role, so users do not see files which they cannot view.  |
| Use SSL to Retrieve Page Content | Specifies whether to use SSL when retrieving page content during indexing.  |

### Api Key
When the Nucleus page meta-data producer creates content for the search data feed, it "visits" the page in order to generate content for the index 
entry.  If you do not set an Api Key, all pages will be viewed as an anonymous user.  If you use an Api Key, you can control which "view roles" are 
assigned to the search feeder when retrieving page content.
