using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions;

namespace Nucleus.Core.FileSystemProviders
{
	public class FileIntegrityCheckerMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
		private IOptions<FileSystemProviderFactoryOptions> Options { get; }
		private ILogger<FileIntegrityCheckerMiddleware> Logger { get; }
		private Context Context { get; }

		public FileIntegrityCheckerMiddleware(IOptions<FileSystemProviderFactoryOptions> options, Context context, ILogger<FileIntegrityCheckerMiddleware> logger)
		{
			this.Options = options;
			this.Logger = logger;
			this.Context = context;
		}

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			if (context.Request.HasFormContentType && (context.Request.Path != $"/{RoutingConstants.ERROR_ROUTE_PATH}"))
			{
				foreach (IFormFile file in context.Request.Form.Files)
				{
					if (context.User.IsInRole(this.Context.Site.AnonymousUsersRole.Name))
					{
						Logger.LogWarning("File upload by anonymous user [filename: {filename}] blocked.", file.FileName);
						context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
						return;
					}

					AllowedFileType fileType = this.Options.Value.AllowedFileTypes.Where(allowedtype => allowedtype.FileExtensions.Contains(System.IO.Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)).FirstOrDefault();
					Boolean isValid = false;
					if (fileType != null)
					{
						if (fileType.Restricted && !context.User.IsSiteAdmin(this.Context.Site))
						{
							Logger.LogWarning("File upload [filename: {filename}] blocked: File type Permission Denied.", file.FileName);
							context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
							return;
						}

						using (System.IO.Stream stream = file.OpenReadStream())
						{
							isValid = fileType.IsValid(stream);

							if (!isValid)
							{
								Logger.LogError("ALERT: File content of file '{filename}' uploaded by {userid} : signature [{sample}] does not match any of the file signatures for file type {filetype}.", file.FileName, context.User.GetUserId(), BitConverter.ToString(Nucleus.Extensions.AllowedFileTypeExtensions.GetSample(stream)).Replace("-", ""), System.IO.Path.GetExtension(file.FileName));
								context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
								await context.Response.WriteAsync($"Invalid file content.  The file that you have uploaded is not a valid {System.IO.Path.GetExtension(file.FileName)} file.");
								return;
							}
						}
					}
					else
					{
						Logger.LogError("ALERT: Unsupported file type.  File '{filename}' uploaded by {userid} does not match any of the allowed file extensions.", file.FileName, context.User.GetUserId());
						context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
						await context.Response.WriteAsync($"Invalid file content.  The file type of the file that you have uploaded is not allowed.");
						return;
					}
				}
			}

			await next(context);
		}

	}

}
