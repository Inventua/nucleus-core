using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Modules.ContactUs.Controllers
{
	[Extension("ContactUs")]
	public class ContactUsController : Controller
	{
		private Context Context { get; }

		private ILogger<ContactUsController> Logger { get; }

		private IPageModuleManager PageModuleManager { get; }

		private IListManager ListManager { get; }

		private IMailTemplateManager MailTemplateManager { get; }

		private IMailClientFactory MailClientFactory { get; }

		public ContactUsController(Context context, ILogger<ContactUsController> logger, IListManager listManager, IPageModuleManager pageModuleManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory)
		{
			this.Context = context;
			this.Logger = logger;
			this.ListManager = listManager;
			this.PageModuleManager = pageModuleManager;
			this.MailTemplateManager = mailTemplateManager;
			this.MailClientFactory = mailClientFactory;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Viewer", await BuildViewModel());
		}

		private void Validate(ViewModels.Viewer viewModel, Models.Settings settings)
		{
			if (settings.RequireName && string.IsNullOrEmpty(viewModel.Message.FirstName))
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.FirstName), "Please enter your first name");	
			}

			if (settings.RequireName && string.IsNullOrEmpty(viewModel.Message.LastName))
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.LastName), "Please enter your last name");
			}

			if (settings.RequirePhoneNumber && string.IsNullOrEmpty(viewModel.Message.PhoneNumber))
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.PhoneNumber), "Please enter your phone number");
			}

			if (settings.RequireCompany && string.IsNullOrEmpty(viewModel.Message.Company))
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.Company), "Please enter your company name");
			}

			if (settings.RequireCategory && viewModel.Message.Category.Id == Guid.Empty)
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.Category), "Please select a category related to your enquiry.");
			}

			if (settings.RequireSubject && string.IsNullOrEmpty(viewModel.Message.Subject))
			{
				ControllerContext.ModelState.AddModelError(nameof(viewModel.Message.Subject), "Please enter the subject of your enquiry");
			}

		}


		[HttpPost]
		public async Task<ActionResult> Send(ViewModels.Viewer viewModel)
		{
			Models.Settings settings = new();
			settings.ReadSettings(this.Context.Module);

			Validate(viewModel, settings);

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}


			// send welcome email (if set and the new user has an email address)

			if (this.Context != null && this.Context.Site != null)
			{
				
				if (!String.IsNullOrEmpty(settings.SendTo))
				{
					if (settings.MailTemplateId != Guid.Empty)
					{
						MailTemplate template = await this.MailTemplateManager.Get(settings.MailTemplateId);
						if (template != null)
						{
							MailArgs args = new()
							{
								{ "Site", this.Context.Site },
								{ "Message", viewModel.Message }
							};

							Logger.LogTrace("Sending contact email {emailTemplateName} to '{sendTo}'.", template.Name, settings.SendTo);

							using (IMailClient mailClient = this.MailClientFactory.Create())
							{
								mailClient.Send(template, args, settings.SendTo);
							}
						}
					}
					else
					{
						Logger.LogTrace("Not sending contact email to '{sendTo}' because no template has been set.", settings.SendTo);
					}
				}
				else
				{
					Logger.LogTrace("Not sending contact email to '{sendTo}' because the module does not have a send to value set.", settings.SendTo);
				}

			}

			return Ok();
			//return View("Viewer", await BuildViewModel());
		}


		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_MAILTEMPLATE_ID, viewModel.MailTemplateId);

			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWNAME, viewModel.ShowName);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWCOMPANY, viewModel.ShowCompany);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWPHONENUMBER, viewModel.ShowPhoneNumber);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWCATEGORY, viewModel.ShowCategory);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWSUBJECT, viewModel.ShowSubject);

			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRENAME, viewModel.RequireName);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRECOMPANY, viewModel.RequireCompany);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIREPHONENUMBER, viewModel.RequirePhoneNumber);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRECATEGORY, viewModel.RequireCategory);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRESUBJECT, viewModel.RequireSubject);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			viewModel.ReadSettings(this.Context.Module);
			viewModel.CategoryList = (await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(Models.Settings.MODULESETTING_CATEGORYLIST_ID, Guid.Empty)));
		
			return viewModel;
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
}