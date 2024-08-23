using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
public class ManageController : Controller
{
  private IExtensionManager ExtensionManager { get; }
  private Context Context { get; }

  private IEnumerable<System.Type> ControllerTypes { get; }

  public ManageController(Context context, IExtensionManager extensionManager)
  {
    this.Context = context;
    this.ExtensionManager = extensionManager;
    this.ControllerTypes = Nucleus.Core.Plugins.AssemblyLoader.GetTypes<Controller>();
  }

  /// <summary>
  /// Display the "manage" admin page
  /// </summary>
  [HttpGet]
  public async Task<ActionResult> Index()
  {
    ViewModels.Admin.Manage viewModel = new()
    {
      Extensions = (await this.ExtensionManager.ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes.Site))
        .Where(extension => User.HasEditPermission(this.Context.Site, extension, GetType(extension)))
        .OrderBy(extension => extension.FriendlyName)
        .ToList()
    };

    return View("Index", viewModel);
  }

  /// <summary>
  /// Display a control panel extension
  /// </summary>
  [HttpGet]
  public async Task<ActionResult> View(Guid id)
  {
    ViewModels.Admin.Manage viewModel = new()
    {      
      Extensions = (await this.ExtensionManager.ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes.Site))
        .Where(extension => User.HasEditPermission(this.Context.Site, extension, GetType(extension)))
        .OrderBy(extension => extension.FriendlyName)
        .ToList()
    };

    viewModel.ControlPanelExtension = viewModel.Extensions.Where(ext => ext.Id == id).FirstOrDefault();

    if (viewModel.ControlPanelExtension == null)
    {
      return BadRequest();
    }

    return View("_ControlPanelExtension", viewModel);
  }

  private Type GetType(ControlPanelExtensionDefinition controlPanelExtension)
  {
    // Look for controller types with the same name and extension name as the control panel extension.  We have to do it this way
    // because controlPanelExtension.ControllerName is actually a route value, not a fully-qualified type name.
    return this.ControllerTypes
      .Where(type => ControllerNameIs(type, controlPanelExtension.ControllerName) && ExtensionNameIs(type, controlPanelExtension.ExtensionName))
      .FirstOrDefault();
  }

  /// <summary>
  /// Return whether the specified type's name matches the specified controller name.
  /// </summary>
  /// <param name="type"></param>
  /// <param name="controllerName"></param>
  /// <returns></returns>
  /// <remarks>
  /// By convention, controller class names end with "Controller" but are generally specified without the "Controller" suffix,
  /// so we check for either the controller name as specified, or with a "Controller" suffix.
  /// </remarks>
  private static Boolean ControllerNameIs(Type type, string controllerName)
  {
    return type.Name == controllerName || type.Name == controllerName + "Controller";
  }

  /// <summary>
  /// Return whether the specified type has an Extension attribute with a matching extension name.
  /// </summary>
  /// <param name="type"></param>
  /// <param name="extensionName"></param>
  /// <returns></returns>
  private static Boolean ExtensionNameIs(Type type, string extensionName)
  {
    ExtensionAttribute extensionAttribute = type.GetCustomAttributes(typeof(ExtensionAttribute), false).FirstOrDefault() as ExtensionAttribute;
    if (extensionAttribute != null)
    {
      return extensionAttribute.RouteValue == extensionName;
    }
    return false;
  }

}
