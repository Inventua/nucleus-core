using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Web.Controllers.Admin
{
  [Area("Admin")]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
  public class MailTemplatesController : Controller
  {
    private const int MODEL_MAX_DEPTH = 5;
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
    /// Display the list of templates
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> Index()
    {
      return View("Index", await BuildViewModel());
    }

    /// <summary>
    /// Display the templates list
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> List(ViewModels.Admin.MailTemplateIndex viewModel)
    {
      return View("_MailTemplatesList", await BuildViewModel(viewModel));
    }

    /// <summary>
    /// Display the mail template editor
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> Editor(Guid id)
    {
      ViewModels.Admin.MailTemplateEditor viewModel;

      viewModel = await BuildViewModel(id == Guid.Empty ? await MailTemplateManager.CreateNew() : await MailTemplateManager.Get(id));

      return View("Editor", viewModel);
    }

    /// <summary>
    /// Refresh drop-down lists and template data model
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> Refresh(ViewModels.Admin.MailTemplateEditor viewModel)
    {
      viewModel = await BuildViewModel(viewModel.MailTemplate);

      return View("Editor", viewModel);
    }

    [HttpPost]
    public async Task<ActionResult> AddMailTemplate()
    {
      return View("Editor", await BuildViewModel(new MailTemplate()));
    }

    [HttpPost]
    public async Task<ActionResult> Verify(ViewModels.Admin.MailTemplateEditor viewModel)
    {      
      if (String.IsNullOrEmpty(viewModel.MailTemplate.DataModelTypeName))
      {
        ModelState.Clear();
        ModelState.AddModelError<ViewModels.Admin.MailTemplateEditor>(viewModel => viewModel.MailTemplate.DataModelTypeName, "Please select a mail template type.");
        return BadRequest(ModelState);
      }

      Type modelType = Type.GetType(viewModel.MailTemplate.DataModelTypeName);

      {
        var result = await Nucleus.Extensions.Razor.RazorParser.TestCompile(modelType, viewModel.MailTemplate.Subject);
        if (!result.Success)
        {
          return Json(new { Title = "Error Compiling Subject", Message = result.Errors, Icon = "error" });
        }
      }

      {
        var result = await Nucleus.Extensions.Razor.RazorParser.TestCompile(modelType, viewModel.MailTemplate.Body);
        if (!result.Success)
        {
          return Json(new { Title = "Error Compiling Body", Message = result.Errors, Icon = "error" });
        }
      }

      return Json(new { Title = "Verify", Message = "The subject and message body were compiled successfully.", Icon = "alert" });
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
      return await BuildViewModel(new ViewModels.Admin.MailTemplateIndex());
    }

    private async Task<ViewModels.Admin.MailTemplateIndex> BuildViewModel(ViewModels.Admin.MailTemplateIndex viewModel)
    {
      viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site, viewModel.MailTemplates);
      return viewModel;
    }

    private async Task<ViewModels.Admin.MailTemplateEditor> BuildViewModel(MailTemplate mailTemplate)
    {
      ViewModels.Admin.MailTemplateEditor viewModel = new()
      {
        MailTemplate = mailTemplate,

        AvailableDataModelTypes =
         (await this.MailTemplateManager.ListTemplateDataModelTypes())
         .Select(type => new ViewModels.Admin.ScheduledTaskEditor.ServiceType(GetFriendlyName(type), $"{type.FullName},{type.Assembly.GetName().Name}"))
      };


      viewModel.DataModel = BuildDataModel(viewModel.MailTemplate.DataModelTypeName);

      return viewModel;
    }

    private string BuildDataModel(string dataModelTypeName)
    {
      System.Type dataModelType;
      Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> results = [];

      if (!String.IsNullOrEmpty(dataModelTypeName))
      {
        dataModelType = Type.GetType(dataModelTypeName);
      }
      else
      {
        // default
        dataModelType = typeof(Nucleus.Abstractions.Models.Mail.Template.UserMailTemplateData);
      }

      if (dataModelType != null)
      {
        results = BuildDataModelProperties(dataModelType, MODEL_MAX_DEPTH);
      }

      results.Add("#ClaimTypes", new() { Properties = BuildDataModelProperties(typeof(System.Security.Claims.ClaimTypes), 1) });

      AddExtensionFunctions(results, typeof(Nucleus.Extensions.SiteExtensions));
      AddExtensionFunctions(results, typeof(Nucleus.Extensions.UserExtensions));
      AddExtensionFunctions(results, typeof(Nucleus.Extensions.UserProfilePropertyExtensions));
      AddExtensionFunctions(results, typeof(Nucleus.Extensions.FileExtensions));
      AddExtensionFunctions(results, typeof(Nucleus.Extensions.PageExtensions));
      AddExtensionFunctions(results, typeof(Nucleus.Extensions.SitePagesExtensions));

      return Newtonsoft.Json.JsonConvert.SerializeObject(results, new Newtonsoft.Json.JsonSerializerSettings()
      {
        DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore
      });
    }

    private void AddExtensionFunctions(Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> results, System.Type extensionType)
    {
      foreach (MethodInfo method in extensionType.GetMethods())
      {
        if (method.GetCustomAttribute<System.Runtime.CompilerServices.ExtensionAttribute>() != null)
        {
          ParameterInfo[] parameters = method.GetParameters();
          ParameterInfo thisParameter = parameters.FirstOrDefault();

          if (thisParameter != null)
          {
            ParameterInfo[] otherParameters = parameters.Skip(1).ToArray();

            AddExtensionFunctions(method, results, thisParameter, otherParameters);
          }
        }
      }
    }

    private void AddExtensionFunctions(MethodInfo method, Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> results, ParameterInfo thisParameter, ParameterInfo[] otherParameters)
    {
      // suppress the User.GetCensored method.  User data sent to mail is already censored.
      if (method.Name == "GetCensored") return;

      foreach (ViewModels.Admin.MailTemplateEditor.DataModelElement element in results.Values)
      {
        if (element.Type != null && element.Type.Equals(thisParameter.ParameterType))
        {
          if (element.Functions == null)
          {
            element.Functions = [];
          }

          element.Functions.Add(method.Name + "_" + String.Join('_', otherParameters.Select(parm => $"{parm.Name}-{parm.ParameterType.Name}")), new()
          {
            Label = $"{method.Name}({String.Join(", ", otherParameters.Select(parm => parm.Name))})",
            Code = $"{method.Name}({String.Join(", ", otherParameters.Select(parm => parm.Name))})",
            Kind = ViewModels.Admin.MailTemplateEditor.DataModelElement.Kinds.Function,
            Properties = method.ReturnType.IsClass ? BuildDataModelProperties(method.ReturnType, MODEL_MAX_DEPTH) : null
          });
        }        
      }
    }

    private ViewModels.Admin.MailTemplateEditor.DataModelElement BuildDataModelItem(PropertyInfo prop, int levels)
    {
      ViewModels.Admin.MailTemplateEditor.DataModelElement result = new()
      {
        Type = prop.PropertyType,
        Label = prop.Name,
        Kind = ViewModels.Admin.MailTemplateEditor.DataModelElement.Kinds.Property
      };

      if (levels > 0)
      {
        result.Properties = BuildDataModelProperties(prop.PropertyType, levels - 1);
      }

      return result;
    }

    private Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> BuildDataModelProperties(System.Type type, int levels)
    {
      if (type == null) return null;

      Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> properties = [];

      foreach (PropertyInfo childProp in type.GetProperties())
      {
        if (childProp.CanRead && childProp.GetGetMethod()?.IsPublic == true)
        {
          if (childProp.PropertyType.IsClass && !childProp.PropertyType.Equals(typeof(System.String)))
          {
            properties.Add(childProp.Name, BuildDataModelItem(childProp, levels));
          }
          else
          {
            properties.Add(childProp.Name, new() { Type = childProp.PropertyType, Label = childProp.Name, Kind = ViewModels.Admin.MailTemplateEditor.DataModelElement.Kinds.Property });
          }
        }
      }

      foreach (FieldInfo field in type.GetFields())
      {
        if (field.IsPublic)
        {
          properties.Add(field.Name, new() { Type = field.FieldType, Label = field.Name, Kind = ViewModels.Admin.MailTemplateEditor.DataModelElement.Kinds.Field });          
        }
      }

      if (properties.Any())
      {
        return properties;
      }

      return null;
    }

    private Dictionary <string, ViewModels.Admin.MailTemplateEditor.DataModelElement> BuildDataModelEnum(System.Type type)
    {
      if (type == null) return null;

      Dictionary<string, ViewModels.Admin.MailTemplateEditor.DataModelElement> values = [];

      foreach (string enumName in type.GetEnumNames())
      {        
        values.Add(enumName, new() { Type = type, Kind = ViewModels.Admin.MailTemplateEditor.DataModelElement.Kinds.Enum } );
      }

      if (values.Any())
      {
        return values;
      }

      return null;
    }

    private static string GetFriendlyName(System.Type type)
    {
      System.ComponentModel.DisplayNameAttribute displayNameAttribute = type.GetCustomAttributes(false)
        .Where(attr => attr is System.ComponentModel.DisplayNameAttribute)
        .Select(attr => attr as System.ComponentModel.DisplayNameAttribute)
        .FirstOrDefault();

      if (displayNameAttribute == null)
      {
        return $"{type.FullName}";
      }
      else
      {
        return displayNameAttribute.DisplayName;
      }

    }
  }
}
