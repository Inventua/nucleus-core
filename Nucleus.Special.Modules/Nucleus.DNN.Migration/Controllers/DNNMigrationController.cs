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
    // we await the role group import, because it is likely to be fast
    await this.DNNMigrationManager.Migrate<Models.DNN.RoleGroup>(this.HttpContext.RequestServices, viewModel.RoleGroups);

    // run the role import asynchronously
    Task task = this.DNNMigrationManager.Migrate<Models.DNN.Role>(this.HttpContext.RequestServices, viewModel.Roles);

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
    ViewModels.Progress viewModel = new()
    {
      CurrentOperationEngine = this.DNNMigrationManager.CurrentOperation
    };

    if (viewModel.CurrentOperationEngine.Completed())
    {

    }
    return Task.FromResult(viewModel);
  }

  private async Task<ViewModels.Role> BuildRolesViewModel(int portalId)
  {
    ViewModels.Role viewModel = new();
    viewModel.PortalId = portalId;
    
    viewModel.RoleGroups = await this.DNNMigrationManager.ListDNNRoleGroups(portalId);
    viewModel.Roles = await this.DNNMigrationManager.ListDNNRoles(portalId);

    await this.HttpContext.RequestServices.CreateEngine<Models.DNN.RoleGroup>().Validate(viewModel.RoleGroups);
    await this.HttpContext.RequestServices.CreateEngine<Models.DNN.Role>().Validate(viewModel.Roles);
    
    return viewModel;
  }

  private async Task<ViewModels.Page> BuildPagesViewModel(int portalId)
  {
    ViewModels.Page viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Pages = await this.DNNMigrationManager.ListDNNPages(portalId);
    await this.HttpContext.RequestServices.CreateEngine<Models.DNN.Page>().Validate(viewModel.Pages);

    return viewModel;
  }

  private async Task<ViewModels.User> BuildUsersViewModel(int portalId)
  {
    ViewModels.User viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Users = await this.DNNMigrationManager.ListDNNUsers(portalId);
    await this.HttpContext.RequestServices.CreateEngine<Models.DNN.User>().Validate(viewModel.Users);

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
