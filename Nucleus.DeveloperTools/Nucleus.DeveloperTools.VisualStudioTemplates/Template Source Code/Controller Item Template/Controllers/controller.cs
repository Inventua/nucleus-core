﻿using System;
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
using $defaultnamespace$.Models;

namespace $defaultnamespace$.Controllers
{
	[Extension("$defaultnamespace$")]
	public class $fileinputname$ViewerController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private $nucleus_extension_name$Manager $nucleus_extension_name$Manager { get; }

    public $fileinputname$ViewerController(Context Context, IPageModuleManager pageModuleManager, $nucleus_extension_name$Manager $nucleus_extension_name_camelcase$Manager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.$nucleus_extension_name$Manager = $nucleus_extension_name_camelcase$Manager;			
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