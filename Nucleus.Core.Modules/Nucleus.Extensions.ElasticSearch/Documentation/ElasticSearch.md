## Elastic Search Extension
The Elastic Search extension provides a search index manager and a search provider (it both feeds the search index, and retrieves search results).  

> The Elastic Search extension provides an interface to Elastic Search, but does not include the Elastic Search application. You need an 
[Elastic Search](https://www.elastic.co/) server to use the Elastic Search provider.  If you are setting up a new Elastic Search server, make sure to 
install the [Ingest Attachment plugin](https://www.elastic.co/guide/en/elasticsearch/plugins/current/ingest-attachment.html).

> Nucleus has built-in support for submitting pages and files to the index, and some modules (like the Documents module) also contain Search meta-data 
providers which submit search index entries. 

The Elastic Search extension settings are accessed in the `Manage` control panel.

![Elastic Search Settings](elasticsearch.png)

## Settings

{.table-25-75}
|                           |                                                                                      |
|---------------------------|--------------------------------------------------------------------------------------|
| Elastic Search Server Url | Enter the domain name or address of the Elastic Search server, including the scheme (http: or https:) and port.  The default port is 9200.  For example, https://192.168.1.100:9200. |
| Index Name                | Enter an index name.  The index is created automatically.  Index names must be lower case, may not  include \, /, *, ?, ", &lt;, &gt;, &#124;, (space character) or # and must be less than 255 bytes. |
| Attachments Size Limit    | You can specify an upper size limit (in mb) for documents which are submitted to the index.  Documents which are lareger than the size limit will have index entries containing meta-data only.  To specify no limit, enter zero (0). |
| Boost Settings            | You can increase or decrease the boost for some search index fields.  This influences the relevance of a document when you are searching, and results are sorted by relevance.  The default boost value for all fields is 1. |
|  - Title                   | Boost the page title, or the file name for files. |
|  - Summary                 | Boost the page summary (not relevant for files). |
|  - Categories              | Boost categories.  Page and file index entries do not currently set categories, but modules may set one or more categories for an index entry. |
|  - Keywords                | Boost page keywords (not relevant for files).   |
|  - Content                 | Boost the page or file content. Elastic search can parse a number of file formats, including office documents and PDFs.  |
|  - Attachment Fields       | (Title, Name, Author, Keywords) Some document formats contain embedded meta-data.  Use the attachment fields boost to increase the relevance of these values. |


## Tools
|                           |                                                                                      |
|---------------------------|--------------------------------------------------------------------------------------|
| Get Index Count           | Displays the number of entries in the index, for use when troubleshooting or verifying that your server is functioning correctly. |
| Get Index Settings        | Displays Elastic Search configuration information. |
| Clear Index               | Use the `Clear Index` function to delete your index.  It will be automatically re-created the next time that the index feeder task runs. |

> Nucleus has a built-in Scheduled Task which collects data from all installed search meta-data providers, and submits that data to all installed search index managers.  You 
must create and enable the scheduled task in the `Settings/Scheduler` control panel as it is not enabled by default.

![Elastic Search Scheduled Task](elasticsearch-task.png)
