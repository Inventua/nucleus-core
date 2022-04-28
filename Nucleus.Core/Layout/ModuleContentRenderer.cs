using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
using Nucleus.Abstractions.Layout;
using Nucleus.Extensions.Authorization;

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
		
		public ModuleContentRenderer(Context context, IActionInvokerFactory actionInvokerFactory, IHttpContextFactory httpContextFactory)
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
				if (moduleInfo.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase))
				{	
					// "permission denied" for viewing a module is not a failure, it just means we don't render the module
					if (HasViewPermission(moduleInfo, this.Context.Site, htmlHelper.ViewContext.HttpContext.User))
					{
						try
						{
							IHtmlContent moduleBody = await RenderModuleView(htmlHelper, moduleInfo, true);
							output.AppendHtml(moduleBody);
						}
						catch (System.UnauthorizedAccessException)
						{
							// modules can throw an UnauthorizedAccessException to indicate that the user doesn't have access to a resource that
							// the module uses.  This should not be treated as a failure, it just means we don't render the module
						}
						catch (System.InvalidOperationException e)
						{
							// If the specified container was not found, and the user is an admin, display the error message in place of the module
							// content.  If the user is not an admin, suppress output of the module.
							if (e.Message.Contains("The view") && e.Message.Contains("was not found"))
							{
								htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ILogger<ModuleContentRenderer>>()?.LogError(e, "Error rendering {pane}.", moduleInfo.Pane);
								if (htmlHelper.ViewContext.HttpContext.User.IsSiteAdmin(this.Context.Site))
								{
									output.AppendHtml(e.Message);
								}
							}
							else
							{
								throw;
							}
						}
						// Removed in favour of Site.ErrorPageId/ErrorReport module
						//catch (Exception e)
						//{
						//	// For other exceptions, make sure a failure in a module doesn't prevent page load, but show something to indicate failure.
						//	IHtmlContent moduleErrorBody = RenderException(htmlHelper.ViewContext?.HttpContext, moduleInfo, e);
						//	output.AppendHtml(moduleErrorBody);
						//}
						}
				}
			}

			return output;
		}

		private IHtmlContent RenderException(HttpContext context, PageModule moduleInfo, Exception e)
		{
			//HtmlContentBuilder output = new();
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
		private Boolean HasViewPermission (PageModule moduleInfo, Site site, System.Security.Claims.ClaimsPrincipal user)
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
				IHtmlContent moduleOutput = await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.ViewAction, renderContainer);
				
				TagBuilder editorBuilder = new("div");
				
				if (!String.IsNullOrEmpty(moduleInfo.Style))
				{
					editorBuilder.AddCssClass(moduleInfo.Style);
				}

				AddModuleEditControls(htmlHelper, editorBuilder, moduleInfo, user);

				editorBuilder.InnerHtml.AppendHtml(moduleOutput);
				output.AppendHtml(editorBuilder);
			}
			return output;
		}

		private void AddModuleEditControls(IHtmlHelper htmlHelper, TagBuilder editorBuilder, PageModule moduleInfo, System.Security.Claims.ClaimsPrincipal user)
		{
			Boolean isEditable = user.IsEditing(htmlHelper.ViewContext?.HttpContext, this.Context.Site, this.Context.Page, moduleInfo);
			IUrlHelper urlHelper = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext);

			if (isEditable)
			{
				// Render edit controls
				editorBuilder.AddCssClass("nucleus-module-editing");

				TagBuilder formBuilder = new("form");
				formBuilder.Attributes.Add("class", "nucleus-inline-edit-controls");
				formBuilder.Attributes.Add("data-target", "#refresh");

				formBuilder.InnerHtml.AppendHtml(BuildEditButton("&#xe3c9;", "Edit", urlHelper.Content("~/Admin/Pages/EditModule"), moduleInfo, null));

				// only render the "common settings" and delete controls if the user has page-edit permissions
				if (user.HasEditPermission(this.Context.Site, this.Context.Page))
				{
					formBuilder.InnerHtml.AppendHtml(BuildEditButton("&#xe8b8;", "Settings", urlHelper.Content("~/Admin/Pages/EditModuleCommonSettings"), moduleInfo, null));
					formBuilder.InnerHtml.AppendHtml(BuildDeleteButton("&#xe14c;", "Delete", urlHelper.Content("~/Admin/Pages/DeletePageModule"), moduleInfo, null));
				}

				editorBuilder.InnerHtml.AppendHtml(formBuilder);
			}
		}

		private HtmlString BuildEditButton(string text, string title, string formaction, PageModule moduleInfo, IDictionary<string, string> attributes)
		{
			TagBuilder editControlBuilder = new("button");
			editControlBuilder.InnerHtml.SetContent(text);
			editControlBuilder.Attributes.Add("class", "nucleus-material-icon btn btn-secondary");
			editControlBuilder.Attributes.Add("title", title);
			editControlBuilder.Attributes.Add("type", "button");
			editControlBuilder.Attributes.Add("data-frametarget", ".nucleus-modulesettings-frame");
			editControlBuilder.Attributes.Add("formaction", $"{formaction}?mid={moduleInfo.Id}&mode=Standalone");
			
			if (attributes != null)
			{
				foreach (KeyValuePair<string,string> item in attributes)
				{
					editControlBuilder.Attributes.Add(item.Key, item.Value);
				}
			}

			StringWriter writer = new();
			editControlBuilder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);

			return new HtmlString(System.Web.HttpUtility.HtmlDecode(writer.ToString()));
		}

		private HtmlString BuildDeleteButton(string text, string title, string formaction, PageModule moduleInfo, IDictionary<string, string> attributes)
		{
			TagBuilder deleteControlBuilder = new("button");
			deleteControlBuilder.InnerHtml.SetContent(text);
			deleteControlBuilder.Attributes.Add("class", "nucleus-material-icon btn btn-danger");
			deleteControlBuilder.Attributes.Add("title", title);
			deleteControlBuilder.Attributes.Add("type", "submit");
			deleteControlBuilder.Attributes.Add("formaction", $"{formaction}?mid={moduleInfo.Id}");
			deleteControlBuilder.Attributes.Add("data-confirm", "Delete this module?");
			
			if (attributes != null)
			{
				foreach (KeyValuePair<string, string> item in attributes)
				{
					deleteControlBuilder.Attributes.Add(item.Key, item.Value);
				}
			}

			StringWriter writer = new();
			deleteControlBuilder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);

			return new HtmlString(System.Web.HttpUtility.HtmlDecode(writer.ToString()));
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
			HtmlContentBuilder output = new();
			if (HasEditPermission(moduleInfo, this.Context.Site, htmlHelper.ViewContext.HttpContext.User))
			{
				output.AppendHtml(await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.EditAction, renderContainer));
			}
			return output;
		}

		/// <summary>
		/// Render the specified action for a module.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="moduleinfo">Specifies the module being rendered.</param>
		/// <param name="action">Specifies the name of the action being rendered.</param>
		/// <param name="RenderContainer">Specifies whether to wrap the module output in a container.</param>
		/// <returns></returns>
		public async Task<IHtmlContent> BuildModuleOutput(IHtmlHelper htmlHelper, PageModule moduleinfo, string action, Boolean RenderContainer)
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
				scopedContext.Parameters = this.Context.Parameters;

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

					using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(moduleinfo.ModuleDefinition.ClassTypeName))
					{
						TypeInfo moduleControllerTypeInfo;
						Type moduleControllerType = Type.GetType(moduleinfo.ModuleDefinition.ClassTypeName);

						if (moduleControllerType == null)
						{
							// Module definition controller type is invalid
							throw new InvalidOperationException($"Module Definition is invalid: {moduleinfo.ModuleDefinition.ClassTypeName} not found.");
						}

						moduleControllerTypeInfo = moduleControllerType.GetTypeInfo();

						IActionDescriptorCollectionProvider actionDescriptorProvider = moduleScope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
						ControllerActionDescriptor actionDescriptor = (ControllerActionDescriptor)actionDescriptorProvider.ActionDescriptors.Items
							.Where(descriptor => ((ControllerActionDescriptor)descriptor).ControllerTypeInfo.Equals(moduleControllerTypeInfo))
							.Where(descriptor => ((ControllerActionDescriptor)descriptor).ActionName.Equals(action))
							.FirstOrDefault();

						// Report common error (Module definition invalid)
						if (actionDescriptor == null || actionDescriptor.MethodInfo == null)
						{
							throw new InvalidOperationException($"Module Definition is invalid: Action {action} does not exist in {moduleControllerTypeInfo}.");
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

						// We must create a NEW routeData object (don't use htmlHelper.ViewContext.RouteData), because we must provide the controller, area and
						// action names for the module, rather than the route values for the original http request.
						foreach (var routeValue in actionDescriptor.RouteValues)
						{
							routeData.Values[routeValue.Key] = routeValue.Value;
						}
						routeData.Values["extension"] = Nucleus.Core.Plugins.AssemblyLoader.GetExtensionFolderName(moduleControllerTypeInfo.Assembly.Location);


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

				
				if (newHttpContext.Response.StatusCode == (int)System.Net.HttpStatusCode.Forbidden)
				{
					throw new System.UnauthorizedAccessException();
				}

				// Extract the response
				newHttpContext.Response.Body.Position = 0;

				using (var reader = new StreamReader(newHttpContext.Response.Body))
				{
					if (RenderContainer)
					{
						// don't add style to the module "inner" content, it is added to the container's outer element
						return await BuildContainerOutput(htmlHelper, moduleinfo, ToHtmlContent(reader.ReadToEnd()));
					}
					else
					{
						// add style to outer div for modules which don't have a container assigned
						return ToHtmlContent(reader.ReadToEnd());
					}
				}
			}
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
		async Task<IHtmlContent> BuildContainerOutput(IHtmlHelper htmlHelper, PageModule moduleinfo, IHtmlContent content)
		{
			//Context scopedContext;
			ContainerContext scopedContainerContext;
			IServiceProvider originalServiceProvider;
			RouteData routeData = new();

			// https://github.com/aspnet/Mvc/issues/6900

			Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();
			HttpContext newHttpContext = this.HttpContextFactory.Create(htmlHelper.ViewContext?.HttpContext.Features);

			using (IServiceScope moduleScope = newHttpContext.RequestServices.CreateScope())
			{
				// set context.Module to the module being processed just in case somewhere down the line a component calls .GetService<Context>()
				//scopedContext = (Context)moduleScope.ServiceProvider.GetService<Context>();
				//scopedContext.Module = moduleinfo;

				scopedContainerContext = (ContainerContext)moduleScope.ServiceProvider.GetService<ContainerContext>();
				scopedContainerContext.Site = context.Site;
				scopedContainerContext.Page = context.Page;
				scopedContainerContext.Module = moduleinfo;
				scopedContainerContext.Content = content;
				
				Controller containerController = (Controller)moduleScope.ServiceProvider.GetService<IContainerController>();

				// If we don't store and restore the original newHttpContext.RequestServices, the htmlHelper.ViewContext?.HttpContext.RequestServices 
				// object gets disposed when moduleScope is disposed.
				// ** NET core doesn't provide nested scopes, so when it disposes of ModuleScope it also disposes of moduleScope..ServiceProvider, which
				//    is equal to htmlHelper.ViewContext.HttpContext.RequestServices.

				originalServiceProvider = newHttpContext.RequestServices;

				// Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
				// executed, it gets DI objects from the module scope
				newHttpContext.RequestServices = moduleScope.ServiceProvider;
				//ControllerActionDescriptor actionDescriptor = new();

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
								
				ActionContext actionContext = new(newHttpContext, routeData, actionDescriptor);
				ControllerContext controllerContext = new(actionContext);

				IActionInvoker actionInvoker = this.ActionInvokerFactory.CreateInvoker(controllerContext);

				// Catch the response
				newHttpContext.Response.Body = new MemoryStream();

				// Create the controller and run the controller action
				try
				{
					await actionInvoker.InvokeAsync();
				}
				finally
				{
					// Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
					newHttpContext.RequestServices = originalServiceProvider;
				}

				// Extract the response
				newHttpContext.Response.Body.Position = 0;

				using (var reader = new StreamReader(newHttpContext.Response.Body))
				{
					return ToHtmlContent(reader.ReadToEnd());
				}
			}
		}

		/// <summary>
		/// Create a new IHtmlContent containing the input value (string).
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// </remarks>
		private static IHtmlContent ToHtmlContent(string value)
		{
			HtmlContentBuilder content = new();

			//TagBuilder outputBuilder = new TagBuilder("div");			

			//outputBuilder.InnerHtml.AppendHtml(value);
			//content.AppendHtml(outputBuilder);
			content.AppendHtml(value);
			return content;
		}


	}
}
