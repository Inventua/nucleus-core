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
	public interface IContainerManager
	{
		/// <summary>
		/// List all available containers.
		/// </summary>
		/// <returns></returns>
		public Task<List<Nucleus.Abstractions.Models.ContainerDefinition>> List();
	}
}
