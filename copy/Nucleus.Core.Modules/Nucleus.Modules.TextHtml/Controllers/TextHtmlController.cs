using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Core;

namespace Nucleus.Modules.TextHtml.Controllers
{
	[Extension("TextHtml")]
	public class TextHtmlController : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }

		private class ModuleSettingsKeys
		{
			public const string SETTINGS_CONTENT = "texthtml:content";
		}

		public TextHtmlController(Context Context, PageModuleManager pageModuleManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
		}

		[HttpGet]
		public ActionResult Index()
		{
			ViewResult result = View("Viewer", BuildViewModel());
			return result;
		}

		[HttpPost]
		public ActionResult Edit()
		{
			ViewResult result = View("Settings", BuildViewModel());
			return result;
		}

		private ViewModels.TextHtml BuildViewModel()
		{
			ViewModels.TextHtml viewModel = new ViewModels.TextHtml();

			if (this.Context.Module != null)
			{
				viewModel.ModuleId = this.Context.Module.Id;
				viewModel.Content = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_CONTENT, "");
			}

			return viewModel;
		}

		[HttpPost]
		public ActionResult Save(ViewModels.TextHtml viewModel)
		{
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_CONTENT, viewModel.Content);

			this.PageModuleManager.SaveSettings(this.Context.Module);
			
			return Ok();
		}
	}
}
