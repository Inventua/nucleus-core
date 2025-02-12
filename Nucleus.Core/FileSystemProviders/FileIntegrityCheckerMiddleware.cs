﻿using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.FileSystemProviders;

public class FileIntegrityCheckerMiddleware : Microsoft.AspNetCore.Http.IMiddleware
{
  private IOptions<FileSystemProviderFactoryOptions> Options { get; }
  private ILogger<FileIntegrityCheckerMiddleware> Logger { get; }
  private Context Context { get; }
  private const long MAX_FILE_SIZE_BYTES = 67108864;

  public FileIntegrityCheckerMiddleware(IOptions<FileSystemProviderFactoryOptions> options, Context context, ILogger<FileIntegrityCheckerMiddleware> logger)
  {
    this.Options = options;
    this.Logger = logger;
    this.Context = context;
  }

  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    if (HasFormDataContentType(context) && (context.Request.Path != $"/{RoutingConstants.ERROR_ROUTE_PATH}"))
    {
      // Special case for the extensions installer: Increase maximum request size to 64mb if it is less than that.
      // This code is required because reading the file bytes indirectly sets IHttpMaxRequestBodySizeFeature.IsReadOnly
      // to true, which prevents the RequestSizeLimitAttribute from working.
      if (context.Request.Path.Equals("/admin/extensions/upload", StringComparison.OrdinalIgnoreCase))
      {
        Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature max = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (max != null && max.MaxRequestBodySize.HasValue && max.MaxRequestBodySize.Value < MAX_FILE_SIZE_BYTES)
        {
          max.MaxRequestBodySize = MAX_FILE_SIZE_BYTES;
        }
      }

      // Using .ReadFormAsync is a performance recommendation from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-7.0
      IFormCollection form = await context.Request.ReadFormAsync();

      foreach (IFormFile file in form.Files)
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

  /// <summary>
  /// Returns whether the request has a content type which can include file uploads.
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  private static Boolean HasFormDataContentType(HttpContext context)
  {
    if (context.Request.ContentType == null) return false;
    if (MediaTypeHeaderValue.TryParse(context.Request.ContentType, out MediaTypeHeaderValue mediaHeaderValue))
    {
      return mediaHeaderValue?.MediaType == "multipart/form-data";
    }
    return false;
  }
}
