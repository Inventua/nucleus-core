using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.Abstractions.Layout
{
	/// <summary>
	/// Defines the interface for the container controller.
	/// </summary>	
	/// <remarks>
	/// The container controller is used to render a container.  
	/// </remarks>
	/// <internal/>
	/// <hidden/>
	public interface IContainerController 
	{
		/// <summary>
		/// Render the container.
		/// </summary>
		/// <returns></returns>
		public abstract ActionResult RenderContainer();
	}
}
