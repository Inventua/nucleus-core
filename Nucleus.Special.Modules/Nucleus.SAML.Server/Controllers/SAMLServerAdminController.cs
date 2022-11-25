using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Nucleus.SAML.Server.Controllers
{
	[Extension("SAMLServer")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class SAMLServerAdminController : Controller
	{
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private ClientAppManager ClientAppManager { get; }
		private IApiKeyManager ApiKeyManager { get; }

		public SAMLServerAdminController(Context Context, IPageManager pageManager, IApiKeyManager apiKeyManager, ClientAppManager clientAppManager)
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

		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Settings viewModel)
		{
			return View("_ClientAppsList", await BuildSettingsViewModel(viewModel));
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
			
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
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
			//viewModel.ApiKeys = await this.ApiKeyManager.List();
			
			X509Store store = new(StoreName.My, StoreLocation.LocalMachine);
			store.Open(OpenFlags.ReadOnly);

			viewModel.SigningCertificates = new();
			viewModel.ValidationCertificates = new();
			foreach (X509Certificate2 cert in store.Certificates)
			{
				if (cert.HasPrivateKey)
				{
					viewModel.SigningCertificates.Add(cert.Thumbprint, cert.Subject.Replace("CN=",""));
				}
				viewModel.ValidationCertificates.Add(cert.Thumbprint, cert.Subject.Replace("CN=", ""));
			}

			return viewModel;
		}

	}
}