using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Nucleus.ViewFeatures;

/// <summary>
/// Extension methods for HttpContext.
/// </summary>
public static class HttpContextExtensions
{
  /// <summary>
  /// Return a custom "Nucleus redirect" response.  We use the X-Location header rather than location because browsers automatically follow
  /// regular redirects, and when we do a POST using jQuery.ajax we want to handle these as a full page redirect, rather than what the browser 
  /// would do, which is replace the AJAX response with the contents of the redirect location url.  Nucleus redirects are handled in nucleus-shared.js.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="location"></param>
  /// <returns></returns>
  public static StatusCodeResult NucleusRedirect(this HttpContext context, string location)
  {
    context.Response.Headers.Append("X-Location", location);
    return new StatusCodeResult((int)System.Net.HttpStatusCode.Found);
  }

}
