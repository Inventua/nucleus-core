using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Modules.ContactUs.Models;

namespace Nucleus.Modules.ContactUs.Controllers;

[Extension("ContactUs")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class ContactUsAdminController : Controller
{
	private Context Context { get; }
	private IPageModuleManager PageModuleManager { get; }
	private IContentManager ContentManager { get; }
	private IListManager ListManager { get; }
	private IMailTemplateManager MailTemplateManager { get; }

	public ContactUsAdminController(Context Context, IPageModuleManager pageModuleManager, IContentManager contentManager, IListManager listManager, IMailTemplateManager mailTemplateManager)
	{
		this.Context = Context;
		this.PageModuleManager = pageModuleManager;
		this.ContentManager = contentManager;
		this.ListManager = listManager;
		this.MailTemplateManager = mailTemplateManager;
	}

	[HttpGet]
	[HttpPost]
	public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
	{
		return View("Settings", await BuildSettingsViewModel(viewModel));
	}

	[HttpPost]
	public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
	{
		if (viewModel.RecaptchaSecretKey != ViewModels.Settings.DUMMY_PASSWORD)
		{
			viewModel.SetSecretKey(this.Context.Site, viewModel.RecaptchaSecretKey);
		}

		viewModel.SetSettings(this.Context.Site, this.Context.Module);

		await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

		return Ok();
	}

	private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
	{
		if (viewModel == null)
		{
			viewModel = new();
		}

		viewModel.ReadSettings(this.Context.Module);

		viewModel.CategoryList = (await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(Models.Settings.MODULESETTING_CATEGORYLIST_ID, Guid.Empty)));

		viewModel.Lists = await this.ListManager.List(this.Context.Site);
		viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site);

		return viewModel;
	}
}

