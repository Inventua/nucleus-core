using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Mail;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

    private void Validate(Boolean isRequired, string value, string propertyName, string message)
    {
      if (isRequired && string.IsNullOrEmpty(value))
      {
        ModelState.AddModelError(propertyName, message);
      }
    }

    private void Validate(Boolean isRequired, ListItem value, string propertyName, string message)
    {
      if (isRequired && (value == null || value.Id == Guid.Empty))
      {
        ModelState.AddModelError(propertyName + ".Id", message);
      }
    }

    private string MessagePropertyName(string propertyName)
    {
      return $"{nameof(ViewModels.Viewer.Message)}.{propertyName}";
    }

    private void Validate(ViewModels.Viewer viewModel, Models.Settings settings)
    {
      Validate(settings.RequireName, viewModel.Message.FirstName, MessagePropertyName(nameof(viewModel.Message.FirstName)), "Please enter your first name.");
      Validate(settings.RequireName, viewModel.Message.LastName, MessagePropertyName(nameof(viewModel.Message.LastName)), "Please enter your last name.");
      Validate(settings.RequirePhoneNumber, viewModel.Message.PhoneNumber, MessagePropertyName(nameof(viewModel.Message.PhoneNumber)), "Please enter your phone number.");
      Validate(settings.RequireCompany, viewModel.Message.Company, MessagePropertyName(nameof(viewModel.Message.Company)), "Please enter your company name.");

      Validate(settings.RequireCategory, viewModel.Message.Category, MessagePropertyName(nameof(viewModel.Message.Category)), "Please select a category related to your enquiry.");
      Validate(settings.RequireSubject, viewModel.Message.Subject, MessagePropertyName(nameof(viewModel.Message.Subject)), "Please enter the subject of your enquiry.");
    }

    private void AddValidationMessage(string propertyName, string message)
    {
      ModelState.AddModelError(propertyName, message);
    }

    [HttpPost]
    public async Task<ActionResult> Send(ViewModels.Viewer viewModel)
    {
      Models.Settings settings = new();
      settings.ReadSettings(this.Context.Module);

      viewModel.Message.Category = await this.ListManager.GetListItem(viewModel.Message.Category.Id);

      ModelState.Clear();
      this.TryValidateModel(viewModel);

      Validate(viewModel, settings);

      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }

      if (this.Context != null && this.Context.Site != null)
      {
        // send contact email (if recipients and mail template have been set)
        if (!String.IsNullOrEmpty(settings.SendTo))
        {
          if (settings.MailTemplateId != Guid.Empty)
          {
            MailTemplate template = await this.MailTemplateManager.Get(settings.MailTemplateId);
            if (template != null)
            {
              Models.Mail.TemplateModel args = new()
              {
                Site = this.Context.Site,
                Message = viewModel.Message,
                Settings = settings
              };

              Logger.LogTrace("Sending contact email '{emailTemplateName}' to '{sendTo}'.", template.Name, settings.SendTo);

              try
              {
                using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
                {
                  await mailClient.Send(template, args, settings.SendTo);
                }
              }
              catch (Exception ex)
              {
                Logger.LogError(ex, "Error sending contact email '{emailTemplateName}' to '{sendTo}'.", template.Name, settings.SendTo);
                return Problem("There was an error sending the contact email.", null, (int)System.Net.HttpStatusCode.InternalServerError, "Error Sending Email");
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

      viewModel.MessageSent = true;

      return View("Viewer", viewModel);
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
      this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_SEND_TO, viewModel.SendTo);

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

      viewModel.IsAdmin = User.IsSiteAdmin(this.Context.Site);
      viewModel.ReadSettings(this.Context.Module);
      viewModel.CategoryList = (await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(Models.Settings.MODULESETTING_CATEGORYLIST_ID, Guid.Empty)));

      // default values if user is logged on
      if (User.Identity.IsAuthenticated)
      {
        viewModel.Message.FirstName = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.GivenName);
        viewModel.Message.LastName = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.Surname);
        viewModel.Message.Email = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.Email);
        viewModel.Message.PhoneNumber = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.OtherPhone);
      }

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