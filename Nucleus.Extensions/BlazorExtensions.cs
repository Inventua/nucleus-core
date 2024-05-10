// This code is experimental.  It allows a Razor component to be returned from an MVC controller instead of a View.
// This code works, but because of Blazor limitations, we can't get a reference to HttpContext.Items, which means 
// we can't add script links to the output.
// The way that we can do that is to output a view from the controller, add the scripts there with @Html.AddScript,
// and add the Razor component to the view with <component type="typeof(...)" render-mode="ServerPrerendered" />

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Endpoints;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Controllers;
//using Microsoft.AspNetCore.Mvc.Infrastructure;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Nucleus.Abstractions.Models.Configuration;
//using Nucleus.Abstractions.Models.StaticResources;

//namespace Nucleus.Extensions;

///// <summary>
///// Extensions for using Blazor in Nucleus.
///// </summary>
//public class BlazorExtensions
//{
//  /// <summary>
//  ///  Creates a <see cref="ComponentViewResult"/> object to render a component type <typeparamref name="TComponent"/> 
//  ///  using the specified <paramref name="renderMode"/> with no input parameters.
//  /// </summary>
//  /// <typeparam name="TComponent"></typeparam>
//  /// <param name="renderMode"></param>
//  /// <returns></returns>
//  public static ActionResult ComponentView<TComponent>(Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode)
//    where TComponent : IComponent
//  {
//    return ComponentView<TComponent>(renderMode, ParameterView.Empty);
//  }

//  /// <summary>
//  ///  Creates a <see cref="ComponentViewResult"/> object to render a component type <typeparamref name="TComponent"/> 
//  ///  using the specified <paramref name="renderMode"/>, and passing in a <paramref name="model"/> parameter.
//  /// </summary>
//  /// <typeparam name="TComponent"></typeparam>
//  /// <param name="renderMode"></param>
//  /// <param name="model"></param>
//  /// <returns></returns>
//  public static ComponentViewResult ComponentView<TComponent>(Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object model)
//    where TComponent : IComponent
//  {
//    Dictionary<string, object> parameters = new()
//    {
//      { "Model", model }
//    };

//    return ComponentView<TComponent>(renderMode, ParameterView.FromDictionary(parameters));
//  }

//  /// <summary>
//  ///  Creates a <see cref="ComponentViewResult"/> object to render a component type <typeparamref name="TComponent"/> 
//  ///  using the specified <paramref name="renderMode"/>, and passing in the specified <paramref name="parameters"/>.
//  /// </summary>
//  /// <typeparam name="TComponent"></typeparam>
//  /// <param name="renderMode"></param>
//  /// <param name="parameters"></param>
//  /// <returns></returns>
//  public static ComponentViewResult ComponentView<TComponent>(Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, Microsoft.AspNetCore.Components.ParameterView parameters)
//    where TComponent : IComponent
//  {
//    return new ComponentViewResult(typeof(TComponent), renderMode, parameters);
//  }

//  /// <summary>
//  ///  ComponentView a <see cref="ComponentViewResult"/> object to render a component type <paramref name="componentType"/> 
//  ///  using the specified <paramref name="renderMode"/> with no input parameters.
//  /// </summary>
//  /// <param name="componentType"></param>
//  /// <param name="renderMode"></param>
//  /// <returns></returns>
//  public static ComponentViewResult ComponentView(Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode)
//  {
//    return ComponentView(componentType, renderMode, ParameterView.Empty);
//  }

//  /// <summary>
//  ///  Creates a <see cref="ComponentViewResult"/> object to render a component type <paramref name="componentType"/> 
//  ///  using the specified <paramref name="renderMode"/>, and passing in a <paramref name="model"/> parameter.
//  /// </summary>
//  /// <param name="componentType"></param>
//  /// <param name="renderMode"></param>
//  /// <param name="model"></param>
//  /// <returns></returns>
//  public static ComponentViewResult ComponentView(Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object model)
//  {
//    Dictionary<string, object> parameters = new()
//    {
//      { "Model", model }
//    };

//    return ComponentView(componentType, renderMode, ParameterView.FromDictionary(parameters));
//  }

//  /// <summary>
//  ///  Creates a <see cref="ComponentViewResult"/> object to render a component type <paramref name="componentType"/> 
//  ///  using the specified <paramref name="renderMode"/>, and passing in the specified <paramref name="parameters"/>.  
//  /// </summary>
//  /// <param name="componentType"></param>
//  /// <param name="renderMode"></param>
//  /// <param name="parameters"></param>
//  /// <returns></returns>
//  /// <exception cref="ArgumentException"></exception>
//  public static ComponentViewResult ComponentView(Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, ParameterView parameters)
//  {
//    if (componentType is not IComponent) throw new ArgumentException(nameof(componentType));
//    return new ComponentViewResult(componentType, renderMode, parameters);
//  }
//}

///// <summary>
///// Represents an <see cref="ActionResult"/> that renders a Blazor component to the response.
///// </summary>
//public class ComponentViewResult : ActionResult
//{
//  internal ComponentViewResult(System.Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, ParameterView parameters)
//  {
//    this.ComponentType = componentType;
//    this.RenderMode = renderMode;
//    this.Parameters = parameters;
//  }

//  /// <summary>
//  /// Gets or sets the HTTP status code.
//  /// </summary>
//  public int? StatusCode { get; set; }

//  /// <summary>
//  /// Specifies the render mode of the component.
//  /// </summary>
//  public Microsoft.AspNetCore.Mvc.Rendering.RenderMode RenderMode { get; }

//  /// <summary>
//  /// Input parameters for the Blazor component.
//  /// </summary>
//  public ParameterView Parameters { get; }

//  /// <summary>
//  /// Specifies the component type, which must implement <see cref="Microsoft.AspNetCore.Components.IComponent"/>>.
//  /// </summary>
//  public System.Type ComponentType { get; }

//  /// <inheritdoc />
//  public override async Task ExecuteResultAsync(ActionContext context)
//  {
//    ArgumentNullException.ThrowIfNull(context);

//    var executor = context.HttpContext.RequestServices.GetService<IActionResultExecutor<ComponentViewResult>>();
//    if (executor == null)
//    {
//      throw new InvalidOperationException("No IActionResultExecutor<ComponentViewResult> type is registered.");
//    }

//    await executor.ExecuteAsync(context, this);
//  }
//}

///// <summary>
///// Render a ComponentViewResult resposne.
///// </summary>
//public class ComponentViewResultExecutor : IActionResultExecutor<ComponentViewResult>
//{
//  /// <inheritdoc/>
//  public async Task ExecuteAsync(ActionContext context, ComponentViewResult result)
//  {
//    ArgumentNullException.ThrowIfNull(context);
//    ArgumentNullException.ThrowIfNull(result);

//    var componentRenderer = context.HttpContext.RequestServices.GetRequiredService<IComponentPrerenderer>();
//    IHtmlAsyncContent content = await componentRenderer.PrerenderComponentAsync(context.HttpContext, result.ComponentType, MapRenderMode(result.RenderMode), result.Parameters);

//    context.HttpContext.Response.ContentType = "text/html";

//    if (result.StatusCode != null)
//    {
//      context.HttpContext.Response.StatusCode = result.StatusCode.Value;
//    }

//    StreamWriter writer = new(context.HttpContext.Response.Body);

//    try
//    {
//      await content.WriteToAsync(writer);
//      await writer.FlushAsync(context.HttpContext.RequestAborted);
//    }
//    catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
//    {
//    }

//    AddBlazorScript(context, result);
//  }

//  /// <summary>
//  /// Add Blazor js script.
//  /// </summary>
//  /// <param name="actionContext"></param>
//  /// <param name="result"></param>
//  /// <remarks>
//  /// This function automatically adds Blazor scripts to the page output. 
//  /// </remarks>
//  internal static void AddBlazorScript(ActionContext actionContext, ComponentViewResult result)
//  {
//    string version = ((ControllerActionDescriptor)actionContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version.ToString();
//    AddScript(actionContext.HttpContext, "_framework/blazor.web.js", false, true, 1000, version);
//  }

//  /// <summary>
//  /// We have to implement our own AddScript here, because we can't reference Nucleus.ViewFeatures.
//  /// </summary>
//  internal static IHtmlContent AddScript(HttpContext context, string scriptPath, Boolean isAsync, Boolean isDynamic, int order, string version)
//  {
//    ResourceFileOptions resourceFileOptions = context.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
//    PageResources pageResources = (PageResources)context.Items[Nucleus.Abstractions.Models.StaticResources.PageResources.ITEMS_KEY] ?? new();

//    if (!pageResources.Scripts.ContainsKey(scriptPath))
//    {
//      pageResources.Scripts.Add(scriptPath, new Nucleus.Abstractions.Models.StaticResources.Script()
//      {
//        Path = scriptPath,
//        IsAsync = isAsync,
//        IsDynamic = isDynamic,
//        Order = order,
//        Version = version,
//        IsExtensionScript = scriptPath.StartsWith("/" + Nucleus.Abstractions.RoutingConstants.EXTENSIONS_ROUTE_PATH, StringComparison.OrdinalIgnoreCase)
//      });

//      context.Items[Nucleus.Abstractions.Models.StaticResources.PageResources.ITEMS_KEY] = pageResources;
//    }

//    return new HtmlContentBuilder();
//  }

//  internal static IComponentRenderMode MapRenderMode(Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode) => renderMode switch
//  {
//    Microsoft.AspNetCore.Mvc.Rendering.RenderMode.Static => null,
//    Microsoft.AspNetCore.Mvc.Rendering.RenderMode.Server => new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode(prerender: false),
//    Microsoft.AspNetCore.Mvc.Rendering.RenderMode.ServerPrerendered => Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer,
//    Microsoft.AspNetCore.Mvc.Rendering.RenderMode.WebAssembly => new Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode(prerender: false),
//    Microsoft.AspNetCore.Mvc.Rendering.RenderMode.WebAssemblyPrerendered => Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveWebAssembly,
//    _ => throw new ArgumentException($"Unsupported render mode {renderMode}", nameof(renderMode)),
//  };
//}

