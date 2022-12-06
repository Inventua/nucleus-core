using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Layout;

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

			if (this.ContainerContext.Module.ContainerDefinition == null)
			{
				if (this.ContainerContext.Page.DefaultContainerDefinition == null)
				{
					if (this.ContainerContext.Site.DefaultContainerDefinition == null)
					{
						containerPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.CONTAINERS_FOLDER}/{DEFAULT_CONTAINER}";
					}
					else
					{
						containerPath = this.ContainerContext.Site.DefaultContainerDefinition.RelativePath;
					}
				}
				else
				{
					containerPath = this.ContainerContext.Page.DefaultContainerDefinition.RelativePath;
				}
			}
			else
			{
				containerPath = this.ContainerContext.Module.ContainerDefinition.RelativePath;
			}

			return View(containerPath, this.ContainerContext);
		}
	}
}
