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

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class ApiKeysController : Controller
	{
		private ILogger<ApiKeysController> Logger { get; }
		private IApiKeyManager ApiKeyManager { get; }
		private Context Context { get; }

		public ApiKeysController(Context context, ILogger<ApiKeysController> logger, IApiKeyManager ApiKeyManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.ApiKeyManager = ApiKeyManager;
		}

		/// <summary>
		/// Display the ApiKeys list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel ());
		}

		/// <summary>
		/// Display the ApiKeys list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.ApiKeyIndex viewModel)
		{
			return View("_ApiKeysList", await BuildViewModel(viewModel));
		}

		/// <summary>
		/// Display the ApiKey editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.ApiKeyEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? await ApiKeyManager.CreateNew() : await ApiKeyManager.Get(id));
			
			if (viewModel.ApiKey.Id == Guid.Empty)
			{
				viewModel.IsNew = true;
				viewModel.ApiKey.Id = Guid.NewGuid();
			}

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddApiKey()
		{
			return View("Editor", BuildViewModel(new ApiKey()));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.ApiKeyEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.ApiKeyManager.Save(viewModel.ApiKey);
			
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteApiKey(ViewModels.Admin.ApiKeyEditor viewModel)
		{
			await this.ApiKeyManager.Delete(viewModel.ApiKey);
			return View("Index", await BuildViewModel());
		}

		private async Task<ViewModels.Admin.ApiKeyIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.ApiKeyIndex());
		}

		private async Task<ViewModels.Admin.ApiKeyIndex> BuildViewModel(ViewModels.Admin.ApiKeyIndex viewModel)
		{
			viewModel.ApiKeys = await this.ApiKeyManager.List(viewModel.ApiKeys);

			return viewModel;
		}

		private ViewModels.Admin.ApiKeyEditor BuildViewModel(ApiKey ApiKey)
		{
			ViewModels.Admin.ApiKeyEditor viewModel = new();

			viewModel.ApiKey = ApiKey;					

			return viewModel;
		}
	}
}
