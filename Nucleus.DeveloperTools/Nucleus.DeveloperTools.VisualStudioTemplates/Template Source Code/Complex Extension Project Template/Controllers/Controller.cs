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
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$.Controllers
{
	[Extension("$nucleus_extension_name$")]
	public class $nucleus_extension_name$Controller : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private $nucleus_extension_name$Manager $nucleus_extension_name$Manager { get; }
		
		public $nucleus_extension_name$Controller(Context Context, IPageModuleManager pageModuleManager, $nucleus_extension_name$Manager $nucleus_extension_name_lcase$Manager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.$nucleus_extension_name$Manager = $nucleus_extension_name_lcase$Manager;			
		}

		[HttpGet]
		public ActionResult Index()
		{
			return View("Viewer", BuildViewModel());
		}

		private ViewModels.Viewer BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			viewModel.ReadSettings(this.Context.Module);
			return viewModel;
		}
	}
}