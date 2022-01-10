using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Linq;
using Nucleus.Abstractions.Models;
using Nucleus.Core.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Web
{
	public static class ExceptionHandler
	{
		public static async Task HandleException(HttpContext httpContext, Func<Task> next)
		{
			IExceptionHandlerFeature exceptionDetails = httpContext.Features.Get<IExceptionHandlerFeature>();
			Exception ex = exceptionDetails?.Error;
			
			if (ex != null)
			{
				if (httpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Accept].ToString().Contains("application/json"))
				{
					if (ex is Microsoft.Data.Sqlite.SqliteException)
					{

					};

					ProblemData data = new()
					{
						Title="Error",
						StatusCode = (int)System.Net.HttpStatusCode.InternalServerError,
						Message = ex.Message,
						TraceId = httpContext.TraceIdentifier
					};

					httpContext.Response.ContentType = "application/json";
					httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
					await System.Text.Json.JsonSerializer.SerializeAsync(httpContext.Response.Body, data);
					return;
				}
				else
				{
					httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

					// only write the error message if the user is a system admin
					if (httpContext.User.IsSystemAdministrator())
					{
						httpContext.Response.ContentType = "text/plain";
						await httpContext.Response.WriteAsync(ex.Message);
					}
					
					return;
				}
			}
			
			// If the request didn't specify that it accepts application/json, just let it fail as normal
			await next.Invoke();
		}

		private class ProblemData
		{
			public string Title { get; set; }
			public int StatusCode { get; set; }
			public string Message { get; set; }
			public string TraceId { get; set; }
			
		}
  }
}
