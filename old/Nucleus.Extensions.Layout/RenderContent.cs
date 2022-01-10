//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc;
//using System.IO;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Http.Extensions;
//using Microsoft.AspNetCore.Mvc.Controllers;
//using Microsoft.AspNetCore.Mvc.Abstractions;
//using Microsoft.AspNetCore.Mvc.Infrastructure;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Routing;
//using System.Reflection;
//using Nucleus.Abstractions.Models;
//using Microsoft.Extensions.DependencyInjection;

//namespace Nucleus.Extensions.Layout
//{
//	/// <summary>
//	/// Renders the modules that make up a "pane" in a page.
//	/// </summary>
//	public class ModuleContentRenderer
//	{
//		private Context Context { get; }
//		private IActionInvokerFactory ActionInvokerFactory { get; }
//		private IHttpContextFactory HttpContextFactory { get; }

//		public ModuleContentRenderer(Context context, IActionInvokerFactory actionInvokerFactory, IHttpContextFactory httpContextFactory)
//		{
//			this.Context = context;
//			this.ActionInvokerFactory = actionInvokerFactory;
//			this.HttpContextFactory = httpContextFactory;
//		}

//		public async Task<IHtmlContent> RenderPaneAsync(IHtmlHelper htmlHelper, string paneName, Boolean renderInline)
//		{
//			HtmlContentBuilder output = new HtmlContentBuilder();

//			if (this.Context == null)
//			{
//				throw new ArgumentNullException(nameof(Context));
//			}

//			if (this.Context.Page == null)
//			{
//				throw new ArgumentNullException(nameof(Context.Page));
//			}

//			if (this.Context.Page.Modules == null)
//			{
//				throw new ArgumentNullException(nameof(Context.Page.Modules));
//			}

//			// Render the module output if the module pane is the specified pane, and the user has permission to view it
//			foreach (PageModule moduleInfo in this.Context.Page.Modules)
//			{
//				if (moduleInfo.Pane.Equals(paneName, StringComparison.OrdinalIgnoreCase))
//				{
//					foreach (Permission permission in moduleInfo.Permissions)
//					{
//						if (IsModuleViewPermission(permission))
//						{
//							if (htmlHelper.ViewContext.HttpContext.User.IsInRole(permission.Role.Name))
//							{
//								output.AppendHtml(ModuleOutputBuilder.Build(await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.ViewAction), renderInline));
//							}
//						}
//					}
//				}
//			}

//			return output;
//		}

//		private Boolean IsModuleViewPermission(Permission permission)
//		{
//			return
//				permission.AllowAccess &&
//				permission.PermissionType.AppliesTo == PermissionType.PermissionAppliesTo.Module &&
//				permission.PermissionType.Key == PermissionType.WELLKNOWN_KEYS.VIEW_MODULE;
//		}

//		public async Task<IHtmlContent> RenderModuleView(IHtmlHelper htmlHelper, PageModule moduleInfo, Boolean renderInline)
//		{
//			HtmlContentBuilder output = new HtmlContentBuilder();
//			output.AppendHtml(ModuleOutputBuilder.Build(await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.ViewAction), renderInline));
//			return output;
//		}

//		public async Task<IHtmlContent> RenderModuleEditor(IHtmlHelper htmlHelper, PageModule moduleInfo, Boolean renderInline)
//		{
//			HtmlContentBuilder output = new HtmlContentBuilder();
//			output.AppendHtml(ModuleOutputBuilder.Build(await BuildModuleOutput(htmlHelper, moduleInfo, moduleInfo.ModuleDefinition.EditAction), renderInline));
//			return output;
//		}

//		private async Task<IHtmlContent> BuildModuleOutput(IHtmlHelper htmlHelper, PageModule moduleinfo, string action)
//		{
//			Context scopedContext;
//			IServiceProvider originalServiceProvider;
//			RouteData routeData = new RouteData();

//			// https://github.com/aspnet/Mvc/issues/6900

//			HttpContext newHttpContext = this.HttpContextFactory.Create(htmlHelper.ViewContext?.HttpContext.Features);

//			using (IServiceScope moduleScope = newHttpContext.RequestServices.CreateScope())
//			{
//				scopedContext = (Context)moduleScope.ServiceProvider.GetService(typeof(Context));

//				// set context.Module to the module being processed
//				scopedContext.Module = moduleinfo;

//				// If we don't store and restore the original newHttpContext.RequestServices, the htmlHelper.ViewContext?.HttpContext.RequestServices 
//				// object gets disposed in between calls.
//				// ** NET core doesn't provide nested scopes, so when it disposes of ModuleScope it also disposes of moduleScope..ServiceProvider, which
//				//    is equal to htmlHelper.ViewContext.HttpContext.RequestServices.

//				originalServiceProvider = newHttpContext.RequestServices;

//				// Set context.RequestServices to the current module scope Service Provider, so when the module's controller is created and 
//				// executed, it gets DI objects from the module scope
//				newHttpContext.RequestServices = moduleScope.ServiceProvider;
//				ControllerActionDescriptor actionDescriptor = new ControllerActionDescriptor();

//				// TODO: Error handling here (moduleinfo.ModuleControllerType may be invalid)
//				// TODO: Cache refelected types?
//				actionDescriptor.ControllerTypeInfo = Type.GetType(moduleinfo.ModuleDefinition.ModuleControllerType).GetTypeInfo();

//				// TODO: Error handling here (moduleinfo.ViewAction may not exist in moduleinfo.ModuleControllerType)
//				// TODO: Cache refelected methods?
//				actionDescriptor.MethodInfo = actionDescriptor.ControllerTypeInfo.GetMethod(action);

//				// help the viewfinder (ViewResultExecutor) find the view.  We must create a NEW routeData object (don't
//				// use htmlHelper.ViewContext.RouteData), because we must provide our own controller and action names, and 
//				// NOT an Area name, because when this is called to render an editor from the admin panel, the area name
//				// would otherwise be set to "Admin"
//				routeData.Values["controller"] = actionDescriptor.ControllerTypeInfo.Name;
//				routeData.Values["action"] = action;

//				// Populate the actionDescriptor.Parameters list with action (method) parameters so that they are bound
//				actionDescriptor.Parameters = new List<ParameterDescriptor>();
//				foreach (ParameterInfo param in actionDescriptor.MethodInfo.GetParameters())
//				{
//					ParameterDescriptor paramDesc = new ParameterDescriptor();
//					paramDesc.Name = param.Name;
//					paramDesc.ParameterType = param.ParameterType;

//					actionDescriptor.Parameters.Add(paramDesc);
//				}

//				ActionContext actionContext = new ActionContext(newHttpContext, routeData, actionDescriptor);
//				ControllerContext controllerContext = new ControllerContext(actionContext);

//				IActionInvoker actionInvoker = this.ActionInvokerFactory.CreateInvoker(controllerContext);

//				// Catch the response
//				newHttpContext.Response.Body = new MemoryStream();

//				// Create the controller and run the controller action
//				await actionInvoker.InvokeAsync();

//				// Restore the original service provider before moduleScope is disposed to prevent it from also being disposed
//				newHttpContext.RequestServices = originalServiceProvider;

//				// Extract the response
//				newHttpContext.Response.Body.Position = 0;

//				using (var reader = new StreamReader(newHttpContext.Response.Body))
//				{
//					return ToHtmlContent(reader.ReadToEnd());
//				}
//			}
//		}

//		private static HtmlContentBuilder ToHtmlContent(string value)
//		{
//			HtmlContentBuilder content = new HtmlContentBuilder();
//			content.AppendHtml(value);
//			return content;
//		}

//	}
//}
