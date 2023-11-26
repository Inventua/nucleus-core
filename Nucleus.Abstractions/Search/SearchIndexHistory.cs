using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Represents the indexing history of a search content item.
/// </summary>
public class SearchIndexHistory
{
  /// <summary>
  /// Site id.
  /// </summary>
  public Guid SiteId { get; set; }

  /// <summary>
  /// The Url used to access the item.
  /// </summary>
  public string Url { get; set; }

  /// <summary>
  /// URN of the entity which was used to create the search entry.
  /// </summary>
  public string Scope { get; set; }

  /// <summary>
  /// Id for the search entry source.  Unique when combined with <see cref="Scope"/>.
  /// </summary>
  public Guid SourceId { get; set; }

  /// <summary>
  /// The date and time that the search content item was indexed.
  /// </summary>
  public DateTime LastIndexedDate { get; set; } = DateTime.UtcNow;

}
