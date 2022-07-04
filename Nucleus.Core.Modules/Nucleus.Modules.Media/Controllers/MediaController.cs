using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Extensions.Authorization;
using Microsoft.AspNetCore.Http;
using Nucleus.Extensions;

namespace Nucleus.Modules.Media.Controllers
{
	[Extension("Media")]
	public class MediaController : Controller
	{
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		private IPageModuleManager PageModuleManager { get; }
		private ILogger<MediaController> Logger { get; }

		private class ModuleSettingsKeys
		{
			public const string MEDIA_FILE_ID = "media:file:id";

			public const string MEDIA_CAPTION = "media:caption";
			public const string MEDIA_ALTERNATETEXT = "media:alternatetext";
			public const string MEDIA_SHOWCAPTION = "media:showcaption";

			public const string MEDIA_HEIGHT = "media:height";
			public const string MEDIA_WIDTH = "media:width";
			public const string MEDIA_ALWAYSDOWNLOAD = "media:alwaysdownload";
		}

		public MediaController(Context Context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, ILogger<MediaController> logger)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.Logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			ViewModels.Viewer viewModel = await BuildViewModel();

			if (viewModel.PermissionDenied)
			{
				this.Logger.LogDebug("Access denied to media file.");
				return Forbid();
			}
			else
			{
				return View("Viewer", viewModel);
			}

		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Edit(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildEditorViewModel());
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Select(ViewModels.Settings viewModel)
		{
			return View("Settings", viewModel);
		}

		[HttpPost]
		public ActionResult SelectAnother(ViewModels.Settings viewModel)
		{
			viewModel.SelectedFile.ClearSelection();

			return View("Settings", viewModel);
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Settings viewModel)
		{
			if (viewModel.SelectedFile.Path != null)
			{
				this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_FILE_ID, viewModel.SelectedFile.Id);
			}
			else
			{
				this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_FILE_ID, Guid.Empty);
			}

			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_CAPTION, viewModel.Caption);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_ALTERNATETEXT, viewModel.AlternateText);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_SHOWCAPTION, viewModel.ShowCaption);

			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_HEIGHT, viewModel.Height);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_WIDTH, viewModel.Width);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_ALWAYSDOWNLOAD, viewModel.AlwaysDownload);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> UploadFile(ViewModels.Settings viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.SelectedFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFile.Parent.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					viewModel.SelectedFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Parent.Path, mediaFile.FileName, fileStream, false) as File;
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Settings", viewModel);
		}

		private static ViewModels.Viewer.MediaTypes GetMediaType(File file, Boolean alwaysDownload)
		{
			if (file == null) return ViewModels.Viewer.MediaTypes.None;

			if (alwaysDownload) return ViewModels.Viewer.MediaTypes.Generic;

			switch (System.IO.Path.GetExtension(file.Name).ToLower())
			{
				case ".mpeg":
				case ".mpg":
				case ".webm":
				case ".ogg":
				case ".mp4":
				case ".avi":
				case ".mov":
					return ViewModels.Viewer.MediaTypes.Video;

				case ".pdf":
					return ViewModels.Viewer.MediaTypes.PDF;

				case ".jpg":
				case ".jpeg":
				case ".gif":
				case ".png":
				case ".bmp":
				case ".tif":
				case ".tiff":
					return ViewModels.Viewer.MediaTypes.Image;

				default:
					return ViewModels.Viewer.MediaTypes.Generic;
			}
		}

		/// <summary>
		/// Initialize the view model for the View action
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			if (this.Context.Module != null)
			{
				Guid selectedFileId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_FILE_ID, Guid.Empty);

				if (selectedFileId != Guid.Empty)
				{
					try
					{
						viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, selectedFileId);
					}
					catch (System.IO.FileNotFoundException)
					{
						// if the selected file has been deleted, set the selected file to null and allow the user to select another file
						viewModel.SelectedFile = null;
					}
				}

				if (viewModel.SelectedFile != null)
				{
					//Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFile.Parent.Id);
					if (viewModel.SelectedFile.Parent != null)
					{
						viewModel.SelectedFile.Parent.Permissions = await this.FileSystemManager.ListPermissions(viewModel.SelectedFile.Parent);
						if (HttpContext.User.HasViewPermission(this.Context.Site, viewModel.SelectedFile.Parent))
						{
							viewModel.Caption = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_CAPTION, "");
							viewModel.AlternateText = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALTERNATETEXT, "");
							viewModel.ShowCaption = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_SHOWCAPTION, false);

							viewModel.Height = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_HEIGHT, "");
							viewModel.Width = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_WIDTH, "");
							viewModel.AlwaysDownload = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALWAYSDOWNLOAD, false);

							viewModel.SelectedItemType = GetMediaType(viewModel.SelectedFile, viewModel.AlwaysDownload);

							viewModel.Style =
								(String.IsNullOrEmpty(viewModel.Height) ? "" : $"height:{viewModel.Height};") +
								(String.IsNullOrEmpty(viewModel.Width) ? "width: 100%;" : $"width:{viewModel.Width};");
						}
						else
						{
							// User does not have view permission for the selected file(folder)
							viewModel.SelectedFile = null;
							viewModel.PermissionDenied = true;
						}
					}
					else
					{
						// folder is missing from the database (assume no permissions)
						viewModel.SelectedFile = null;
						viewModel.PermissionDenied = true;
					}
				}
				else
				{
					//  no file is selected/file has been deleted
					viewModel.SelectedFile = null;
					viewModel.PermissionDenied = false;
				}


			}

			return viewModel;
		}

		/// <summary>
		/// Initialize the view model from saved module settings
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.Settings> BuildEditorViewModel()
		{
			ViewModels.Settings viewModel = new();
			if (this.Context.Module != null)
			{
				Guid selectedFileId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_FILE_ID, Guid.Empty);

				if (selectedFileId != Guid.Empty)
				{
					try
					{
						viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, selectedFileId);
					}
					catch (System.IO.FileNotFoundException)
					{
						// if the selected file has been deleted, set the selected file to null and allow the user to select another file
						viewModel.SelectedFile = null;
					}
				}

				viewModel.Caption = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_CAPTION, "");
				viewModel.AlternateText = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALTERNATETEXT, "");
				viewModel.ShowCaption = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_SHOWCAPTION, false);

				viewModel.Height = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_HEIGHT, "");
				viewModel.Width = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_WIDTH, "");
				viewModel.AlwaysDownload = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALWAYSDOWNLOAD, false);
			}

			return viewModel;
		}



	}
}