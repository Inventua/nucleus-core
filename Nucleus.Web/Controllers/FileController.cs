using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers
{
	/// <summary>
	/// Display a page and module content, using the selected layout
	/// </summary>
	public class FileController : Controller
	{
		private ILogger<FileController> Logger { get; }
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		public FileController(ILogger<FileController> logger, Context context, IFileSystemManager fileSystemManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		[Route("/files/{providerKey}/{path}")]
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
		[Route("/files/{encodedpath}")]
		public async Task<ActionResult> Index(string encodedPath, Boolean inline)
		{
			string idString;

			this.Logger.LogInformation("File with encoded path {encodedPath} requested.", encodedPath);

			idString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPath));

			if (!Guid.TryParse(idString, out Guid id))
			{
				this.Logger.LogInformation("Encoded path {encodedPath} is not valid.", encodedPath);
				return BadRequest();
			}

			try
			{
				File file = await this.FileSystemManager.GetFile(this.Context.Site, id);

				if (file != null)
				{
					return await Index(file, inline);
				}
				else
				{
					this.Logger.LogInformation("File {id} not found.", id);
					return NotFound();
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				this.Logger.LogInformation("File {id} not found.", id);
				return NotFound();
			}
		}

		private async Task<ActionResult> Index(File file, Boolean inline)
		{
			try
			{
				if (file != null)
				{
					DateTime lastModifiedDate = file.DateModified;

					this.Logger.LogInformation("File {provider}/{path} found.", file.Provider, file.Path);

					Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, file.Parent.Id);
					if (folder != null)
					{
						folder.Permissions = await this.FileSystemManager.ListPermissions(folder);
					}

					if (folder != null && HttpContext.User.HasViewPermission(this.Context.Site, folder))
					{
						DateTimeOffset? ifModifiedSince = ControllerContext.HttpContext.Request.GetTypedHeaders().IfModifiedSince;
						if (ifModifiedSince.HasValue && ifModifiedSince.Value >= lastModifiedDate)
						{
							return StatusCode((int)System.Net.HttpStatusCode.NotModified);
						}

						// For file system providers that support it, we redirect to the "direct" url for files, so as
						// to avoid additional data traffic charges in cloud-hosted environments.  
						// By rendering a Nucleus file link (to this controller) and then returning a redirect, we still check
						// for Nucleus permissions, but are using the direct link to serve the file.
						if (file.Capabilities.CanDirectLink)
						{
							System.Uri uri = this.FileSystemManager.GetFileDirectUrl(this.Context.Site, file);
							if (uri != null)
							{
								return Redirect(uri.AbsoluteUri);
							}
						}

						System.Net.Http.Headers.ContentDispositionHeaderValue contentDisposition = new(inline ? "inline" : "attachment")
						{
							FileName = file.Name,
						};

						Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();
						
						Response.Headers.Add(Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition, contentDisposition.ToString());
						Response.GetTypedHeaders().LastModified = lastModifiedDate;

						if (!extensionProvider.TryGetContentType(file.Path, out string mimeType))
						{
							mimeType = "application/octet-stream";
						}

						return File(this.FileSystemManager.GetFileContents(this.Context.Site, file), mimeType);
					}
					else
					{
						this.Logger.LogInformation("File {provider}/{path} permission denied for {username}.", file.Provider, file.Path, HttpContext.User.Identity.Name);
						return Forbid();
					}
				}
				else
				{
					this.Logger.LogInformation("File {provider}/{path} not found.", file.Provider, file.Path);
					return NotFound();
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				this.Logger.LogInformation("File {provider}/{path} not found.", file.Provider, file.Path);
				return NotFound();
			}
		}
	}
}
