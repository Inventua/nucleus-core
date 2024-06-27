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
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Media.Controllers
{
	[Extension("Media")]
	public class MediaController : Controller
	{
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		private IPageModuleManager PageModuleManager { get; }
		private ILogger<MediaController> Logger { get; }
        
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
			return View("Settings", await BuildEditorViewModel(viewModel));
		}


		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Select(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildEditorViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnother(ViewModels.Settings viewModel)
		{
			viewModel.SelectedFile.ClearSelection();

			return View("Settings", await BuildEditorViewModel(viewModel));
		}

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> Save(ViewModels.Settings viewModel)
    {
      viewModel.SetSettings(this.Context.Module);
			await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

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

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> UpdateCaption(string value)
    {
      ViewModels.Settings settings = new();
      
      settings.GetSettings(this.Context.Module);      
      settings.Caption = value;
      settings.SetSettings(this.Context.Module);

      await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

      return Ok();
    }

    private static ViewModels.Viewer.MediaTypes GetMediaType(ViewModels.Viewer viewModel, Boolean alwaysDownload)
		{
      if (alwaysDownload)
      {
        return ViewModels.Viewer.MediaTypes.Generic;
      }

      if (String.IsNullOrEmpty(viewModel.SourceUrl))
      {
        return ViewModels.Viewer.MediaTypes.None;
      }

      if (viewModel.SourceType == Models.Settings.AvailableSourceTypes.YouTube)
      {
        return ViewModels.Viewer.MediaTypes.YouTube;
      }

      string sourceFileName = "";
      switch (viewModel.SourceType)
      {
        case Models.Settings.AvailableSourceTypes.File:
          sourceFileName = viewModel.SelectedFile.Name;
          break;
        case Models.Settings.AvailableSourceTypes.Url:
          int position = viewModel.Url.IndexOf('?');
          if (position > 1)
          {
            sourceFileName = viewModel.Url.Substring(0, position);
          }
          else
          {
            sourceFileName = viewModel.Url;
          }
          break;
      }

			switch (System.IO.Path.GetExtension(sourceFileName).ToLower().Trim())
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
        case ".webp":
        case ".tif":
				case ".tiff":
					return ViewModels.Viewer.MediaTypes.Image;

				default:
          if (viewModel.SourceType == Models.Settings.AvailableSourceTypes.File)
          {
            // assume "generic" for files which don't match a known extension
            return ViewModels.Viewer.MediaTypes.Generic;
          }
          else
          {
            // assume "video" for Urls which don't match a known extension or have no file extension
            return ViewModels.Viewer.MediaTypes.Video;
          }
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
        viewModel.GetSettings(this.Context.Module);

        if (viewModel.SourceType == Models.Settings.AvailableSourceTypes.File)
        {
          if (viewModel.SelectedFileId != Guid.Empty)
          {
            try
            {
              viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFileId);
            }
            catch (System.IO.FileNotFoundException)
            {
              // if the selected file has been deleted, set the selected file to null and allow the user to select another file
              viewModel.SelectedFile = null;
            }
          }

          if (viewModel.SelectedFile != null)
          {
            if (viewModel.SelectedFile.Parent == null)
            {
              // folder is missing from the database (assume no permissions)
              viewModel.SelectedFile = null;
              viewModel.PermissionDenied = true;
            }
            else
            // check view permissions on the file folder
            if (!HttpContext.User.HasViewPermission(this.Context.Site, viewModel.SelectedFile.Parent))
            {
              viewModel.SelectedFile = null;
              viewModel.PermissionDenied = true;
            }
          }
        }

        switch (viewModel.SourceType)
        {
          case Models.Settings.AvailableSourceTypes.File:
            viewModel.SourceUrl = Url.FileLink(viewModel.SelectedFile, true);
            break;
          case Models.Settings.AvailableSourceTypes.Url:
            viewModel.SourceUrl = viewModel.Url;
            break;
          case Models.Settings.AvailableSourceTypes.YouTube:
            viewModel.SourceUrl = $"https://www.youtube.com/embed/{viewModel.YoutubeId}?mute=1&autoplay={(viewModel.AutoPlay ? "1" : "0")}";
            break;
        }
         
				viewModel.SelectedItemType = GetMediaType(viewModel, viewModel.AlwaysDownload);

				viewModel.Style =
					(String.IsNullOrEmpty(viewModel.Height) ? "" : $"height:{viewModel.Height};") +
					(String.IsNullOrEmpty(viewModel.Width) ? "width: 100%;" : $"width:{viewModel.Width};") + 
          (String.IsNullOrEmpty(viewModel.Height) || String.IsNullOrEmpty(viewModel.Width) ? "aspect-ratio: 16 / 9;" : "");
			}

			return viewModel;
		}

		/// <summary>
		/// Initialize the view model from saved module settings
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.Settings> BuildEditorViewModel(ViewModels.Settings viewModel)
		{
      if (String.IsNullOrEmpty(viewModel.SourceType))
      {
        viewModel = new();
        viewModel.GetSettings(this.Context.Module);
      }

      viewModel.SourceTypes = new();
      viewModel.SourceTypes.Add(Models.Settings.AvailableSourceTypes.File, "File");
      viewModel.SourceTypes.Add(Models.Settings.AvailableSourceTypes.Url, "Url");
      viewModel.SourceTypes.Add(Models.Settings.AvailableSourceTypes.YouTube, "YouTube");

      if (viewModel.SourceType == Models.Settings.AvailableSourceTypes.File)
      {
        if (viewModel.SelectedFileId != Guid.Empty)
        {
          try
          {
            viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFileId);
          }
          catch (System.IO.FileNotFoundException)
          {
            // if the selected file has been deleted, set the selected file to null and allow the user to select another file
            viewModel.SelectedFile = null;
          }
        }
      }

      return viewModel;
		}
	}
}