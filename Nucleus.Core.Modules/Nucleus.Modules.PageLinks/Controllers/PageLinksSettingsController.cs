using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Modules.PageLinks.Controllers;

[Extension("PageLinks")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class PageLinksSettingsController : Controller
{
	private Context Context { get; }
	private IPageModuleManager PageModuleManager { get; }

	public PageLinksSettingsController(Context Context, IPageModuleManager pageModuleManager)
	{
		this.Context = Context;
		this.PageModuleManager = pageModuleManager;
	}

	[HttpGet]
	[HttpPost]
	public ActionResult Settings(ViewModels.Settings viewModel)
	{
		return View("Settings", BuildSettingsViewModel(viewModel));
	}

	[HttpPost]
	public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
	{
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH1, Models.Settings.HtmlHeaderTags.H1);
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH2, Models.Settings.HtmlHeaderTags.H2);
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH3, Models.Settings.HtmlHeaderTags.H3);
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH4, Models.Settings.HtmlHeaderTags.H4);
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH5, Models.Settings.HtmlHeaderTags.H5);
		viewModel.IncludeHeaders = SetIncludeHeader(viewModel, viewModel.HeaderH6, Models.Settings.HtmlHeaderTags.H6);

		viewModel.SetSettings(this.Context.Module);

		await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

		return View("Settings", BuildSettingsViewModel(viewModel));
		//return Ok();
	}

	private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
	{
		if (viewModel == null)
		{
			viewModel = new();
		}

		viewModel.GetSettings(this.Context.Module);

		BuildHeaderToggleViewModel(viewModel);

		return viewModel;
	}

	private void BuildHeaderToggleViewModel(ViewModels.Settings viewModel)
	{
		viewModel.HeaderH1 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H1);
		viewModel.HeaderH2 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H2);
		viewModel.HeaderH3 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H3);
		viewModel.HeaderH4 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H4);
		viewModel.HeaderH5 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H5);
		viewModel.HeaderH6 = viewModel.IncludeHeaders.HasFlag(Models.Settings.HtmlHeaderTags.H6);
	}

	private Models.Settings.HtmlHeaderTags SetIncludeHeader(ViewModels.Settings viewModel, Boolean headerSetting, Models.Settings.HtmlHeaderTags headerTag)
	{
		if (headerSetting)
		{
			return viewModel.IncludeHeaders |= headerTag;
		}
		else
		{
			return viewModel.IncludeHeaders &= ~headerTag;
		}
	}
}