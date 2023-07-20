using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Modules.MultiContent.Controllers
{
	[Extension("MultiContent")]
	public class MultiContentController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IContentManager ContentManager { get; }
    private FolderOptions FolderOptions { get; }

    private const string MODULESETTING_LAYOUT = "multicontent:layout";

		private const string MODULESETTING_OPENFIRST = "multicontent:openfirst";
		private const string MODULESETTING_RENDERFLUSH = "multicontent:renderflush";
		private const string MODULESETTING_SHOWCONTROLS = "multicontent:showcontrols";
		private const string MODULESETTING_SHOWINDICATORS = "multicontent:showindicators";

		private const string MODULESETTING_SHOWCLOSEBUTTON = "multicontent:showclosebutton";
		private const string MODULESETTING_ALERTSTYLE = "multicontent:alertstyle";
		private const string MODULESETTING_ALIGNMENT = "multicontent:alignment";
		private const string MODULESETTING_ORIENTATION = "multicontent:orientation";
		private const string MODULESETTING_FILL = "multicontent:fill";
		private const string MODULESETTING_JUSTIFY = "multicontent:justify";
		private const string MODULESETTING_ICON = "multicontent:icon";

		private const string MODULESETTING_INTERVAL = "multicontent:interval";

		public MultiContentController(IOptions<FolderOptions> folderOptions, Context context, IPageModuleManager pageModuleManager, IContentManager contentManager)
		{
      this.Context = context;
      this.FolderOptions = folderOptions.Value;
      this.PageModuleManager = pageModuleManager;
			this.ContentManager = contentManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Viewer", await BuildViewModel());
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		public async Task<ActionResult> Settings()
		{
			return View("Settings", await BuildSettingsViewModel(null));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_OPENFIRST, viewModel.LayoutSettings.OpenFirst);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_RENDERFLUSH, viewModel.LayoutSettings.RenderFlush);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOWCONTROLS, viewModel.LayoutSettings.ShowControls);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOWINDICATORS, viewModel.LayoutSettings.ShowIndicators);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOWCLOSEBUTTON, viewModel.LayoutSettings.ShowCloseButton);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ALERTSTYLE, viewModel.LayoutSettings.AlertStyle);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ALIGNMENT, viewModel.LayoutSettings.Alignment);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ORIENTATION, viewModel.LayoutSettings.Orientation);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_FILL, viewModel.LayoutSettings.Fill);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_JUSTIFY, viewModel.LayoutSettings.Justify);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ICON, viewModel.LayoutSettings.Icon);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_INTERVAL, viewModel.LayoutSettings.Interval);
			
			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Json(new { Title = "Changes Saved", Message = "Your changes have been saved." });
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Create(ViewModels.Settings viewModel)
		{
			return View("Editor", await BuildEditorViewModel(new(), Guid.Empty));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Edit(ViewModels.Editor viewModel, Guid id)
		{
			return View("Editor", await BuildEditorViewModel(viewModel, id));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveContent(ViewModels.Editor viewModel)
		{
			await this.ContentManager.Save(this.Context.Module, viewModel.Content);

			return View("_ContentList", await BuildSettingsViewModel(new ViewModels.Settings()));
		}

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> UpdateTitle(Guid id, string value)
    {
      Content content = await this.ContentManager.Get(id);
      content.Title = value;
      await this.ContentManager.Save(this.Context.Module, content);

      return Ok();
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> UpdateContent(Guid id, string value)
    {
      Content content = await this.ContentManager.Get(id);
      content.Value = value;
      await this.ContentManager.Save(this.Context.Module, content);

      return Ok();
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Delete(ViewModels.Settings viewModel, Guid id)
		{
			Content content = await this.ContentManager.Get(id);
			await this.ContentManager.Delete(content);

			return View("_ContentList", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
		{
			await this.ContentManager.MoveUp(this.Context.Module, id);
			return View("_ContentList", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> MoveDown(ViewModels.Settings viewModel, Guid id)
		{
			await this.ContentManager.MoveDown(this.Context.Module, id);
			return View("_ContentList", await BuildSettingsViewModel(viewModel));
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			viewModel.Module = this.Context.Module;
			viewModel.Contents = await this.ContentManager.List(this.Context.Module);
			
			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Carousel")}.cshtml";

			viewModel.LayoutSettings = GetLayoutSettings();
			return viewModel;
		}

		private ViewModels.LayoutSettings GetLayoutSettings()
		{
			ViewModels.LayoutSettings settings = new();

			settings.OpenFirst = this.Context.Module.ModuleSettings.Get(MODULESETTING_OPENFIRST, true);
			settings.RenderFlush = this.Context.Module.ModuleSettings.Get(MODULESETTING_RENDERFLUSH, false);
			settings.ShowControls = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOWCONTROLS, true);
			settings.ShowIndicators = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOWINDICATORS, true);

			settings.ShowCloseButton = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOWCLOSEBUTTON, false);
			settings.AlertStyle = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALERTSTYLE, "");
			settings.Orientation = this.Context.Module.ModuleSettings.Get(MODULESETTING_ORIENTATION, ViewModels.LayoutSettings.OrientationStyles.Horizontal);
			settings.Alignment = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALIGNMENT, ViewModels.LayoutSettings.AlignmentStyles.Left);
			settings.Fill = this.Context.Module.ModuleSettings.Get(MODULESETTING_FILL, false);
			settings.Justify = this.Context.Module.ModuleSettings.Get(MODULESETTING_JUSTIFY, false);
			settings.Icon = this.Context.Module.ModuleSettings.Get(MODULESETTING_ICON, ViewModels.LayoutSettings.Icons.Default);

			settings.Interval = this.Context.Module.ModuleSettings.Get(MODULESETTING_INTERVAL, settings.Interval);
			
			return settings;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
				viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Carousel");

				viewModel.LayoutSettings = GetLayoutSettings();
			}

			viewModel.Contents = await this.ContentManager.List(this.Context.Module);

			System.IO.DirectoryInfo thisFolder = new System.IO.DirectoryInfo(this.GetType().Assembly.Location);

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.FolderOptions.GetExtensionFolder("MultiContent", false)}/Views/ViewerLayouts/", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}

			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid contentId)
		{
			if (input.Content == null)
			{
				if (contentId == Guid.Empty)
				{
					input.Content = new();
				}
				else
				{
					input.Content = await this.ContentManager.Get(contentId);
				}
			}

			return input;
		}

	}
}