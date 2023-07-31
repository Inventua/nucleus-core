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
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Core.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Layout;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Layout
{
  /// <summary>
  /// Renders the modules that make up a "pane" in a page.
  /// </summary>
  public class ModuleContentRenderer : IModuleContentRenderer
  {
    //private Context Context { get; }
    private IActionInvokerFactory ActionInvokerFactory { get; }
    private IHttpContextFactory HttpContextFactory { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IPageManager PageManager { get; }

    private ILogger<ModuleContentRenderer> Logger { get; }

    public ModuleContentRenderer(IPageManager pageManager, IPageModuleManager pageModuleManager, IActionInvokerFactory actionInvokerFactory, IHttpContextFactory httpContextFactory, ILogger<ModuleContentRenderer> logger)
    {
      //this.Context = context;
      this.ActionInvokerFactory = actionInvokerFactory;
      this.HttpContextFactory = httpContextFactory;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
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
        throw new InvalidOperationException($"{nameof(Context)} is null.");
      }

      if (context.Page == null)
      {
        throw new InvalidOperationException($"{nameof(Context.Page)} is null.");
      }

      if (context.Page.Modules == null)
      {
        throw new InvalidOperationException($"{nameof(Context.Page.Modules)} is null.");
      }

      // if the pane is empty, add a "move module" drag target to the start of the pane
      if (viewContext.HttpContext.User.IsEditing(viewContext?.HttpContext, context.Site, context.Page))
      {
        if (!context.Page.Modules.Where(module => module.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase)).Any())
        {
          output.AppendHtml(Nucleus.Extensions.PageModuleExtensions.BuildMoveDropTarget(null, paneName, $"Move to {paneName}"));
        }
      }

      // Render the module output if the module pane is the specified pane, and the user has permission to view it
      foreach (PageModule moduleInfo in context.Page.Modules)
      {
        if (paneName == "*" || moduleInfo.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase))
        {
          // "permission denied" for viewing a module is not a failure, it just means we don't render the module
          if (HasViewPermission(context.Site, context.Page, moduleInfo, viewContext.HttpContext.User))
          {
            try
            {
              // pane name '*' is a special value, used by search feed generators to render content for all panes in a plain format without containers
              IHtmlContent moduleBody = await RenderModuleView(viewContext, context.Site, context.Page, moduleInfo, context.LocalPath, paneName != "*");
              output.AppendHtml(moduleBody);
            }
            catch (System.InvalidOperationException e)
            {
              // If an error occurred while rendering module, and the user is an admin, display the error message in place of the module
              // content.  If the user is not an admin, suppress output of the module.							
              this.Logger?.LogError(e, "Error rendering {pane}.", moduleInfo.Pane);
              if (viewContext.HttpContext.User.IsSiteAdmin(context.Site))
              {
                output.AppendHtml(e.Message);
              }
            }
          }
        }
      }

      // if the pane is not empty, add a "move module" drag target to the end of the pane
      if (viewContext.HttpContext.User.IsEditing(viewContext?.HttpContext, context.Site, context.Page))
      {
        if (context.Page.Modules.Where(module => module.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase)).Any())
        {
          output.AppendHtml(Nucleus.Extensions.PageModuleExtensions.BuildMoveDropTarget(null, paneName, $"Move to end of {paneName}"));
        }
      }

      return output;
    }

    private IHtmlContent RenderException(HttpContext context, Site site, PageModule moduleInfo, Exception e)
    {
      TagBuilder output = new("div");

      if (context.User.IsSiteAdmin(site))
      {
        // admin user, render error information
        output.InnerHtml.Append($"An error occurred rendering the {moduleInfo.Title} module: {e.Message}");
      }
      else
      {
        // regular user, render generic error
        output.InnerHtml.Append($"An error occurred rendering the {moduleInfo.Title} module.");
      }

      return output;
    }

    /// <summary>
    /// Returns a true/false value indicating whether the user has view rights for the module.
    /// </summary>
    /// <param name="moduleInfo"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    private Boolean HasViewPermission(Site site, Page page, PageModule moduleInfo, System.Security.Claims.ClaimsPrincipal user)
    {
      return user.HasViewPermission(site, page, moduleInfo);
    }

    /// <summary>
		/// Returns a true/false value indicating whether the module permissions are set to allow only administrators to view the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <returns></returns>
		private Boolean HasAdminPermissionOnly(PageModule moduleInfo)
    {
      return !(moduleInfo.InheritPagePermissions || moduleInfo.Permissions.Any());
    }

    /// <summary>
    /// Returns a true/false value indicating whether the user has edit rights for the module.
    /// </summary>
    /// <param name="moduleInfo"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    private Boolean HasEditPermission(Site site, Page page, PageModule moduleInfo, System.Security.Claims.ClaimsPrincipal user)
		{
			return user.HasEditPermission(site, page, moduleInfo);
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

			if (HasViewPermission(site, page, moduleInfo, user))
			{
				HttpResponse moduleOutput = await BuildModuleOutput(viewContext, site, page, moduleInfo, localPath, moduleInfo.ModuleDefinition.ViewController, moduleInfo.ModuleDefinition.ViewAction, renderContainer);

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
                moduleOutput = await BuildModuleOutput(viewContext, site, redirectPage, redirectModuleInfo, localPath, redirectModuleInfo.ModuleDefinition.ViewController, redirectModuleInfo.ModuleDefinition.ViewAction, renderContainer);
              }
              else
              {
                // user does not have view permission to the module that would be rendered, so render nothing
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

				Boolean isEditing = user.IsEditing(viewContext?.HttpContext, site, page, moduleInfo);
        
        if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.NoContent)
				{
					// modules can return NoContent to indicate that they have nothing to display, and should not be rendered (including that 
					// their container is not rendered).  We check to see if the user is editing the page & ignore NoContent so that editors
					// can always edit settings.
					if (!isEditing)
					{
						moduleOutput.StatusCode = (int)System.Net.HttpStatusCode.OK;
						return output;
					}
				}
				
				// create a wrapping div to contain CSS classes defined for the module, and to contain the editing controls (if rendered) and the
				// module output.  We always render a wrapping div even if no module "styles" are defined/user is not editing, because we want the 
				// output DOM to be consistent, so that CSS for the layout/container/module can always target the same DOM structure.
				TagBuilder moduleView = new("div");

				if (!String.IsNullOrEmpty(moduleInfo.Style))
				{
					moduleView.AddCssClass(moduleInfo.Style);
				}

        if (isEditing)
				{
          moduleView.AddCssClass("nucleus-module-editing");

          if (user.IsSiteAdmin(site) && HasAdminPermissionOnly(moduleInfo))
          {
            moduleView.AddCssClass("nucleus-adminviewonly");
          }

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
		private TagBuilder BuildModuleEditControls(ViewContext viewContext, PageModule moduleInfo, Boolean hasPageEditPermission)
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
			if (moduleInfo.ModuleDefinition.EditAction != null && HasEditPermission(site, page, moduleInfo, viewContext.HttpContext.User))
			{
				HttpResponse moduleOutput = await BuildModuleOutput(viewContext, site, page, moduleInfo, localPath, String.IsNullOrEmpty(moduleInfo.ModuleDefinition.SettingsController) ? moduleInfo.ModuleDefinition.ViewController : moduleInfo.ModuleDefinition.SettingsController, moduleInfo.ModuleDefinition.EditAction, renderContainer);

				return ToHtmlContent(moduleOutput);
			}
			else
			{
				return new HtmlContentBuilder();
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
		private async Task<HttpResponse> BuildModuleOutput(ViewContext viewContext, Site site, Page page, PageModule moduleinfo, LocalPath localPath, string controller, string action, Boolean RenderContainer)
		{
			Context scopedContext;
			IServiceProvider originalServiceProvider;
			RouteData routeData = new();

			HttpContext newHttpContext = this.HttpContextFactory.Create(viewContext?.HttpContext.Features);

			using (IServiceScope moduleScope = newHttpContext.RequestServices.CreateScope())
			{
				scopedContext = (Context)moduleScope.ServiceProvider.GetService(typeof(Context));

				// set context.Module to the module being processed
				scopedContext.Module = moduleinfo;
				scopedContext.Page = page;
				scopedContext.Site = site;
				scopedContext.LocalPath = localPath;

				// If we don't store and restore the original newHttpContext.RequestServices, the htmlHelper.ViewContext?.HttpContext.RequestServices 
				// object gets disposed in between calls (when there are multiple modules on a page).
				// ** NET core doesn't provide nested scopes, so when it disposes of ModuleScope it also disposes of moduleScope.ServiceProvider, which
				//    is equal to htmlHelper.ViewContext.HttpContext.RequestServices.
				originalServiceProvider = newHttpContext.RequestServices;

				try
				{
					// Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
					// executed, it gets DI objects from the module scope
					newHttpContext.RequestServices = moduleScope.ServiceProvider;

					IActionDescriptorCollectionProvider actionDescriptorProvider = moduleScope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
					ControllerActionDescriptor actionDescriptor = null;

					if (String.IsNullOrEmpty(controller) || String.IsNullOrEmpty(moduleinfo.ModuleDefinition.Extension))
					{
						// backward compatibility
						TypeInfo moduleControllerTypeInfo = null;
						Type moduleControllerType = Type.GetType(moduleinfo.ModuleDefinition.ClassTypeName);

						if (moduleControllerType != null)
						{
							moduleControllerTypeInfo = moduleControllerType.GetTypeInfo();
						}

						moduleinfo.ModuleDefinition.Extension = Nucleus.Core.Plugins.AssemblyLoader.GetExtensionFolderName(moduleControllerTypeInfo.Assembly.Location).Replace(" ", "");
						controller = moduleControllerType.Name;						
						if (controller.EndsWith("Controller"))
						{
							controller = controller.Substring(0, controller.Length - "Controller".Length);
						}
					}

					if (!String.IsNullOrEmpty(controller))
					{
						actionDescriptor = (ControllerActionDescriptor)actionDescriptorProvider.ActionDescriptors.Items
							.Where(descriptor => descriptor is ControllerActionDescriptor)
							.Where(descriptor => IsMatch((ControllerActionDescriptor)descriptor, action, controller, moduleinfo.ModuleDefinition.Extension))
							.FirstOrDefault();
					}
					
					if (actionDescriptor == null)
					{
						throw new InvalidOperationException($"Unable to load an action descriptor for the module '{moduleinfo.ModuleDefinition.FriendlyName}' [{controller}.{action}].  Check your package.xml and controller class.  The most common cause of this error is that your package.xml has the wrong controller or action name specified, or your controller class doesn't have a Nucleus 'Extension' attribute.");
					}

					using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(actionDescriptor.ControllerTypeInfo.AssemblyQualifiedName))
					{
						// Report common error (Module definition invalid)
						if (actionDescriptor == null || actionDescriptor.MethodInfo == null)
						{
							throw new InvalidOperationException($"Module Definition is invalid: Action '{action}' does not exist in extension '{moduleinfo.ModuleDefinition.Extension}', controller '{controller}'.");
						}

						// Populate the actionDescriptor.Parameters list with action (method) parameters so that they are bound
						// actionDescriptor.Parameters must be set to a new List, or the actionDescriptor.Parameters.Add() will fail with a 
						// 'Collection was of a fixed size.' exception.
						actionDescriptor.Parameters = new List<ParameterDescriptor>();
						foreach (ParameterInfo param in actionDescriptor.MethodInfo.GetParameters())
						{
							ParameterDescriptor paramDesc = new();
							paramDesc.Name = param.Name;
							paramDesc.ParameterType = param.ParameterType;

							actionDescriptor.Parameters.Add(paramDesc);
						}

						// TODO Use routeData from actionDescriptor? ****

						// We must create a NEW routeData object (don't use htmlHelper.ViewContext.RouteData), because we must provide the controller, area and
						// action names for the module, rather than the route values for the original http request.
						foreach (var routeValue in actionDescriptor.RouteValues)
						{
							routeData.Values[routeValue.Key] = routeValue.Value;
						}
						routeData.Values["extension"] = moduleinfo.ModuleDefinition.Extension; //Nucleus.Core.Plugins.AssemblyLoader.GetExtensionFolderName(moduleControllerTypeInfo.Assembly.Location);

						ActionContext actionContext = new(newHttpContext, routeData, actionDescriptor);					
						ControllerContext controllerContext = new(actionContext);

						// Catch the module's rendered output 
						newHttpContext.Response.Body = new MemoryStream();

						// Create the controller and run the controller action
						await this.ActionInvokerFactory.CreateInvoker(controllerContext).InvokeAsync();
          }

          // Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
          newHttpContext.RequestServices = originalServiceProvider;

				}
				catch (Exception)
				{
					// Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
					newHttpContext.RequestServices = originalServiceProvider;

					throw;
				}

        if (RenderContainer)
				{
					return await BuildContainerOutput(viewContext, site, page, moduleinfo, newHttpContext);
				}
				else
				{
					return newHttpContext.Response;
				}
			}
		}

		private Boolean IsMatch(ControllerActionDescriptor actionDescriptor, string action, string controller, string extension)
		{
			return
				(actionDescriptor.ControllerName.Equals(controller) || (actionDescriptor.ControllerName + "Controller").Equals(controller)) &&
				actionDescriptor.ActionName.Equals(action) &&
				actionDescriptor.RouteValues.ContainsKey("extension") &&
				actionDescriptor.RouteValues["extension"] != null &&
				actionDescriptor.RouteValues["extension"].Equals(extension);
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
		private async Task<HttpResponse> BuildContainerOutput(ViewContext viewContext, Site site, Page page, PageModule moduleinfo, HttpContext httpContext)
		{
			ContainerContext scopedContainerContext;
			IServiceProvider originalServiceProvider;
			RouteData routeData = new();

			// https://github.com/aspnet/Mvc/issues/6900

			using (IServiceScope moduleScope = httpContext.RequestServices.CreateScope())
			{
				scopedContainerContext = (ContainerContext)moduleScope.ServiceProvider.GetService<ContainerContext>();
				scopedContainerContext.Site = site;
				scopedContainerContext.Page = page;
				scopedContainerContext.Module = moduleinfo;

        TagBuilder section = new("section");
        section.InnerHtml.AppendHtml(ToHtmlContent(httpContext.Response));
        scopedContainerContext.Content = section;        

				Controller containerController = (Controller)moduleScope.ServiceProvider.GetService<IContainerController>();

				// If we don't store and restore the original newHttpContext.RequestServices, the htmlHelper.ViewContext?.HttpContext.RequestServices 
				// object gets disposed when moduleScope is disposed.
				// ** NET core doesn't provide nested scopes, so when it disposes of ModuleScope it also disposes of moduleScope..ServiceProvider, which
				//    is equal to htmlHelper.ViewContext.HttpContext.RequestServices.

				originalServiceProvider = httpContext.RequestServices;

				// Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
				// executed, it gets DI objects from the module scope
				httpContext.RequestServices = moduleScope.ServiceProvider;

				IActionDescriptorCollectionProvider actionDescriptorProvider = moduleScope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
				ControllerActionDescriptor actionDescriptor = (ControllerActionDescriptor)actionDescriptorProvider.ActionDescriptors.Items
					.Where(descriptor => ((ControllerActionDescriptor)descriptor).ControllerTypeInfo.Equals(containerController.GetType().GetTypeInfo()))
					.Where(descriptor => ((ControllerActionDescriptor)descriptor).ActionName.Equals(nameof(IContainerController.RenderContainer)))
					.FirstOrDefault();

				// We must create a NEW routeData object (don't use htmlHelper.ViewContext.RouteData), because we must provide the controller, area and
				// action names for the container controller/action, rather than the route values for the original http request.
				foreach (var routeValue in actionDescriptor.RouteValues)
				{
					routeData.Values[routeValue.Key] = routeValue.Value;
				}

				ActionContext actionContext = new(httpContext, routeData, actionDescriptor);								
				ControllerContext controllerContext = new(actionContext);

				IActionInvoker actionInvoker = this.ActionInvokerFactory.CreateInvoker(controllerContext);

				// Catch the response
				httpContext.Response.Body = new MemoryStream();

				// Create the controller and run the controller action
				try
				{
					await actionInvoker.InvokeAsync();
				}
				finally
				{
					// Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
					httpContext.RequestServices = originalServiceProvider;
				}

				return httpContext.Response;
			}
		}

		/// <summary>
		/// Create a new IHtmlContent containing the body of the specified request.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// </remarks>
		private static IHtmlContent ToHtmlContent(HttpResponse response)
		{
			HtmlContentBuilder content = new();
            
			response.Body.Position = 0;
			using (var reader = new StreamReader(response.Body))
			{
				content.AppendHtml(reader.ReadToEnd());
			}

      return content;
		}		
	}
}
