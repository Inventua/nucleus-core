using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Hosting;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class SettingsController : Controller
	{
		private IExtensionManager ExtensionManager { get; }
		private IWebHostEnvironment WebHostEnvironment {get;}

		public SettingsController(IWebHostEnvironment webHostEnvironment, IExtensionManager extensionManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.ExtensionManager = extensionManager;
		}

		/// <summary>
		/// Display the "settings" admin page
		/// </summary>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			ViewModels.Admin.Settings viewModel = new()
			{
				IsDevelopment = this.WebHostEnvironment.EnvironmentName == Microsoft.Extensions.Hosting.Environments.Development,
				Extensions = await this.ExtensionManager.ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes.Global)
			};

			return View("Index", viewModel);
		}
	}
}
