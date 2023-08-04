using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using $nucleus.extension.namespace$.Models;

namespace $nucleus.extension.namespace$.Controllers
{
	[Extension("$nucleus.extension.name$")]
	public class $nucleus.extension.name$ViewerController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		
		public $nucleus.extension.name$ViewerController(Context Context, IPageModuleManager pageModuleManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;		
		}

		[HttpGet]
		public ActionResult Index()
		{
			return View("Viewer", BuildViewModel());
		}

		private ViewModels.Viewer BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			viewModel.GetSettings(this.Context.Module);
			return viewModel;
		}
	}
}