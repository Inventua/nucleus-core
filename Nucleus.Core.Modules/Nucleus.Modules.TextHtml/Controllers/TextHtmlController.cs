using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Extensions;

namespace Nucleus.Modules.TextHtml.Controllers
{
	[Extension("TextHtml")]
	public class TextHtmlController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IContentManager ContentManager { get; }
		
		public TextHtmlController(Context Context, IPageModuleManager pageModuleManager, IContentManager contentManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.ContentManager = contentManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			ViewResult result = View("Viewer", await BuildViewModel());
			return result;
		}

		[HttpPost]
		public async Task<ActionResult> Edit()
		{
			ViewResult result = View("Settings", await BuildSettingsViewModel());
			return result;
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new ViewModels.Viewer();

			if (this.Context.Module != null)
			{
				List<Content> contents = await this.ContentManager.List(this.Context.Module);

				// Text/Html only ever has one item
				if (contents.Count > 0)
				{
					viewModel.Content = contents.First().ToHtml();					
				}
			}

			return viewModel;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel()
		{
			ViewModels.Settings viewModel = new ViewModels.Settings();

			if (this.Context.Module != null)
			{
				viewModel.ModuleId = this.Context.Module.Id;
				List<Content> contents = await this.ContentManager.List(this.Context.Module);

				// Text/Html only ever has one item
				viewModel.Content = contents.FirstOrDefault();				
			}

			return viewModel;
		}


		[HttpPost]
		public ActionResult Save(ViewModels.Settings viewModel)
		{
			// Text/Html only ever has one item
			viewModel.Content.SortOrder = 10;
			
			this.ContentManager.Save(this.Context.Module, viewModel.Content);
			
			return Ok();
		}
	}
}
