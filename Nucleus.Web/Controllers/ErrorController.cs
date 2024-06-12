using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers;

public class ErrorController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private ApplicationPartManager ApplicationPartManager { get; }
  private Context Context { get; }
  private IPageManager PageManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private Application Application { get; }
  private ILogger<ErrorController> Logger { get; }

  public ErrorController(IWebHostEnvironment webHostEnvironment, ApplicationPartManager applicationPartManager, Context context, Application application, IFileSystemManager fileSystemManager, IPageManager pageManager, ILogger<ErrorController> logger)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.ApplicationPartManager = applicationPartManager;

    this.Context = context;
    this.Application = application;
    this.FileSystemManager = fileSystemManager;
    this.PageManager = pageManager;
    this.Logger = logger;
  }

  public async Task<IActionResult> Index()
  {
    IExceptionHandlerFeature exceptionDetails = ControllerContext.HttpContext.Features.Get<IExceptionHandlerFeature>();
    Microsoft.AspNetCore.Mvc.ProblemDetails data;
    Page errorPage = null;

    if (exceptionDetails?.Error != null)
    {
      //// try to convert database errors to a more friendly message
      //// This code has been moved to individial database providers which are called by Nucleus.Data.EntityFramework.ExceptionInterceptor
      //// Exception ex = exceptionDetails.Error.Parse();
      //// data = WrapException(ex);
      data = WrapException(exceptionDetails.Error);
      if (ControllerContext.HttpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Accept].ToString().Contains("application/json"))
      {
        ControllerContext.HttpContext.Response.ContentType = "application/json";
        ControllerContext.HttpContext.Response.StatusCode = data.Status.Value;

        return Json(data);
      }
    }
    else
    {
      // no error.  User has manually navigated to the error page
      data = new Microsoft.AspNetCore.Mvc.ProblemDetails();
    }

    if (this.Context.Site != null)
    {
      SitePages sitePages = this.Context.Site.GetSitePages();
      if (sitePages.ErrorPageId.HasValue)
      {
        errorPage = await this.PageManager.Get(sitePages.ErrorPageId.Value);
      }
    }

    if (errorPage != null)
    {
      this.Context.Page = errorPage;      
      return View(DefaultController.GetLayoutPath(this.WebHostEnvironment, this.ApplicationPartManager, this.Context, this.Logger), await DefaultController.BuildViewModel(this.Url, this.Context, this.HttpContext, this.Application, this.FileSystemManager, this.Logger));
    }
    else
    {
      // no error page, or Context.Site is null
      if (data != null)
      {
        if (IsConnectionFailure(exceptionDetails.Error))
        {
          string innerMessage = UnwrapInnerExceptions(exceptionDetails.Error);
          if (!String.IsNullOrEmpty(innerMessage))
          {
            innerMessage = "\n\n- " + innerMessage;
          }

          // Special case.  Display database connection errors regardless of user, because database connection configuration is a likely/common misconfiguration.
          return new Microsoft.AspNetCore.Mvc.ContentResult()
          {
            ContentType = "text/plain",
            StatusCode = data.Status.Value,
            Content = $"{Assembly.GetExecutingAssembly().Product()} version {Assembly.GetExecutingAssembly().Version()}.\n\n{exceptionDetails.Error.Message}{innerMessage}"
          };
        }
        else if (exceptionDetails.Error is System.UnauthorizedAccessException)
        {
          // Special case.  File permissions errors regardless of user, because file permission errors are a likely/common misconfiguration.
          return new Microsoft.AspNetCore.Mvc.ContentResult()
          {
            ContentType = "text/plain",
            StatusCode = data.Status.Value,
            Content = $"{Assembly.GetExecutingAssembly().Product()} version {Assembly.GetExecutingAssembly().Version()}.\n\n{exceptionDetails.Error.Message}"
          };
        }
        // write the error message to the response if the user is a system admin
        else if (ControllerContext.HttpContext.User.IsSystemAdministrator())
        {
          return new Microsoft.AspNetCore.Mvc.ContentResult()
          {
            ContentType = "text/plain",
            StatusCode = data.Status.Value,
            Content = data.Detail
          };
        }
        else
        {
          // regular user, show generic error 
          return new Microsoft.AspNetCore.Mvc.ContentResult()
          {
            ContentType = "text/plain",
            StatusCode = data.Status.Value,
            Content = "An error occurred while processing your request."
          };
        }
      }
    }

    return new Microsoft.AspNetCore.Mvc.ContentResult()
    {
      ContentType = "text/plain",
      StatusCode = 404,
      Content = "This site does not have an error page configured."
    };
  }

  private static string UnwrapInnerExceptions(Exception e)
  {
    string message = "";

    while (e.InnerException != null)
    {
      if (!String.IsNullOrEmpty(message))
      {
        message += "\n- ";
      }

      message += e.InnerException.Message;
      e = e.InnerException;
    }

    return message;
  }

  private static Boolean IsConnectionFailure(Exception e)
  {
    return e is Nucleus.Data.Common.ConnectionException;
  }

  private static Microsoft.AspNetCore.Mvc.ProblemDetails WrapException(Exception ex)
  {
    if (ex is Nucleus.Abstractions.DataProviderException)
    {
      return new()
      {
        Title = "Error",
        Status = (int)System.Net.HttpStatusCode.Conflict,
        Detail = ex.Message
      };
    }
    else if (ex is Microsoft.AspNetCore.Http.BadHttpRequestException)
    {
      return new()
      {
        Title = "Error",
        Status = (ex as Microsoft.AspNetCore.Http.BadHttpRequestException).StatusCode,
        Detail = ex.Message
      };
    }
    else if (ex is System.Data.Common.DbException || ex?.InnerException is System.Data.Common.DbException)
    {
      return new()
      {
        Title = "Error",
        Status = (int)System.Net.HttpStatusCode.InternalServerError,
        Detail = ex.InnerException == null ? ex.Message : ex.InnerException.Message
      };
    }
    else
    {
      return new()
      {
        Title = "Error",
        Status = (int)System.Net.HttpStatusCode.InternalServerError,
        Detail = ex.GetBaseException().Message
      };
    }
  }
}
