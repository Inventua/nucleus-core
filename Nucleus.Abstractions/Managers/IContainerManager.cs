using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Managers
{
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
	}
}
