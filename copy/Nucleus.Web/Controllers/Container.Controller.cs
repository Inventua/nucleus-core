using Microsoft.AspNetCore.Mvc;
using Nucleus.Core.Layout;
using Nucleus.Abstractions;

namespace Nucleus.Web.Controllers
{
	public class ContainerController : Controller, IContainerController
	{
		public const string DEFAULT_CONTAINER = "default.cshtml";

		private ContainerContext ContainerContext { get; }

		public ContainerController(ContainerContext containerContext)
		{
			this.ContainerContext = containerContext;
		}

		public ActionResult RenderContainer()
		{
			string containerPath;

			if (this.ContainerContext.Module.Container == null)
			{
				containerPath = $"{Folders.CONTAINERS_FOLDER}\\{DEFAULT_CONTAINER}";
			}
			else
			{
				containerPath = this.ContainerContext.Module.Container.RelativePath;
			}

			return View(containerPath, this.ContainerContext);
		}
	}
}
