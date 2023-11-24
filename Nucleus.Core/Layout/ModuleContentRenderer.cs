using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Reflection;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Layout;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;
using Nucleus.Abstractions.Managers;
using Microsoft.IO;

namespace Nucleus.Core.Layout
{
  /// <summary>
  /// Class which renders module content.
  /// </summary>
  public class ModuleContentRenderer : IModuleContentRenderer
  {
    private IActionInvokerFactory ActionInvokerFactory { get; }
    private IHttpContextFactory HttpContextFactory { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IPageManager PageManager { get; }

    private ILogger<ModuleContentRenderer> Logger { get; }

    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new Microsoft.IO.RecyclableMemoryStreamManager();

    public ModuleContentRenderer(IHttpContextFactory httpContextFactory, IActionInvokerFactory actionInvokerFactory, IPageManager pageManager, IPageModuleManager pageModuleManager, ILogger<ModuleContentRenderer> logger)
    {
      this.ActionInvokerFactory = actionInvokerFactory;
      this.HttpContextFactory = httpContextFactory;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.Logger = logger;
    }

    /// <summary>
    /// Render the module instances within the specified pane.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="paneName"></param>
    /// <returns></returns>
    public async Task<IHtmlContent> RenderPaneAsync(ViewContext viewContext, Context context, string paneName)
    {
      HtmlContentBuilder output = new();

      if (context == null)
      {
        throw new ArgumentNullException(nameof(Context));
      }

      if (context.Page == null)
      {
        throw new ArgumentNullException(nameof(Context.Page));
      }

      if (context.Page.Modules == null)
      {
        throw new ArgumentNullException(nameof(Context.Page.Modules));
      }

      Boolean isEditing = viewContext.HttpContext.User.IsEditing(viewContext?.HttpContext, context.Site, context.Page);

      IEnumerable<PageModule> modules = 
        context.Page.Modules
          .Where(module => paneName == "*" || module.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase));

      // if the pane is empty and the user is editing, add a "move module" drag target to the start of the pane
      if (!modules.Any() && isEditing)
      {
        output.AppendHtml(Nucleus.Extensions.PageModuleExtensions.BuildMoveDropTarget(null, paneName, $"Move to {paneName}"));       
      }

      if (modules.Any())
      {
        // Render the module output if the module pane is the specified pane, and the user has permission to view it
        foreach (PageModule moduleInfo in modules)
        {
          // Check the current user's view permission for the module.  "Permission denied" for viewing a module is not a failure, it just means we don't render the module.
          if (viewContext.HttpContext.User.HasViewPermission(context.Site, context.Page, moduleInfo))
          {
            try
            {
              // pane name '*' is a special value, used by search feed generators to render content for all panes in a plain format without containers
              IHtmlContent moduleBody = await RenderModuleView(viewContext, context.Site, context.Page, moduleInfo, context.LocalPath, paneName != "*");
              output.AppendHtml(moduleBody);
            }
            catch (System.InvalidOperationException e)
            {
              // This handler is to deal with cases where the module information is invalid (not general errors thrown from a module).
              // If the user is an admin, display the error message in place of the module content.  If the user is not an admin, log the exception
              // and suppress output of the module.							
              this.Logger?.LogError(e, "Error rendering {pane}.", moduleInfo.Pane);
              if (viewContext.HttpContext.User.IsSiteAdmin(context.Site))
              {
                output.AppendHtml(e.Message);
              }
            }
          }
        }

        // if the pane is not empty, add a "move module" drag target to the end of the pane
        if (isEditing)
        {
          output.AppendHtml(Nucleus.Extensions.PageModuleExtensions.BuildMoveDropTarget(null, paneName, $"Move to end of {paneName}"));
        }
      }

      return output;
    }


    /// <summary>
    /// Render a module's "View" action.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="moduleInfo">Specifies the module being rendered.</param>
    /// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
    /// <returns></returns>
    public async Task<IHtmlContent> RenderModuleView(ViewContext viewContext, Site site, Page page, PageModule moduleInfo, LocalPath localPath, Boolean renderContainer)
    {
      HtmlContentBuilder output = new();
      System.Security.Claims.ClaimsPrincipal user = viewContext.HttpContext.User;

      if (user.HasViewPermission(site, page, moduleInfo))
      {
        HttpResponse moduleOutput = await BuildModuleOutput(this.HttpContextFactory, this.ActionInvokerFactory, viewContext, site, page, moduleInfo, localPath, moduleInfo.ModuleDefinition.ViewController, moduleInfo.ModuleDefinition.ViewAction, renderContainer);

        if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.PermanentRedirect)
        {
          // the shadow module uses this response to direct Nucleus to render another module            
          if (moduleOutput.Headers.ContainsKey("X-NucleusAction") && moduleOutput.Headers.Location.First()?.StartsWith("/mid=") == true)
          {
            string location = moduleOutput.Headers.Location.First();

            // reset the response status code and remove the headers 
            viewContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            viewContext.HttpContext.Response.Headers.Remove("Location");
            viewContext.HttpContext.Response.Headers.Remove("X-NucleusAction");

            if (Guid.TryParse(location.Split('=')[1], out Guid moduleId))
            {
              PageModule redirectModuleInfo = await this.PageModuleManager.Get(moduleId);
              Page redirectPage = await this.PageManager.Get(redirectModuleInfo.PageId);
              if (user.HasViewPermission(site, redirectPage, redirectModuleInfo))
              {
                moduleOutput = await BuildModuleOutput(this.HttpContextFactory, this.ActionInvokerFactory, viewContext, site, redirectPage, redirectModuleInfo, localPath, redirectModuleInfo.ModuleDefinition.ViewController, redirectModuleInfo.ModuleDefinition.ViewAction, renderContainer);
              }
              else
              {
                // user does not have view permission for the module that would be rendered, so render nothing
                return output;
              }
            }
            else
            {
              this.Logger?.LogTrace("A PermanentRedirect response with a X-NucleusAction header could not be handled because the location header value '{location}' did not contain valid module information.", moduleOutput.Headers.Location);
              if (!user.IsEditing(viewContext.HttpContext, site, page))
              {
                // if the user is not editing, return empty content.  Otherwise continue, so that we render a module with no content (but with edit controls and a container)
                return output;
              }
            }
          }
        }

        if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.Forbidden)
        {
          // modules can return a status of Forbidden to indicate that the user doesn't have access to a resource that
          // the module uses.  This should not be treated as a failure, it just means we don't render the module view.
          return output;
        }

        Boolean isEditing = user.IsEditing(viewContext.HttpContext, site, page, moduleInfo);

        if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.NoContent && !isEditing)
        {
          // modules can return NoContent to indicate that they have nothing to display, and should not be rendered (including that 
          // their container is not rendered).  We check to see if the user is editing the page & ignore NoContent so that editors
          // can always edit settings.        
          moduleOutput.StatusCode = (int)System.Net.HttpStatusCode.OK;
          return output;
        }

        // create a wrapping div to contain CSS classes defined for the module, and to contain the editing controls (if rendered) and the
        // module output.  We always render a wrapping div even if no module "styles" are defined and/or the user is not editing, because we want the 
        // output DOM to be consistent, so that CSS for the layout/container/module can always target the same DOM structure.
        TagBuilder moduleView = new("div");

        if (HasAdminPermissionOnly(moduleInfo))
        {
          if (isEditing && user.IsSiteAdmin(site))
          {
            moduleView.AddCssClass("nucleus-adminviewonly");
          }
          else
          {
            // suppress display of modules with no permissions when an admin is not in editing mode
            moduleOutput.StatusCode = (int)System.Net.HttpStatusCode.OK;
            return output;
          }
        }

        if (!String.IsNullOrEmpty(moduleInfo.Style))
        {
          moduleView.AddCssClass(moduleInfo.Style);
        }

        if (isEditing)
        {
          moduleView.AddCssClass("nucleus-module-editing");

          moduleView.InnerHtml.AppendHtml(moduleInfo.BuildMoveDropTarget(moduleInfo.Pane, "Move here"));
          moduleView.InnerHtml.AppendHtml(BuildModuleEditControls(viewContext, moduleInfo, user.HasEditPermission(site, page)));
        }

        moduleView.InnerHtml.AppendHtml(ToHtmlContent(moduleOutput));
        output.AppendHtml(moduleView);
      }

      return output;
    }

    /// <summary>
    /// Add inline editing controls and menus for the specified <paramref name="moduleInfo"/> to the specified 
    /// <paramref name="editorBuilder"/>.
    /// </summary>
    /// <param name="viewContext"></param>
    /// <param name="editorBuilder"></param>
    /// <param name="moduleInfo"></param>
    /// <param name="user"></param>
    /// <remarks>
    /// This function does not perform permissions checks.  The caller is responsible for determining whether the 
    /// current user has edit permissions and is currently in edit mode.
    /// </remarks>
		private static TagBuilder BuildModuleEditControls(ViewContext viewContext, PageModule moduleInfo, Boolean hasPageEditPermission)
    {
      IUrlHelper urlHelper = viewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(viewContext);

      // Render edit controls
      TagBuilder formBuilder = new("form");
      formBuilder.Attributes.Add("class", "nucleus-inline-edit-controls");

      // #refresh is a dummy value - we want nucleus-shared.js#_postPartialContent to process the clicks for the inline
      // editing functions, so we need a non-blank data-target attribute so that the click event is bound to _postPartialContent.  
      // The value that we are using - "#refresh" - doesn't have any special meaning or code to process it in nucleus-shared.js.
      formBuilder.Attributes.Add("data-target", "#refresh");

      // add the "edit module settings" and "common settings" buttons
      if (!String.IsNullOrEmpty(moduleInfo.ModuleDefinition.EditAction))
      {
        formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildEditButton("&#xe3c9;", "Edit Content/Settings", urlHelper.Content("~/Admin/Pages/EditModule"), null));
      }

      formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildEditButton("&#xe8b8;", "Layout and Permissions Settings", urlHelper.Content("~/Admin/Pages/EditModuleCommonSettings"), null));

      // add the "move" drag source button
      formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildMoveButton("&#xe89f;", "Drag and drop to move this module", null));

      // add the "delete module" button if the user has page-edit permissions
      if (hasPageEditPermission)
      {
        formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildDeleteButton("&#xe14c;", "Delete Module", urlHelper.Content("~/Admin/Pages/DeletePageModuleInline"), null));
      }

      return formBuilder;
    }

    /// <summary>
    /// Render a module's "Edit" action.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="moduleInfo">Specifies the module being rendered.</param>
    /// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
    /// <returns></returns>
    public async Task<IHtmlContent> RenderModuleEditor(ViewContext viewContext, Site site, Page page, PageModule moduleInfo, LocalPath localPath, Boolean renderContainer)
    {
      if (moduleInfo.ModuleDefinition.EditAction != null && viewContext.HttpContext.User.HasEditPermission(site, page, moduleInfo))
      {
        HttpResponse moduleOutput = await BuildModuleOutput(this.HttpContextFactory, this.ActionInvokerFactory, viewContext, site, page, moduleInfo, localPath, String.IsNullOrEmpty(moduleInfo.ModuleDefinition.SettingsController) ? moduleInfo.ModuleDefinition.ViewController : moduleInfo.ModuleDefinition.SettingsController, moduleInfo.ModuleDefinition.EditAction, renderContainer);

        return ToHtmlContent(moduleOutput);
      }
      else
      {
        return HtmlString.Empty;
      }
    }

    /// <summary>
    /// Render the specified action for a module.
    /// </summary>
    /// <param name="viewContext"></param>
    /// <param name="moduleinfo">Specifies the module being rendered.</param>
    /// <param name="action">Specifies the name of the action being rendered.</param>
    /// <param name="RenderContainer">Specifies whether to wrap the module output in a container.</param>
    /// <returns></returns>
    /// <remarks>
    /// Permissions checks should be done by the caller.
    /// </remarks>
    private static async Task<HttpResponse> BuildModuleOutput(IHttpContextFactory httpContextFactory, IActionInvokerFactory actionInvokerFactory, ViewContext viewContext, Site site, Page page, PageModule moduleinfo, LocalPath localPath, string controller, string action, Boolean RenderContainer)
    {
      Context scopedContext;
      IServiceProvider originalServiceProvider;

      HttpContext newHttpContext = httpContextFactory.Create(viewContext?.HttpContext.Features);

      await using (AsyncServiceScope moduleScope = newHttpContext.RequestServices.CreateAsyncScope())
      {
        // We must store and restore the original newHttpContext.RequestServices, so that the main HttpContext.RequestServices 
        // object doesn't get disposed in between calls (when there are multiple modules on a page).
        originalServiceProvider = newHttpContext.RequestServices;

        try
        {
          // Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
          // executed, it gets DI objects from the module scope
          newHttpContext.RequestServices = moduleScope.ServiceProvider;

          IActionDescriptorCollectionProvider actionDescriptorProvider = moduleScope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
          ControllerActionDescriptor actionDescriptor = BuildActionDescriptor(actionDescriptorProvider, controller, action, moduleinfo);

          // Report common error (when the module definition is invalid)
          if (actionDescriptor == null || actionDescriptor.MethodInfo == null)
          {
            throw new InvalidOperationException($"Module Definition is invalid: Action '{action}' does not exist in extension '{moduleinfo.ModuleDefinition.Extension}', controller '{controller}'.");
          }

          scopedContext = (Context)moduleScope.ServiceProvider.GetService(typeof(Context));

          // set context.Module to the module being processed
          scopedContext.Module = moduleinfo;
          scopedContext.Page = page;
          scopedContext.Site = site;
          scopedContext.LocalPath = localPath;

          await BuildContent(newHttpContext, actionInvokerFactory, actionDescriptor);

          // Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
          newHttpContext.RequestServices = originalServiceProvider;

          if (RenderContainer)
          {
            return await BuildContainerOutput(actionInvokerFactory, viewContext, site, page, moduleinfo, newHttpContext);
          }
          else
          {
            return newHttpContext.Response;
          }
        }
        catch (Exception)
        {
          // Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
          newHttpContext.RequestServices = originalServiceProvider;
          throw;
        }
      }
    }


    /// <summary>
    /// Render a container and return the rendered result.
    /// </summary>
    /// <param name="viewContext"></param>
    /// <param name="moduleinfo">Specifies the module being rendered.</param>
    /// <param name="content">Rendered module output.</param>
    /// <returns></returns>
    /// <remarks>
    /// The rendered output of a container includes the module output.
    /// </remarks>
    private static async Task<HttpResponse> BuildContainerOutput(IActionInvokerFactory actionInvokerFactory, ViewContext viewContext, Site site, Page page, PageModule moduleinfo, HttpContext httpContext)
    {
      ContainerContext scopedContainerContext;
      IServiceProvider originalServiceProvider;

      // https://github.com/aspnet/Mvc/issues/6900

      await using (AsyncServiceScope moduleScope = httpContext.RequestServices.CreateAsyncScope())
      {
        // We must store and restore the original newHttpContext.RequestServices, so that the main HttpContext.RequestServices 
        // object doesn't get disposed in between calls (when there are multiple modules on a page).
        originalServiceProvider = httpContext.RequestServices;

        try
        {
          // Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
          // executed, it gets DI objects from the module scope
          httpContext.RequestServices = moduleScope.ServiceProvider;

          scopedContainerContext = (ContainerContext)moduleScope.ServiceProvider.GetService<ContainerContext>();
          scopedContainerContext.Site = site;
          scopedContainerContext.Page = page;
          scopedContainerContext.Module = moduleinfo;

          TagBuilder section = new("section");
          section.InnerHtml.AppendHtml(ToHtmlContent(httpContext.Response));
          scopedContainerContext.Content = section;

          Type containerType = typeof(IContainerController);

          IActionDescriptorCollectionProvider actionDescriptorProvider = moduleScope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
          ControllerActionDescriptor actionDescriptor = actionDescriptorProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(descriptor => descriptor.ControllerTypeInfo.IsAssignableTo(containerType))
            .Where(descriptor => descriptor.ActionName.Equals(nameof(IContainerController.RenderContainer)))
            .FirstOrDefault();

          await BuildContent(httpContext, actionInvokerFactory, actionDescriptor);

          // Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
          httpContext.RequestServices = originalServiceProvider;
        }
        catch (Exception)
        {
          // Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
          httpContext.RequestServices = originalServiceProvider;
          throw;
        }

        return httpContext.Response;
      }
    }

    /// <summary>
    /// Invoke the action specified by <paramref name="actionDescriptor"/>.  The response is written to httpContext.Response.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="actionInvokerFactory"></param>
    /// <param name="actionDescriptor"></param>
    /// <returns></returns>
    private static async Task BuildContent(HttpContext httpContext, IActionInvokerFactory actionInvokerFactory, ControllerActionDescriptor actionDescriptor)
    {
      using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(actionDescriptor.ControllerTypeInfo.AssemblyQualifiedName))
      {
        // Populate the actionDescriptor.Parameters list with action (method) parameters so that they are bound.
        // actionDescriptor.Parameters must be a new List, or actionDescriptor.Parameters.Add() will fail with a 
        // 'Collection was of a fixed size' exception.
        actionDescriptor.Parameters = actionDescriptor.MethodInfo.GetParameters().Select(param => new ParameterDescriptor () { Name = param.Name, ParameterType = param.ParameterType }).ToList();

        //actionDescriptor.Parameters = new List<ParameterDescriptor>();
        //foreach (ParameterInfo param in actionDescriptor.MethodInfo.GetParameters())
        //{
        //  actionDescriptor.Parameters.Add(new()
        //  {
        //    Name = param.Name,
        //    ParameterType = param.ParameterType
        //  });
        //}

        // We must create a new routeData object (don't use htmlHelper.ViewContext.RouteData), because we must provide the controller, area and
        // action names for the module, rather than the route values for the original http request.
        Microsoft.AspNetCore.Routing.RouteData routeData = new();
        foreach (KeyValuePair<string, string> routeValue in actionDescriptor.RouteValues)
        {
          routeData.Values[routeValue.Key] = routeValue.Value;
        }

        ActionContext actionContext = new(httpContext, routeData, actionDescriptor);
        ControllerContext controllerContext = new(actionContext);

        // we catch the module's rendered output in a memory stream so that we can add it to the page output
        httpContext.Response.Body = RecyclableMemoryStreamManager.GetStream();

        // Create the controller and run the controller action
        await actionInvokerFactory.CreateInvoker(controllerContext).InvokeAsync();
      }
    }

    /// <summary>
		/// Returns a true/false value indicating whether the module permissions are set to allow only administrators to view the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <returns></returns>
		private static Boolean HasAdminPermissionOnly(PageModule moduleInfo)
    {
      return !(moduleInfo.InheritPagePermissions || moduleInfo.Permissions.Any());
    }

    /// <summary>
    /// Build an action descriptor for the specified <paramref name="controllerName"/> and <paramref name="action"/>.
    /// </summary>
    /// <param name="actionDescriptorProvider"></param>
    /// <param name="controllerName"></param>
    /// <param name="action"></param>
    /// <param name="moduleinfo"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static ControllerActionDescriptor BuildActionDescriptor(IActionDescriptorCollectionProvider actionDescriptorProvider, string controllerName, string action, PageModule moduleinfo)
    {
      ControllerActionDescriptor actionDescriptor = null;

      if (String.IsNullOrEmpty(controllerName) || String.IsNullOrEmpty(moduleinfo.ModuleDefinition.Extension))
      {
        // backward compatibility
        Type moduleControllerType = Type.GetType(moduleinfo.ModuleDefinition.ClassTypeName);

        if (moduleControllerType != null)
        {
          TypeInfo moduleControllerTypeInfo = moduleControllerType.GetTypeInfo();

          moduleinfo.ModuleDefinition.Extension = Nucleus.Core.Plugins.AssemblyLoader.GetExtensionFolderName(moduleControllerTypeInfo.Assembly.Location).Replace(" ", "");
          controllerName = moduleControllerType.Name;
          if (controllerName.EndsWith("Controller"))
          {
            // remove "Controller" from the end of the controller name
            controllerName = controllerName[..^"Controller".Length];
          }
        }
      }

      if (!String.IsNullOrEmpty(controllerName))
      {
        actionDescriptor = actionDescriptorProvider.ActionDescriptors.Items
          .OfType<ControllerActionDescriptor>()
          .Where(descriptor => IsMatch(descriptor, moduleinfo.ModuleDefinition.Extension, controllerName, action))
          .FirstOrDefault();
      }

      if (actionDescriptor == null)
      {
        throw new InvalidOperationException($"Unable to load an action descriptor for the module '{moduleinfo.ModuleDefinition.FriendlyName}' [{controllerName}.{action}].  Check your package.xml and controller class.  The most common cause of this error is that your package.xml has the wrong controller or action name specified, or your controller class doesn't have a Nucleus 'Extension' attribute.");
      }

      actionDescriptor.RouteValues["extension"] = moduleinfo.ModuleDefinition.Extension;

      return actionDescriptor;
    }

    /// <summary>
    /// Returns whether the specified <paramref name="actionDescriptor"/> matches the specified <paramref name="controller"/>, <paramref name="action"/> and <paramref name="extension"/>.
    /// </summary>
    /// <param name="actionDescriptor"></param>
    /// <param name="action"></param>
    /// <param name="controller"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    private static Boolean IsMatch(ControllerActionDescriptor actionDescriptor, string extension, string controller, string action)
    {
      return
        (new string[] { actionDescriptor.ControllerName, actionDescriptor.ControllerName + "Controller" }).Contains(controller) &&
        actionDescriptor.ActionName.Equals(action) &&
        actionDescriptor.RouteValues.ContainsKey("extension") && actionDescriptor.RouteValues["extension"]?.Equals(extension) == true;
    }

    /// <summary>
    /// Create a new IHtmlContent containing the body of the specified response.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <remarks>
    /// </remarks>
    private static IHtmlContent ToHtmlContent(HttpResponse response)
    {
      response.Body.Position = 0;
      using (var reader = new StreamReader(response.Body))
      {
        return new HtmlString(reader.ReadToEnd());
      }
    }
  }
}