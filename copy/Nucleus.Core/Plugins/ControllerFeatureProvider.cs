////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Text;
////using System.Threading.Tasks;
////using Microsoft.AspNetCore.Mvc.Controllers;
////using Microsoft.AspNetCore.Mvc.ApplicationParts;
////using System.Reflection;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.AspNetCore.Mvc.Infrastructure;

////builder.Services.AddSingleton<IControllerActivator>(new ControllerActivator());


////namespace Nucleus.Core.Plugins
////{
////	public class ControllerActivator : Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator
////	{
////    private IActionInvokerFactory ActionInvokerFactory { get; }

////    public object Create(ControllerContext controllerContext)
////		{
////      if (controllerContext == null)
////      {
////        throw new ArgumentNullException(nameof(controllerContext));
////      }

////      if (controllerContext.ActionDescriptor == null)
////      {
////        throw new ArgumentNullException(nameof(controllerContext.ActionDescriptor));
////      }

////      var controllerTypeInfo = controllerContext.ActionDescriptor.ControllerTypeInfo;

////      if (controllerTypeInfo == null)
////      {
////        throw new ArgumentNullException(nameof(controllerContext.ActionDescriptor.ControllerTypeInfo));
////      }

      
////      var serviceProvider = controllerContext.HttpContext.RequestServices;


////      return Microsoft.Extensions.DependencyInjection.ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, controllerTypeInfo.AsType());

////     // return serviceProvider.GetService(controllerTypeInfo.AsType());
////      //	await this.ActionInvokerFactory.CreateInvoker(controllerContext).InvokeAsync();
////      //return _typeActivatorCache.CreateInstance<object>(serviceProvider, controllerTypeInfo.AsType());
////    }

////    public void Release(ControllerContext context, object controller)
////		{
////      if (controller is IDisposable disposable)
////      {
////        disposable.Dispose();
////      }
////    }
////	}
////}

