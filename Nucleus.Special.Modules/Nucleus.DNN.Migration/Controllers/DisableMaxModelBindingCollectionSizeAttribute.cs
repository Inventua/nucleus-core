using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Nucleus.DNN.Migration.Controllers;

/// <summary>
/// Disables MvcOptions.MaxModelBindingCollectionSize validation.
/// </summary>
/// <remarks>
/// This implementation disables MaxModelBindingCollectionSize validation for all future requests, regardless of Controller/Action.  It
/// is not a good implementation in this sense, it would be better to only modify MvcOptions for the current request, but it doesn't look 
/// like a copy of MvcOptions is added to HttpContext.Features.
/// 
/// If the DNN Migration extension were not intended for a "use and then uninstall" use case, this would be a very bad thing to
/// do, as it affects the setting until Nucleus is restarted.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DisableMaxModelBindingCollectionSizeAttribute : Attribute, IResourceFilter, IOrderedFilter
{  
  public int Order { get; set; } = 10000;

  /// <inheritdoc />
  public bool IsReusable => true;

  public void OnResourceExecuted(ResourceExecutedContext context)
  {
  }

  public void OnResourceExecuting(ResourceExecutingContext context)
  {
    var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();
    options.Value.MaxModelBindingCollectionSize = Int32.MaxValue;    
  }
}


