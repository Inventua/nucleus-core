using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Http;
using Nucleus.Data.Common;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Portable;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
public class MailSettingsController : Controller
{
  private Context Context { get; }

  private ISiteManager SiteManager { get; }

  private IMailClientFactory MailClientFactory { get; }
  private IEnumerable<IMailClient> MailClients { get; }

  private ILogger<MailSettingsController> Logger { get; }

  public MailSettingsController(Context context, ISiteManager siteManager, IMailClientFactory mailClientFactory, IEnumerable<IMailClient> mailClients, ILogger<MailSettingsController> logger)
  {
    this.Context = context;
    this.SiteManager = siteManager;
    this.MailClientFactory = mailClientFactory;
    this.MailClients = mailClients;
    this.Logger = logger;
  }

  /// <summary>
  /// Display the mail settings editor
  /// </summary>
  /// <returns></returns>
  [HttpGet]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
  public async Task<ActionResult> Index()
  {
    return View("Index", await BuildViewModel(null));
  }

  /// <summary>
  /// Display the mail settings editor
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
  public async Task<ActionResult> SelectMailClient(ViewModels.Admin.MailSettings viewModel)
  {
    return View("Index", await BuildViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> Save(ViewModels.Admin.MailSettings viewModel, [FromForm] Dictionary<string, string> settings)
  {
    this.Context.Site.SetMailClientType(viewModel.DefaultMailClientTypeName);
    PopulateSettings(this.Context.Site, viewModel, settings);
    
    await this.SiteManager.Save(this.Context.Site);

    return Ok();  
  }

  [HttpPost]
  public async Task<ActionResult> TestMailSettings(ViewModels.Admin.MailSettings viewModel, [FromForm] Dictionary<string, string> settings)
  {
    if (String.IsNullOrEmpty(viewModel.SendTestMailTo))
    {
      ModelState.Clear();
      ModelState.AddModelError<ViewModels.Admin.MailSettings>(viewModel => viewModel.SendTestMailTo, "Please enter an email address.");
      return BadRequest(ModelState);
    }

    // we make a copy of the site object so that we aren't changing the values of the cached site object when testing settings.
    Site site = this.Context.Site.CopyTo<Site>();
    site.SetMailClientType(viewModel.DefaultMailClientTypeName);
    PopulateSettings(site, viewModel, settings);

    using (IMailClient client = this.MailClientFactory.Create(site))
    {
      try
      {
        client.Site = site;
        await client.Send(new Abstractions.Models.Mail.MailTemplate()
        {
          Subject = $"Email Configuration Test from {this.Context.Site.Name}, {DateTime.Now}",
          Body = $"This email was generated as a test by the user {User.Identity.Name} at {DateTime.Now}.  If you received it, then your site's email configuration is working correctly."
        },
          new object(),
          viewModel.SendTestMailTo);
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
    }

    return Json(new { Title = "Test Email Settings", Message = $"A test email was sent successfully to '{viewModel.SendTestMailTo}'.", Icon = "alert" });    
  }

  private Task<ViewModels.Admin.MailSettings> BuildViewModel(ViewModels.Admin.MailSettings viewModel)
  {
    IMailClient mailClient = null;

    if (viewModel == null)
    {
      viewModel = new();
      viewModel.DefaultMailClientTypeName = this.Context.Site.GetMailClientType();
    }

    viewModel.AvailableMailClientTypes =
      this.MailClients
        .Select(mailClient => new ViewModels.Admin.ScheduledTaskEditor.ServiceType(mailClient.GetType().GetFriendlyName(), mailClient.GetType().ShortTypeName()));

    mailClient = this.MailClients
      .Where(mailClient => viewModel.DefaultMailClientTypeName != null && mailClient.GetType().ShortTypeName() == viewModel.DefaultMailClientTypeName)
      .FirstOrDefault();
    
    viewModel.SettingsPath = mailClient?.SettingsPath;

    if (mailClient != null)
    {
      viewModel.Settings = mailClient.GetSettings(this.Context.Site);
    }    

    viewModel.SettingsViewData = new(this.ViewData);
    viewModel.SettingsViewData.TemplateInfo.HtmlFieldPrefix = "Settings"; // this must match the 2nd argument in the Save() & TestMailSettings method

    return Task.FromResult(viewModel);
  }


  private void PopulateSettings(Site site, ViewModels.Admin.MailSettings viewModel, [FromForm] Dictionary<string, string> settings)
  {
    IMailClient mailClient = this.MailClients
      .Where(mailClient => viewModel.DefaultMailClientTypeName != null && mailClient.GetType().ShortTypeName() == viewModel.DefaultMailClientTypeName)
      .FirstOrDefault();

    if (mailClient != null)
    {
      IMailSettings mailSettings = mailClient.GetSettings(this.Context.Site);

      foreach (KeyValuePair<string, string> value in settings)
      {
        System.Reflection.PropertyInfo prop = mailSettings.GetType().GetProperty(value.Key);
        if (prop != null)
        {
          prop.SetValue(mailSettings, Convert.ChangeType(value.Value, prop.PropertyType));
        }
      }

      mailClient.SetSettings(site, mailSettings);
    }

  }
}