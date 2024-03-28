using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Layout;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Web.Controllers
{
	public class ContainerController : Controller, IContainerController
	{
    private string ContainerPath { get; }

		private ContainerContext ContainerContext { get; }
		
		public ContainerController(ContainerContext containerContext, IContainerManager containerManager)
		{
			this.ContainerContext = containerContext;
      this.ContainerPath = containerManager.GetEffectiveContainerPath(containerContext.Site, containerContext.Page, containerContext.Module.ContainerDefinition);
		}

		public ActionResult RenderContainer()
		{
			return View(this.ContainerPath, this.ContainerContext);
		}
	}
}
