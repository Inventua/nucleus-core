using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Portable;

/// <summary>
/// Module import/export interface.
/// </summary>
public interface IPortable
{
  /// <summary>
  /// The Id of the module, from the package file.
  /// </summary>
  /// <remarks>
  /// This value is used to identify which module the interface works with and by extension, the format of the
  /// object(s) passed to the Import and returned by the Export function.
  /// </remarks>
  public Guid ModuleDefinitionId { get; }

  /// <summary>
  /// Module friendly name.  This can be used for troubleshooting, logging and related purposes.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Import the specified items for the specified module.
  /// </summary>
  /// <param name="module"></param>
  /// <param name="content"></param>
  /// <returns></returns>
  public Task Import(Nucleus.Abstractions.Models.PageModule module, PortableContent content);

  /// <summary>
  /// Export items which belong to the specified module.
  /// </summary>
  /// <param name="module"></param>
  /// <returns></returns>
  public Task<PortableContent> Export(Nucleus.Abstractions.Models.PageModule module);


}