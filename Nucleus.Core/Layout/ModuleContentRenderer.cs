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

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Renders the modules that make up a "pane" in a page.
	/// </summary>
	public class ModuleContentRenderer : IModuleContentRenderer
	{
		private Context Context { get; }
		private IActionInvokerFactory ActionInvokerFactory { get; }
		private IHttpContextFactory HttpContextFactory { get; }
		private ILogger<ModuleContentRenderer> Logger { get; }

		public ModuleContentRenderer(Context context, ILogger<ModuleContentRenderer> logger, IActionInvokerFactory actionInvokerFactory, IHttpContextFactory httpContextFactory)
		{
			this.Context = context;
			this.ActionInvokerFactory = actionInvokerFactory;
			this.HttpContextFactory = httpContextFactory;
		}

		/// <summary>
		/// Render the module instances within the specified pane.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="paneName"></param>
		/// <returns></returns>
		public async Task<IHtmlContent> RenderPaneAsync(IHtmlHelper htmlHelper, string paneName)
		{
			HtmlContentBuilder output = new();

			if (this.Context == null)
			{
				throw new InvalidOperationException($"{nameof(Context)} is null.");
			}

			if (this.Context.Page == null)
			{
				throw new InvalidOperationException($"{nameof(Context.Page)} is null.");
			}

			if (this.Context.Page.Modules == null)
			{
				throw new InvalidOperationException($"{nameof(Context.Page.Modules)} is null.");
			}

			// Render the module output if the module pane is the specified pane, and the user has permission to view it
			foreach (PageModule moduleInfo in this.Context.Page.Modules)
			{
				if (paneName == "*" || moduleInfo.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase))
				{
					// "permission denied" for viewing a module is not a failure, it just means we don't render the module
					if (HasViewPermission(moduleInfo, this.Context.Site, htmlHelper.ViewContext.HttpContext.User))
					{
						try
						{
							IHtmlContent moduleBody = await RenderModuleView(htmlHelper, moduleInfo, paneName != "*");
							output.AppendHtml(moduleBody);
						}
						catch (System.InvalidOperationException e)
						{
							// If an error occurred while rendering module, and the user is an admin, display the error message in place of the module
							// content.  If the user is not an admin, suppress output of the module.							
							this.Logger?.LogError(e, "Error rendering {pane}.", moduleInfo.Pane);
							if (htmlHelper.ViewContext.HttpContext.User.IsSiteAdmin(this.Context.Site))
							{
								output.AppendHtml(e.Message);
							}
						}
					}
				}
			}

			return output;
		}

		private IHtmlContent RenderException(HttpContext context, PageModule moduleInfo, Exception e)
		{
			TagBuilder output = new("div");

			if (context.User.IsSiteAdmin(this.Context.Site))
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
		private Boolean HasViewPermission(PageModule moduleInfo, Site site, System.Security.Claims.ClaimsPrincipal user)
		{
			return user.HasViewPermission(site, this.Context.Page, moduleInfo);
		}

		/// <summary>
		/// Returns a true/false value indicating whether the user has edit rights for the module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		private Boolean HasEditPermission(PageModule moduleInfo, Site site, System.Security.Claims.ClaimsPrincipal user)
		{
			return user.HasEditPermission(site, this.Context.Page, moduleInfo);
		}

		/// <summary>
		/// Render a module's "View" action.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="moduleInfo">Specifies the module being rendered.</param>
		/// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
		/// <returns></returns>

		public async Task<IHtmlContent> RenderModuleView(IHtmlHelper htmlHelper, PageModule moduleInfo, Boolean renderContainer)
		{
			HtmlContentBuilder output = new();
			System.Security.Claims.ClaimsPrincipal user = htmlHelper.ViewContext.HttpContext.User;

			if (HasViewPermission(moduleInfo, this.Context.Site, user))
			{
				HttpResponse moduleOutput = await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.ViewController, moduleInfo.ModuleDefinition.ViewAction, renderContainer);

				if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.Forbidden)
				{
					// modules can return a status of Forbidden to indicate that the user doesn't have access to a resource that
					// the module uses.  This should not be treated as a failure, it just means we don't render the module view.
					return output;
				}

				Boolean isEditing = user.IsEditing(htmlHelper.ViewContext?.HttpContext, this.Context.Site, this.Context.Page, moduleInfo);

				if (moduleOutput.StatusCode == (int)System.Net.HttpStatusCode.NoContent)
				{
					// modules can return NoContent to indicate that they have nothing to display, and should not be rendered (including that 
					// their container is not rendered.  We check to see if the user is editing the page & ignore NoContent so that editors
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
				TagBuilder editorBuilder = new("div");

				if (!String.IsNullOrEmpty(moduleInfo.Style))
				{
					editorBuilder.AddCssClass(moduleInfo.Style);
				}

				if (isEditing)
				{
					AddModuleEditControls(htmlHelper, editorBuilder, moduleInfo, user);
				}

				editorBuilder.InnerHtml.AppendHtml(ToHtmlContent(moduleOutput));

				output.AppendHtml(editorBuilder);
			}

			return output;
		}

		private void AddModuleEditControls(IHtmlHelper htmlHelper, TagBuilder editorBuilder, PageModule moduleInfo, System.Security.Claims.ClaimsPrincipal user)
		{
			//IUrlHelper urlHelper = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext);
			IUrlHelper urlHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(htmlHelper.ViewContext);

			// Render edit controls
			editorBuilder.AddCssClass("nucleus-module-editing");

			TagBuilder formBuilder = new("form");
			formBuilder.Attributes.Add("class", "nucleus-inline-edit-controls");

			// #refresh is a dummy value - we want nucleus-shared.js#_postPartialContent to process the clicks for the inline editing functions, so we need a 
			// non-blank data-target attribute so that the click event is bound to _postPartialContent.  
			// The value that we are using - "#refresh" - doesn't have any special meaning or code to process it in nucleus-shared.js.
			formBuilder.Attributes.Add("data-target", "#refresh");

      // users with module edit permissions can edit module settings and common settings
      if (!String.IsNullOrEmpty(moduleInfo.ModuleDefinition.EditAction))
      {
        formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildEditButton("&#xe3c9;", "Edit Content/Settings", urlHelper.Content("~/Admin/Pages/EditModule"), null));
      }
			formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildEditButton("&#xe8b8;", "Layout and Permissions Settings", urlHelper.Content("~/Admin/Pages/EditModuleCommonSettings"), null));

			// only render the delete control if the user has page-edit permissions
			if (user.HasEditPermission(this.Context.Site, this.Context.Page))
			{
				formBuilder.InnerHtml.AppendHtml(moduleInfo.BuildDeleteButton("&#xe14c;", "Delete Module", urlHelper.Content("~/Admin/Pages/DeletePageModuleInline"), null));
			}

			editorBuilder.InnerHtml.AppendHtml(formBuilder);
		}
		
		/// <summary>
		/// Render a module's "Edit" action.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="moduleInfo">Specifies the module being rendered.</param>
		/// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
		/// <returns></returns>
		public async Task<IHtmlContent> RenderModuleEditor(IHtmlHelper htmlHelper, PageModule moduleInfo, Boolean renderContainer)
		{
			if (moduleInfo.ModuleDefinition.EditAction != null && HasEditPermission(moduleInfo, this.Context.Site, htmlHelper.ViewContext.HttpContext.User))
			{
				HttpResponse moduleOutput = await BuildModuleOutput(htmlHelper, moduleInfo, String.IsNullOrEmpty(moduleInfo.ModuleDefinition.SettingsController) ? moduleInfo.ModuleDefinition.ViewController : moduleInfo.ModuleDefinition.SettingsController, moduleInfo.ModuleDefinition.EditAction, renderContainer);

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
		/// <param name="htmlHelper"></param>
		/// <param name="moduleinfo">Specifies the module being rendered.</param>
		/// <param name="action">Specifies the name of the action being rendered.</param>
		/// <param name="RenderContainer">Specifies whether to wrap the module output in a container.</param>
		/// <returns></returns>
		private async Task<HttpResponse> BuildModuleOutput(IHtmlHelper htmlHelper, PageModule moduleinfo, string controller, string action, Boolean RenderContainer)
		{
			Context scopedContext;
			IServiceProvider originalServiceProvider;
			RouteData routeData = new();

			HttpContext newHttpContext = this.HttpContextFactory.Create(htmlHelper.ViewContext?.HttpContext.Features);

			using (IServiceScope moduleScope = newHttpContext.RequestServices.CreateScope())
			{
				scopedContext = (Context)moduleScope.ServiceProvider.GetService(typeof(Context));

				// set context.Module to the module being processed
				scopedContext.Module = moduleinfo;
				scopedContext.Page = this.Context.Page;
				scopedContext.Site = this.Context.Site;
				scopedContext.LocalPath = this.Context.LocalPath;

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
						throw new InvalidOperationException($"Unable to load an action descriptor for the module '{moduleinfo.ModuleDefinition.FriendlyName}' [{controller}.{action}].");
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
					return await BuildContainerOutput(htmlHelper, moduleinfo, newHttpContext);
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
		/// <param name="htmlHelper"></param>
		/// <param name="moduleinfo">Specifies the module being rendered.</param>
		/// <param name="content">Rendered module output.</param>
		/// <returns></returns>
		/// <remarks>
		/// The rendered output of a container includes the module output.
		/// </remarks>
		private async Task<HttpResponse> BuildContainerOutput(IHtmlHelper htmlHelper, PageModule moduleinfo, HttpContext httpContext)
		{
			ContainerContext scopedContainerContext;
			IServiceProvider originalServiceProvider;
			RouteData routeData = new();

			// https://github.com/aspnet/Mvc/issues/6900

			Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();

			using (IServiceScope moduleScope = httpContext.RequestServices.CreateScope())
			{
				scopedContainerContext = (ContainerContext)moduleScope.ServiceProvider.GetService<ContainerContext>();
				scopedContainerContext.Site = context.Site;
				scopedContainerContext.Page = context.Page;
				scopedContainerContext.Module = moduleinfo;
				scopedContainerContext.Content = ToHtmlContent(httpContext.Response);

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
