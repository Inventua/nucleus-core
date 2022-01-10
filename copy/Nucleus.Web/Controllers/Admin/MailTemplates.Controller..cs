using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class MailTemplatesController : Controller
	{
		private ILogger<MailTemplatesController> Logger { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private Context Context { get; }

		public MailTemplatesController(Context context, ILogger<MailTemplatesController> logger, MailTemplateManager mailTemplateManager)
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
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.MailTemplateEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? MailTemplateManager.CreateNew() : MailTemplateManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddMailTemplate()
		{
			return View("Editor", BuildViewModel(new MailTemplate()));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.MailTemplateEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.MailTemplateManager.Save(this.Context.Site, viewModel.MailTemplate);
			
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteMailTemplate(ViewModels.Admin.MailTemplateEditor viewModel)
		{
			this.MailTemplateManager.Delete(viewModel.MailTemplate);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.MailTemplateIndex BuildViewModel()
		{
			ViewModels.Admin.MailTemplateIndex viewModel = new();

			viewModel.MailTemplates = this.MailTemplateManager.List(this.Context.Site);
			
			return viewModel;
		}

		private ViewModels.Admin.MailTemplateEditor BuildViewModel(MailTemplate mailTemplate)
		{
			ViewModels.Admin.MailTemplateEditor viewModel = new();

			viewModel.MailTemplate = mailTemplate;						

			return viewModel;
		}
	}
}
