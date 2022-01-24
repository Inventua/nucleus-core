using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers
{
	public class Error : Controller
	{
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private IFileSystemManager FileSystemManager { get; }


		public Error(Context context, IFileSystemManager fileSystemManager, IPageManager pageManager)
		{
			this.Context = context;
			this.FileSystemManager = fileSystemManager;
			this.PageManager = pageManager;
		}

		public async Task<IActionResult> Index()
		{
			IExceptionHandlerFeature exceptionDetails = ControllerContext.HttpContext.Features.Get<IExceptionHandlerFeature>();
			Microsoft.AspNetCore.Mvc.ProblemDetails data;

			if (exceptionDetails?.Error != null)
			{
				data = ParseException(exceptionDetails.Error);

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
				Page errorPage;
				if (sitePages.ErrorPageId.HasValue)
				{
					errorPage = await this.PageManager.Get(sitePages.ErrorPageId.Value);
					if (errorPage != null)
					{
						this.Context.Page = errorPage;

						ViewModels.Default viewModel = new(this.Context);

						viewModel.IsEditing = User.IsEditing(HttpContext, this.Context.Site, this.Context.Page);
						viewModel.CanEdit = User.CanEditContent(this.Context.Site, this.Context.Page);
						viewModel.SiteIconPath = await Context.Site.GetIconPath(this.FileSystemManager);
						viewModel.SiteCssFilePath = await Context.Site.GetCssFilePath(this.FileSystemManager);

						return View(this.Context.Page.LayoutPath(this.Context.Site), viewModel);
					}
					else
					{
						// no error page
						if (data != null)
						{
							// write the error message to the response if the user is a system admin
							if (ControllerContext.HttpContext.User.IsSystemAdministrator())
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
				}
			}

			return new Microsoft.AspNetCore.Mvc.ContentResult()
			{
				ContentType = "text/plain",
				StatusCode = 404,
				Content = "This site does not have an error page configured."
			};
		}

		private Microsoft.AspNetCore.Mvc.ProblemDetails ParseException(Exception ex)
		{
			if (ex is Microsoft.Data.Sqlite.SqliteException)
			{
				return new()
				{
					Title = "Error",
					Status = (int)System.Net.HttpStatusCode.InternalServerError,
					Detail = ex.Message
				};
			}
			else if (ex is Microsoft.EntityFrameworkCore.DbUpdateException)
			{
				return new()
					{
						Title = "Error",
						Status = (int)System.Net.HttpStatusCode.InternalServerError,
						Detail = ex.InnerException == null ? ex.Message : ex.InnerException.Message
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
			else
			{
				return new()
				{
					Title = "Error",
					Status = (int)System.Net.HttpStatusCode.InternalServerError,
					Detail = ex.Message
				};
			}
		}

		//private class ProblemData
		//{
		//	public string Title { get; set; }
		//	public int StatusCode { get; set; }
		//	public string Message { get; set; }
		//	public string TraceId { get; set; }

		//}
	}
}
