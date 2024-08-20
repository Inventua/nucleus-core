using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Web.Controllers;

/// <summary>
/// Check permissions and return the requested file.
/// </summary>
public class FileController : Controller
{
  private ILogger<FileController> Logger { get; }
  private Context Context { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IOptions<FileSystemProviderFactoryOptions> Options { get; }

  public FileController(IOptions<FileSystemProviderFactoryOptions> options, ILogger<FileController> logger, Context context, IFileSystemManager fileSystemManager)
  {
    this.Options = options;
    this.Logger = logger;
    this.Context = context;
    this.FileSystemManager = fileSystemManager;
  }

  [HttpGet]
  [Route("/" + Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX + "/{providerKey}/{**path}")]
  public async Task<ActionResult> Index(string providerKey, string path, Boolean inline)
  {
    this.Logger.LogInformation("File {providerKey}/{path} requested.", providerKey, path);

    try
    {
      File file = await this.FileSystemManager.GetFile(this.Context.Site, providerKey, path);

      if (file != null)
      {
        return await Index(file, inline);
      }
      else
      {
        this.Logger.LogInformation("File {providerKey}/{path} not found.", providerKey, path);
        return NotFound();
      }
    }
    catch (System.IO.FileNotFoundException)
    {
      this.Logger.LogInformation("File {providerKey}/{path} not found.", providerKey, path);
      return NotFound();
    }
  }

  [HttpGet]
  [Route("/" + Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX + "/{encodedpath}")]
  public async Task<ActionResult> Index(string encodedPath, Boolean inline)
  {
    //string idString;
    Guid fileId;

    this.Logger.LogTrace("File with encoded path {encodedPath} requested.", encodedPath);

    try
    {
      fileId = Nucleus.Extensions.FileExtensions.DecodeFileId(encodedPath);
    }
    catch (InvalidOperationException ex)
    {
      this.Logger.LogError(ex, "Encoded path '{encodedPath}' is not valid.", encodedPath);
      return BadRequest();
    }

    try
    {
      File file = await this.FileSystemManager.GetFile(this.Context.Site, fileId);

      if (file != null)
      {
        return await Index(file, inline);
      }
      else
      {
        this.Logger.LogInformation("File {id} not found.", fileId);
        return NotFound();
      }
    }
    catch (System.IO.FileNotFoundException)
    {
      this.Logger.LogInformation("File {id} not found.", fileId);
      return NotFound();
    }
  }

  private async Task<ActionResult> Index(File file, Boolean inline)
  {
    ArgumentNullException.ThrowIfNull(file);

    try
    {
      Microsoft.AspNetCore.Http.Headers.ResponseHeaders headers = Response.GetTypedHeaders();
      DateTime lastModifiedDate = file.DateModified;

      this.Logger.LogTrace("File {provider}/{path} found.", file.Provider, file.Path);

      // Verify that the file extension is allowed
      if (!this.Options.Value.AllowedFileTypes.Where(allowedtype => allowedtype.FileExtensions.Contains(System.IO.Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase)).Any())
      {
        return BadRequest();
      }

      Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, file.Parent.Id);

      // As of 1.0.1, .GetFolder(site,id) always populates permissions
      ////if (folder != null)
      ////{
      ////	folder.Permissions = await this.FileSystemManager.ListPermissions(folder);
      ////}

      if (folder != null && HttpContext.User.HasViewPermission(this.Context.Site, folder))
      {
        DateTimeOffset? ifModifiedSince = ControllerContext.HttpContext.Request.GetTypedHeaders().IfModifiedSince;
        if (ifModifiedSince.HasValue && ifModifiedSince.Value >= lastModifiedDate)
        {
          return StatusCode((int)System.Net.HttpStatusCode.NotModified);
        }

        headers.LastModified = lastModifiedDate;
        headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
          Public = true,
          MaxAge = TimeSpan.FromDays(30)
        };

        // For file system providers that support it, we redirect to the "direct" url for files, so as
        // to avoid additional data traffic charges in cloud-hosted environments.  
        // By rendering a Nucleus file link (to this controller) and then returning a redirect, we still check
        // for Nucleus permissions, but are using the direct link to serve the file content.
        if (file.Capabilities.CanDirectLink)
        {
          System.Uri uri = await this.FileSystemManager.GetFileDirectUrl(this.Context.Site, file);
          if (uri != null)
          {
            return Redirect(uri.AbsoluteUri);
          }
        }

        headers.ContentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue(inline ? "inline" : "attachment")
        {
          FileName = file.Name,
        };

        return File(await this.FileSystemManager.GetFileContents(this.Context.Site, file), file.GetMIMEType(true));
      }
      else
      {
        this.Logger.LogInformation("File not specified (null).");
        return NotFound();
      }
    }
    catch (System.IO.FileNotFoundException)
    {
      this.Logger.LogInformation("File '{provider}/{path}' not found.", file.Provider, file.Path);
      return NotFound();
    }
  }
}
