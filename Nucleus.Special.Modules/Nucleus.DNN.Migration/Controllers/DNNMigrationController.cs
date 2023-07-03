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
using Markdig.Extensions.Footnotes;
using static Nucleus.DNN.Migration.MigrationEngines.MigrationEngineBase;

//https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.storage.irelationalcommand.executereaderasync?view=efcore-7.0

namespace Nucleus.DNN.Migration.Controllers;

[Extension("DNNMigration")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
[DisableMaxModelBindingCollectionSize]
public class DNNMigrationController : Controller
{
  private Context Context { get; }
  private DNNMigrationManager DNNMigrationManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  private IOptions<DatabaseOptions> DatabaseOptions { get; }

  public DNNMigrationController(Context Context, DNNMigrationManager dnnMigrationManager, IFileSystemManager fileSystemManager, IOptions<DatabaseOptions> databaseOptions)
  {
    this.Context = Context;
    this.DNNMigrationManager = dnnMigrationManager;
    this.FileSystemManager = fileSystemManager;
    this.DatabaseOptions = databaseOptions;
  }

  [HttpGet]
  public async Task <ActionResult> Index()
  {
    return View("Index", await BuildIndexViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> RolesIndex(int portalId)
  {
    return View("_Roles", await BuildRolesViewModel(portalId));
  }

  [HttpPost]
  public async Task<ActionResult> ListsIndex(int portalId)
  {
    return View("_Lists", await BuildListsViewModel(portalId));
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

  [HttpPost]
  [DisableRequestSizeLimitAttribute]
  [RequestFormLimits(ValueCountLimit = Int32.MaxValue)]
  public async Task<ActionResult> MigrateRoles(ViewModels.Role viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().UpdateSelections(viewModel.RoleGroups);
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().UpdateSelections(viewModel.Roles);

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().SignalStart();
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().SignalStart();
      
    Task task = Task.Run(async () => 
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.RoleGroup>().Migrate(viewModel.UpdateExisting);
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Role>().Migrate(viewModel.UpdateExisting);
    });
    
    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> MigrateLists(ViewModels.List viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.List>().UpdateSelections(viewModel.Lists);

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.List>().SignalStart();

    Task task = Task.Run(async () =>
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.List>().Migrate(viewModel.UpdateExisting);
    });

    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  [DisableRequestSizeLimitAttribute]
  [RequestFormLimits(ValueCountLimit = Int32.MaxValue)]
  public async Task<ActionResult> MigrateUsers(ViewModels.User viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.User>().UpdateSelections(viewModel.Users);
    
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.User>().SignalStart();

    Task task = Task.Run(async () =>
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.User>().Migrate(viewModel.UpdateExisting);
    });

    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  [DisableRequestSizeLimitAttribute]
  [RequestFormLimits(ValueCountLimit = Int32.MaxValue)]
  public async Task<ActionResult> MigratePages(ViewModels.Page viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().UpdateSelections(viewModel.Pages);
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().SignalStart();

    Task task = Task.Run(async () =>
    {
      this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Message = "Synchronizing file system ...";
      await SyncFileSystem();

      this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Message = "";

      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Migrate(viewModel.UpdateExisting);
    });

    return View("_Progress", await BuildProgressViewModel());
  }


  [HttpGet]
  public async Task<ActionResult> UpdateProgress(ViewModels.Progress viewModel)
  {
    return View("_Progress", await BuildProgressViewModel());
  }

  /// <summary>
  /// Traverse file system folders to detect new files.
  /// </summary>
  private async Task SyncFileSystem()
  {
    Folder topFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.FileSystemManager.ListProviders().First().Key, "");
    Folder folderData = await this.FileSystemManager.ListFolder(this.Context.Site, topFolder.Id, "");

    foreach (Folder subFolder in folderData.Folders)
    {
      await SyncFolder(subFolder);
    }
  }

  /// <summary>
  /// Traverse file system folders to detect new files.
  /// </summary>
  /// <param name="folder"></param>
  /// <returns></returns>
  private async Task SyncFolder(Folder folder)
  {
    Folder folderData = await this.FileSystemManager.ListFolder(this.Context.Site, folder.Id, "");
    foreach (Folder subFolder in folderData.Folders)
    {
      await SyncFolder(subFolder);
    }
  }

  private async Task <ViewModels.Index> BuildIndexViewModel()
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

    string message = this.DNNMigrationManager.GetMigrationEngines.Where(engine => engine.State() == EngineStates.InProgress)
      .Select(engine => engine.Message)
      .FirstOrDefault(); ;

    ViewModels.Progress viewModel = new()
    {
      EngineProgress = progress,
      InProgress = doRefresh,
      Message = message
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

  private async Task<ViewModels.List> BuildListsViewModel(int portalId)
  {
    ViewModels.List viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Lists = await this.DNNMigrationManager.ListDNNLists(portalId);
    
    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.List>(this.HttpContext.RequestServices, viewModel.Lists);
    
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
