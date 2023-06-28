using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.DNN.Migration.MigrationEngines;
using Nucleus.ViewFeatures.TagHelpers;
using DocumentFormat.OpenXml.Math;

//https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.storage.irelationalcommand.executereaderasync?view=efcore-7.0

namespace Nucleus.DNN.Migration.Controllers;

[Extension("DNNMigration")]
public class DNNMigrationController : Controller
{
  private Context Context { get; }
  private DNNMigrationManager DNNMigrationManager { get; }
  private IOptions<DatabaseOptions> DatabaseOptions { get; }

  public DNNMigrationController(Context Context, DNNMigrationManager dnnMigrationManager, IOptions<DatabaseOptions> databaseOptions)
  {
    this.Context = Context;
    this.DNNMigrationManager = dnnMigrationManager;
    this.DatabaseOptions = databaseOptions;
  }

  [HttpGet]
  public async Task <ActionResult> Index()
  {
    return View("Index", await BuildViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> RolesIndex(int portalId)
  {
    return View("_Roles", await BuildRolesViewModel(portalId));
  }

  [HttpPost]
  public async Task<ActionResult> MigrateRoles(ViewModels.Role viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().UpdateSelections(viewModel.RoleGroups);
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().UpdateSelections(viewModel.Roles);

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().SignalStart();
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().SignalStart();
      
    Task task = Task.Run(async () => 
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().Migrate();
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().Migrate();
    });
    
    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpGet]
  public async Task<ActionResult> UpdateProgress(ViewModels.Progress viewModel)
  {  
    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> PagesIndex(ViewModels.Index viewModel)
  {
    return View("_Pages", await BuildPagesViewModel(viewModel.PortalId));
  }

  [HttpPost]
  public async Task<ActionResult> UsersIndex(ViewModels.Index viewModel)
  {
    return View("_Users", await BuildUsersViewModel(viewModel.PortalId));
  }


  private async Task <ViewModels.Index> BuildViewModel()
  {
    ViewModels.Index viewModel = new();

    viewModel.Version = await this.DNNMigrationManager.GetDNNVersion();
    viewModel.Portals = await this.DNNMigrationManager.ListDNNPortals();

    DatabaseConnectionOption connection = this.DatabaseOptions.Value.GetDatabaseConnection(Startup.DNN_SCHEMA_NAME);
    if (connection != null)
    {
      viewModel.ConnectionString = Sanitize(connection.ConnectionString);
    }
  
    return viewModel;
  }

  private Task <ViewModels.Progress> BuildProgressViewModel()
  {
    // copy the engine object data to a EngineProgress object so that the progress values do not change during page rendering
    IEnumerable<EngineProgress> progress = this.DNNMigrationManager.GetMigrationEngines.Select(engine => engine.GetProgress());    
    Boolean doRefresh = progress.Any(engine => engine.State != Nucleus.DNN.Migration.MigrationEngines.MigrationEngineBase.EngineStates.Completed);

    ViewModels.Progress viewModel = new()
    {
      EngineProgress = progress,
      InProgress = doRefresh
    };

    return Task.FromResult(viewModel);
  }

  private async Task<ViewModels.Role> BuildRolesViewModel(int portalId)
  {
    ViewModels.Role viewModel = new();
    viewModel.PortalId = portalId;
    
    viewModel.RoleGroups = await this.DNNMigrationManager.ListDNNRoleGroups(portalId);
    viewModel.Roles = await this.DNNMigrationManager.ListDNNRoles(portalId);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.RoleGroup>(this.HttpContext.RequestServices, viewModel.RoleGroups);
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.Role>(this.HttpContext.RequestServices, viewModel.Roles);
    
    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }
    
    return viewModel;
  }

  private async Task<ViewModels.Page> BuildPagesViewModel(int portalId)
  {
    ViewModels.Page viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Pages = await this.DNNMigrationManager.ListDNNPages(portalId);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.Page>(this.HttpContext.RequestServices, viewModel.Pages);    

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }


    return viewModel;
  }

  private async Task<ViewModels.User> BuildUsersViewModel(int portalId)
  {
    ViewModels.User viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Users = await this.DNNMigrationManager.ListDNNUsers(portalId);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.User>(this.HttpContext.RequestServices, viewModel.Users);

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }

    return viewModel;
  }

  /// <summary>
  /// Look for connection strings and passwords and hide them
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  private static string Sanitize(string value)
  {
    const string TOKEN_REGEX = "(?<Pair>(?<Key>[^=;\"]+)=(?<Value>[^;]+))";

    return System.Text.RegularExpressions.Regex.Replace(value, TOKEN_REGEX, new System.Text.RegularExpressions.MatchEvaluator(SanitizeToken));
  }

  private static string SanitizeToken(System.Text.RegularExpressions.Match match)
  {
    string[] securityTokens = { "password", "pwd", "user", "userid", "user id", "uid", "username", "user name", "connectionstring" };

    if (match.Groups["Key"].Success)
    {
      if (securityTokens.Contains(match.Groups["Key"].Value, StringComparer.OrdinalIgnoreCase))
      {
        return match.Groups["Key"].Value + "=" + "****";
      }
      else
      {
        return match.Groups[0].Value;
      }
    }
    else
    {
      return String.Empty;
    }

  }
}
