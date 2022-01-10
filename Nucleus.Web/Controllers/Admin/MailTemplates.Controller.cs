using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class MailTemplatesController : Controller
	{
		private ILogger<MailTemplatesController> Logger { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private Context Context { get; }

		public MailTemplatesController(Context context, ILogger<MailTemplatesController> logger, IMailTemplateManager mailTemplateManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.MailTemplateManager = mailTemplateManager;
		}

		/// <summary>
		/// Display the editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.MailTemplateEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await MailTemplateManager.CreateNew() : await MailTemplateManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddMailTemplate()
		{
			return View("Editor", await BuildViewModel(new MailTemplate()));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.MailTemplateEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.MailTemplateManager.Save(this.Context.Site, viewModel.MailTemplate);
			
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteMailTemplate(ViewModels.Admin.MailTemplateEditor viewModel)
		{
			await this.MailTemplateManager.Delete(viewModel.MailTemplate);
			return View("Index", await BuildViewModel());
		}

		private async Task<ViewModels.Admin.MailTemplateIndex> BuildViewModel()
		{
			ViewModels.Admin.MailTemplateIndex viewModel = new();

			viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site);
			
			return viewModel;
		}

		private Task<ViewModels.Admin.MailTemplateEditor> BuildViewModel(MailTemplate mailTemplate)
		{
			ViewModels.Admin.MailTemplateEditor viewModel = new();

			viewModel.MailTemplate = mailTemplate;						

			return Task.FromResult(viewModel);
		}
	}
}
