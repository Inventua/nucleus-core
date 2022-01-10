using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Core;

namespace Nucleus.Web.Controllers
{	
	/// <summary>
	/// Display a page and module content, using the selected layout
	/// </summary>
	public class FileController : Controller
	{
		private ILogger<DefaultController> Logger { get; }
		private Context Context { get; }
		private FileSystemManager FileSystemManager { get; }

		public FileController(FileSystemProviderFactory fileSystemProviderFactory, ILogger<DefaultController> logger, Context context, FileSystemManager fileSystemManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		[Route("/files/{providerKey}/{path}")]
		public ActionResult Index(string providerKey, string path, Boolean inline)
		{
			this.Logger.LogInformation("File {0}/{1} requested.", providerKey, path);
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(providerKey);
			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();
			string mimeType = "application/octet-stream";

			try
			{
				File file = this.FileSystemManager.GetFile(this.Context.Site, providerKey, path);

				if (file != null)
				{
					DateTime lastModifiedDate = file.DateModified;
					
					this.Logger.LogInformation("File {0}/{1} found.", providerKey, path);

					Folder folder = this.FileSystemManager.GetFolder(this.Context.Site, file.Provider, file.Parent.Path);
					if (folder != null)
					{
						folder.Permissions = this.FileSystemManager.ListPermissions(folder);
					}

					if (folder != null && HttpContext.User.HasViewPermission(this.Context.Site, folder))
					{
						
						DateTimeOffset? ifModifiedSince = ControllerContext.HttpContext.Request.GetTypedHeaders().IfModifiedSince;
						if (ifModifiedSince.HasValue && ifModifiedSince.Value >= lastModifiedDate)
						{						
							return StatusCode((int)System.Net.HttpStatusCode.NotModified);						
						}

						System.Net.Http.Headers.ContentDispositionHeaderValue contentDisposition = new(inline ? "inline" : "attachment")
						{
							FileName = file.Name,
						};
					
						Response.Headers.Add(Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition, contentDisposition.ToString());
						Response.GetTypedHeaders().LastModified = lastModifiedDate;

						extensionProvider.TryGetContentType(path, out mimeType);

						return File(this.FileSystemManager.GetFileContents(this.Context.Site, file.Provider, path), mimeType);
					}
					else
					{
						this.Logger.LogInformation("File {0}/{1} permission denied for {2}.", providerKey, path, HttpContext.User.Identity.Name);
						return Forbid();
					}
				}
				else
				{
					this.Logger.LogInformation("File {0}/{1} not found.", providerKey, path);
					return NotFound();
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				this.Logger.LogInformation("File {0}/{1} not found.", providerKey, path);
				return NotFound();
			}
		}

		[HttpGet]
		[Route("/files/{encodedpath}")]
		public ActionResult Index(string encodedPath, Boolean inline)
		{
			string[] pathParts;

			this.Logger.LogInformation("File with encoded path {0} requested.", encodedPath);

			pathParts = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPath)).Split('/');

			if (pathParts.Length != 2)
			{
				this.Logger.LogInformation("Encoded path {0} is not valid.", encodedPath);
				return BadRequest();
			}

			return Index(pathParts[0], pathParts[1], inline);
		}

	}
}
