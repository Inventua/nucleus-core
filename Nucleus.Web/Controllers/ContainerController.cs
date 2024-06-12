using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Layout;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Web.Controllers;

public class ContainerController : Controller, IContainerController
{
  private ContainerContext ContainerContext { get; }
  private IWebHostEnvironment WebHostEnvironment { get; }
  private ApplicationPartManager ApplicationPartManager { get; }
  private IContainerManager ContainerManager { get; }
  private ILogger<ContainerController> Logger { get; }

  public ContainerController(IWebHostEnvironment webHostEnvironment, ApplicationPartManager applicationPartManager, ContainerContext containerContext, IContainerManager containerManager, ILogger<ContainerController> logger)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.ApplicationPartManager = applicationPartManager;

    this.ContainerContext = containerContext;
    this.ContainerManager = containerManager;
    this.Logger = logger;
  }

  public ActionResult RenderContainer()
  {
    // this.HttpContext is not available from within the constructor because ControllerFactoryProvider creates the controller and then sets all of the
    // ControllerBase properties.  We use HttpContext.Request.Path when logging a warning, so we need to determine and validate the request path here,
    // rather than in the constructor.
    string containerPath = this.ContainerManager.GetEffectiveContainerPath(this.ContainerContext.Site, this.ContainerContext.Page, this.ContainerContext.Module.ContainerDefinition);
    
    IEnumerable<CompiledRazorAssemblyPart> parts = this.ApplicationPartManager.ApplicationParts
      .Where(part => part is CompiledRazorAssemblyPart)
      .Select(part => part as CompiledRazorAssemblyPart);

    if
    (
      !this.WebHostEnvironment.ContentRootFileProvider.GetFileInfo(containerPath).Exists && 
      !parts.Any(part => (part as IRazorCompiledItemProvider).CompiledItems.Any(item => item.Identifier == '/' + containerPath))
    )
    {
      this.Logger.LogWarning("A '{moduleType}' module with title '{title}' on page '{page}' ({path}) is configured to use a missing container '{container}'.  The default container was used instead.", this.ContainerContext.Module.ModuleDefinition.FriendlyName, this.ContainerContext.Module.Title, this.ContainerContext.Page.Name, this.HttpContext.Request.Path, containerPath);
      containerPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.CONTAINERS_FOLDER}/{Nucleus.Abstractions.Managers.IContainerManager.DEFAULT_CONTAINER}";
    }

    return View(containerPath, this.ContainerContext);
  }
}
