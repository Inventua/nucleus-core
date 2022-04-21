using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.Controllers
{
	[Extension("OAuthServer")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class OAuthServerAdminController : Controller
	{
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private ClientAppManager ClientAppManager { get; }
		private IApiKeyManager ApiKeyManager { get; }

		public OAuthServerAdminController(Context Context, IPageManager pageManager, IApiKeyManager apiKeyManager, ClientAppManager clientAppManager)
		{
			this.Context = Context;
			this.PageManager = pageManager;
			this.ApiKeyManager = apiKeyManager;
			this.ClientAppManager = clientAppManager;
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Editor(Guid id)
		{
			return View("Editor", await BuildSettingsViewModel(id == Guid.Empty ? await this.ClientAppManager.CreateNew() : await this.ClientAppManager.Get(id)));
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.PageIndexPartial viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1
				);

			return View("_PageMenu", viewModel);
		}


		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			ModelState.Remove($"{nameof(viewModel.ClientApp)}.{nameof(viewModel.ClientApp.ApiKey)}.{nameof(viewModel.ClientApp.ApiKey.Id)}");

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (viewModel.ClientApp.ApiKey.Id == Guid.Empty)
			{
				// create a new Api Key
				viewModel.ClientApp.ApiKey = await this.ApiKeyManager.CreateNew();
				viewModel.ClientApp.ApiKey.Name = $"API Key for OAuth2 Client App {viewModel.ClientApp.Title}";
				viewModel.ClientApp.ApiKey.Scope = "OAuth2";
				viewModel.ClientApp.ApiKey.Notes = "This API Key was automatically created by the OAuth Server Control Panel.";

				await this.ApiKeyManager.Save(viewModel.ClientApp.ApiKey);
			}

			await this.ClientAppManager.Save(this.Context.Site, viewModel.ClientApp);

			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ClientApps = await this.ClientAppManager.List(this.Context.Site, viewModel.ClientApps);

			return viewModel;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ClientApp clientApp)
		{
			ViewModels.Settings viewModel = new();

			viewModel.ClientApp = clientApp;
			viewModel.Pages = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, 1);
			viewModel.ApiKeys = await this.ApiKeyManager.List();
			return viewModel;
		}

	}
}