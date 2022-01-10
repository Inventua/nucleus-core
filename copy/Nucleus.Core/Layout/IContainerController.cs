using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Defines the interface for the container controller.
	/// </summary>
	/// <remarks>
	/// This interface exists primarily in order to avoid a circular reference between Nucleus.Core and Nucleus.Web.  The 
	/// <see cref="ModuleContentRenderer"/> gets the Nucleus.Web.Controllers.ContainerController from DI, and uses this interface
	/// to ensure that it has a <see cref="RenderContainer"/> method.
	/// </remarks>
	public interface IContainerController 
	{
		public abstract ActionResult RenderContainer();
	}
}
