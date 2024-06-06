using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions.Models.Cache;

/// <summary>
/// Represents a read-only report of the current state of a cache.
/// </summary>
public class CacheReport
{
  /// <summary>
  /// Cache name.
  /// </summary>
  public string Name { get; init; }  
  
  /// <summary>
  /// The number of items in the cache.
  /// </summary>
  public int Count { get; init; }  

  /// <summary>
  /// The cache settings.
  /// </summary>
  public CacheOption Options { get; init; }

}
