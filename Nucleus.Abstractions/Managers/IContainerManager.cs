using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers;

/// <summary>
/// Defines the interface for the Container manager.
/// </summary>
/// <remarks>
/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
/// </remarks>
public interface IContainerManager
{
  /// <summary>
  /// List all available containers, sorted by <see cref="Models.ContainerDefinition.FriendlyName"/>.
  /// </summary>
  /// <returns></returns>
  public Task<List<Nucleus.Abstractions.Models.ContainerDefinition>> List();

  /// <summary>
  /// Return a list of available styles for the specified container.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="containerDefinition"></param>
  /// <returns></returns>
  public Task<List<ContainerStyle>> ListContainerStyles(Site site, Page page, ContainerDefinition containerDefinition);
}
