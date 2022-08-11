using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
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
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SearchSettings.ApiKey)}.{nameof(ViewModels.Admin.SearchSettings.ApiKey.Name)}");

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			if (viewModel.ApiKey.Id == Guid.Empty)
			{
				// create a new Api Key
				viewModel.ApiKey = await this.ApiKeyManager.CreateNew();
				viewModel.ApiKey.Name = $"Search Feeder";
				viewModel.ApiKey.Scope = "role:Registered Users";
				viewModel.ApiKey.Notes = "API Key used by the search feeder to request page content for indexing.";
			}

			await this.ApiKeyManager.Save(viewModel.ApiKey);

			this.Context.Site.SiteSettings.TrySetValue(Site.SiteSearchSettingsKeys.APIKEY_ID, viewModel.ApiKey.Id);
			this.Context.Site.SiteSettings.TrySetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_FILES_ONLY, viewModel.IndexPublicFilesOnly);
			this.Context.Site.SiteSettings.TrySetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_PAGES_ONLY, viewModel.IndexPublicPagesOnly);
			this.Context.Site.SiteSettings.TrySetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, viewModel.IndexPagesUseSsl);

			await this.SiteManager.Save(this.Context.Site);

			return Ok();
		}

		private async Task<ViewModels.Admin.SearchSettings> BuildViewModel()
		{
			ViewModels.Admin.SearchSettings viewModel = new();

			if (this.Context.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.APIKEY_ID, out Guid apiKeyId))
			{
				viewModel.ApiKey = await this.ApiKeyManager.Get(apiKeyId);
			}

			if (this.Context.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_FILES_ONLY, out Boolean indexPublicFilesOnly))
			{
				viewModel.IndexPublicFilesOnly = indexPublicFilesOnly;
			}

			if (this.Context.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_PAGES_ONLY, out Boolean indexPublicPagesOnly))
			{
				viewModel.IndexPublicPagesOnly = indexPublicPagesOnly;
			}

			if (this.Context.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean indexPagesUseSsl))
			{
				viewModel.IndexPagesUseSsl = indexPagesUseSsl;
			}

			

			viewModel.ApiKeys = await this.ApiKeyManager.List();
			
			return viewModel;
		}

	}
}
