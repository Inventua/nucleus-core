using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class SearchSettingsController : Controller
	{
		private ILogger<ApiKeysController> Logger { get; }
		private IApiKeyManager ApiKeyManager { get; }
		private ISiteManager SiteManager { get; }

		private Context Context { get; }

		private const string SETTING_APIKEY_ID = "searchsettings:apikey:id";

		public SearchSettingsController(Context context, ILogger<ApiKeysController> logger, ISiteManager siteManager, IApiKeyManager ApiKeyManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.ApiKeyManager = ApiKeyManager;
			this.SiteManager = siteManager;
		}

		/// <summary>
		/// Display the search settings editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Settings()
		{
			return View("Settings", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.SearchSettings viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.Context.Site.SiteSettings.TrySetValue(SETTING_APIKEY_ID, viewModel.ApiKey.Id);

			await this.SiteManager.Save(this.Context.Site);

			return Ok();
		}

		private async Task<ViewModels.Admin.SearchSettings> BuildViewModel()
		{
			ViewModels.Admin.SearchSettings viewModel = new();

			if (this.Context.Site.SiteSettings.TryGetValue(SETTING_APIKEY_ID, out Guid result))
			{
				viewModel.ApiKey = await this.ApiKeyManager.Get(result);
			}

			viewModel.ApiKeys = await this.ApiKeyManager.List();

			return viewModel;
		}

	}
}
