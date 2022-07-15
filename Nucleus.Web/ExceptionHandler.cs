//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Diagnostics;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Nucleus.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.Net.Http.Headers;
//using System.Linq;
//using Nucleus.Abstractions.Models;
//using Nucleus.Extensions.Authorization;
//using Microsoft.Extensions.DependencyInjection;

//namespace Nucleus.Web
//{
//	public static class ExceptionHandler
//	{
		
//		public static async Task HandleException(HttpContext httpContext, Func<Task> next)
//		{
//			IExceptionHandlerFeature exceptionDetails = httpContext.Features.Get<IExceptionHandlerFeature>();
//			Microsoft.AspNetCore.Mvc.ProblemDetails data;

//			if (exceptionDetails?.Error != null)
//			{
//				data = ParseException(exceptionDetails.Error);

//				if (httpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Accept].ToString().Contains("application/json"))
//				{
//					httpContext.Response.ContentType = "application/json";
//					httpContext.Response.StatusCode = data.Status.Value;
//					await httpContext.Response.WriteAsJsonAsync(data);					
//				}
//				else
//				{
//					// Not JSON: Write the error message if the user is a system admin
//					if (httpContext.User.IsSystemAdministrator())
//					{
//						httpContext.Response.ContentType = "text/plain";
//						httpContext.Response.StatusCode = data.Status.Value;
//						await httpContext.Response.WriteAsync(data.Detail);
//					}
//					else
//					{
//						// regular user, throw error 
//						await next();
//					}
//				}
//			}
//		}

//		private static Microsoft.AspNetCore.Mvc.ProblemDetails ParseException(Exception ex)
//		{
//			if (ex is Microsoft.Data.Sqlite.SqliteException)
//			{
//				return new()
//				{
//					Title = "Error",
//					Status = (int)System.Net.HttpStatusCode.InternalServerError,
//					Detail = ex.Message
//				};
//			}
//			else if (ex is Microsoft.AspNetCore.Http.BadHttpRequestException)
//			{
//				return new()
//				{
//					Title = "Error",
//					Status = (ex as Microsoft.AspNetCore.Http.BadHttpRequestException).StatusCode,
//					Detail = ex.Message
//				};
//			}
//			else
//			{
//				return new()
//				{
//					Title = "Error",
//					Status = (int)System.Net.HttpStatusCode.InternalServerError,
//					Detail = ex.Message
//				};
//			}
//		}
//	}
//}
