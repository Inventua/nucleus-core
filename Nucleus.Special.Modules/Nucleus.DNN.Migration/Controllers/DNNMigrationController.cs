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
using static Nucleus.DNN.Migration.MigrationEngines.MigrationEngineBase;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
  private IUserManager UserManager { get; }
  private ILayoutManager LayoutManager { get; }
  private IContainerManager ContainerManager { get; }
  private IMailTemplateManager MailTemplateManager { get; }

  private IOptions<DatabaseOptions> DatabaseOptions { get; }

  public DNNMigrationController(Context Context, DNNMigrationManager dnnMigrationManager, IFileSystemManager fileSystemManager, IOptions<DatabaseOptions> databaseOptions, IUserManager userManager, IMailTemplateManager mailTemplateManager, ILayoutManager layoutManager, IContainerManager containerManager)
  {
    this.Context = Context;
    this.DNNMigrationManager = dnnMigrationManager;
    this.FileSystemManager = fileSystemManager;
    this.DatabaseOptions = databaseOptions;
    this.UserManager = userManager;
    this.MailTemplateManager = mailTemplateManager;
    this.LayoutManager = layoutManager;
    this.ContainerManager = containerManager;
  }

  [HttpGet]
  public async Task<ActionResult> Index()
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
  public async Task<ActionResult> FoldersIndex(int portalId)
  {
    return View("_Folders", await BuildFoldersViewModel(portalId));
  }


  [HttpPost]
  public async Task<ActionResult> ForumsIndex(int portalId)
  {
    return View("_Forums", await BuildForumsViewModel(portalId));
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
  public async Task<ActionResult> NotifyUsersIndex(ViewModels.Notify viewModel)
  {
    return View("_Notify", await BuildNotifyUsersViewModel());
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
  public async Task<ActionResult> MigrateFolders(ViewModels.Folder viewModel)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }
    List<Models.DNN.Portal> portals = await this.DNNMigrationManager.ListDnnPortals();

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().UpdateSelections(viewModel.Folders);

    MigrationEngines.FilesMigration engine = this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>() as MigrationEngines.FilesMigration;
    engine.SetAlias
    (
      viewModel.UseSSL,
      portals
        .Where(portal => portal.PortalId == viewModel.PortalId)
        .SelectMany(portal => portal.PortalAliases)
        .Where(alias => alias.PortalAliasId == viewModel.PortalAliasId)
        .FirstOrDefault()
    );

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().SignalStart();

    engine.CalculateProgressTotal();

    Task task = Task.Run(async () =>
    {
      this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().Message = "Synchronizing file system ...";

      try
      {
        await SyncFileSystem();
        this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().Message = "";
      }
      catch (Exception e)
      {
        this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().Message = $"Sync File System Error: {e.Message}";
      }
            
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Folder>().Migrate(viewModel.UpdateExisting);      
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

    (this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>() as MigrationEngines.PageMigration)
      .Setup(viewModel.DNNSkins, viewModel.DNNContainers);

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().SignalStart();

    Task task = Task.Run(async () =>
    {
      this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Message = "Synchronizing file system ...";

      try
      {
        await SyncFileSystem();
        this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Message = "";
      }
      catch (Exception e)
      {
        this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Message = $"Sync File System Error: {e.Message}";
      }

      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Page>().Migrate(viewModel.UpdateExisting);
    });

    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  [DisableRequestSizeLimitAttribute]
  [RequestFormLimits(ValueCountLimit = Int32.MaxValue)]
  public async Task<ActionResult> MigrateForums(ViewModels.Forum viewModel)
  {
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().UpdateSelections(viewModel.Forums);
    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().SignalStart();

    // The forums migration is different than the others, because it is migrating forum posts, not the forums themselves.  We have to
    // manually set TotalCount
    viewModel.TotalPosts = 0;

    foreach (Models.DNN.Modules.Forum forum in viewModel.Forums)
    {
      if (forum.CanSelect && forum.IsSelected)
      {
        viewModel.TotalPosts += await this.DNNMigrationManager.CountForumPosts(forum.ForumId);
      }
    }

    this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().TotalCount = viewModel.TotalPosts;

    Task task = Task.Run(async () =>
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().Migrate(viewModel.UpdateExisting);

      // Forum migration can end up with less posts/replies than predicted because of orphaned records, etc, so we
      // have to manually signal completion.
      this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().Current = this.DNNMigrationManager.GetMigrationEngine<Models.DNN.Modules.Forum>().TotalCount;
    });

    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpPost]
  [DisableRequestSizeLimitAttribute]
  [RequestFormLimits(ValueCountLimit = Int32.MaxValue)]
  public async Task<ActionResult> NotifyUsers(ViewModels.Notify viewModel)
  {
    if (viewModel.NotifyUserTemplateId == Guid.Empty)
    {
      ModelState.Clear();
      ModelState.AddModelError<ViewModels.Notify>(viewModel => viewModel.NotifyUserTemplateId, "Please select a mail template.");
      return BadRequest(ModelState);
    }

    NotifyUsers engine = (NotifyUsers)this.DNNMigrationManager.GetMigrationEngine<Models.NotifyUser>();
    engine.SetTemplate(await this.MailTemplateManager.Get(viewModel.NotifyUserTemplateId));

    this.DNNMigrationManager.GetMigrationEngine<Models.NotifyUser>().UpdateSelections(viewModel.Users);
    this.DNNMigrationManager.GetMigrationEngine<Models.NotifyUser>().SignalStart();

    Task task = Task.Run(async () =>
    {
      await this.DNNMigrationManager.GetMigrationEngine<Models.NotifyUser>().Migrate(false);
    });

    return View("_Progress", await BuildProgressViewModel());
  }

  [HttpGet]
  public ActionResult ExportResults(int index)
  {
    var exporter = new Nucleus.Extensions.Excel.ExcelWriter<Models.DNN.DNNEntity>();

    exporter.AddColumn("Name", "Name", ClosedXML.Excel.XLDataType.Text , entity => entity.DisplayName());
    exporter.AddColumn("ID", "ID", ClosedXML.Excel.XLDataType.Number, entity => entity.Id());
    exporter.AddColumn("Results", "Results", ClosedXML.Excel.XLDataType.Text, 
      entity => entity.Results.Any() ? String.Join("\n", entity.Results.Select(result=>result.ToString())) : "Success"
    );

    if (index >= this.DNNMigrationManager.GetMigrationEngines.Count)
    {
      return BadRequest();
    }

    MigrationEngineBase engine = this.DNNMigrationManager.GetMigrationEngines[index];

    string worksheetName = engine.Title.Replace("Migrating ", "");
    if (worksheetName.Length > 32)
    {
      worksheetName= worksheetName.Substring(0, 32);
    }
    exporter.Worksheet.Name = worksheetName;
    exporter.Export(engine.InnerItems);

    return File(exporter.GetOutputStream(), Nucleus.Extensions.Excel.ExcelWorksheet.MIMETYPE_EXCEL, $"{engine.Title.Replace("Migrating", "Migration Results - ")} {DateTime.Now}.xlsx");
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

  private async Task<ViewModels.Index> BuildIndexViewModel()
  {
    ViewModels.Index viewModel = new();

    DatabaseConnectionOption connection = this.DatabaseOptions.Value.GetDatabaseConnection(Startup.DNN_SCHEMA_NAME, false);
    if (connection != null)
    {
      viewModel.Version = await this.DNNMigrationManager.GetDnnVersion();
      viewModel.Portals = await this.DNNMigrationManager.ListDnnPortals();

      if (viewModel.Version.ToVersion() < new System.Version(5,5,1))
      {
        viewModel.VersionWarning = true;
      }
      viewModel.ConnectionString = Sanitize(connection.ConnectionString);
    }

    return viewModel;
  }

  private Task<ViewModels.Progress> BuildProgressViewModel()
  {
    // copy the engine object data to a EngineProgress object so that the progress values do not change during page rendering
    IEnumerable<EngineProgress> progress = this.DNNMigrationManager.GetMigrationEngines.Select(engine => engine.GetProgress());
    Boolean doRefresh = progress.Any(engine => engine.State != Nucleus.DNN.Migration.MigrationEngines.MigrationEngineBase.EngineStates.Completed);

    string message = this.DNNMigrationManager.GetMigrationEngines.Where(engine => engine.State() == EngineStates.InProgress)
      .Select(engine => engine.Message)
      .FirstOrDefault();

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

    viewModel.RoleGroups = await this.DNNMigrationManager.ListDnnRoleGroups(portalId);
    viewModel.Roles = await this.DNNMigrationManager.ListDnnRoles(portalId);

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

    viewModel.Lists = await this.DNNMigrationManager.ListDnnLists(portalId);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.List>(this.HttpContext.RequestServices, viewModel.Lists);

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }

    return viewModel;
  }

  private async Task<ViewModels.Folder> BuildFoldersViewModel(int portalId)
  {
    Stack<Models.DNN.Folder> folderHierachy = new();

    ViewModels.Folder viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.AvailablePortalAliases = (await this.DNNMigrationManager.ListDnnPortals())
      .Where(portal => portal.PortalId == portalId)
      .FirstOrDefault()
      .PortalAliases;

    viewModel.Folders = await this.DNNMigrationManager.ListDnnFolders(portalId);

    foreach (Models.DNN.Folder folder in viewModel.Folders)
    {
      if (String.IsNullOrEmpty(folder.FolderPath))
      {
        folder.FolderName = "/";
      }
      else
      {
        folder.FolderName = folder.FolderPath
          .Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
          .LastOrDefault() ?? "";
      }

      if (!folderHierachy.Any())
      {
        folderHierachy.Push(folder);
      }
      else
      {
        // walk back the stack to the ancestor of the current folder
        while (!folder.FolderPath.StartsWith(folderHierachy.Peek().FolderPath, StringComparison.OrdinalIgnoreCase))
        {
          folderHierachy.Pop();
        }

        folder.ParentId = folderHierachy.Peek().FolderId;
        folder.Level = folderHierachy.Count;
        folderHierachy.Peek().FolderCount++;

        // add the current folder to the stack
        folderHierachy.Push(folder);        
      }


    }

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.Folder>(this.HttpContext.RequestServices, viewModel.Folders);

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }

    return viewModel;
  }

  private async Task<ViewModels.Forum> BuildForumsViewModel(int portalId)
  {
    ViewModels.Forum viewModel = new();
    viewModel.PortalId = portalId;
    viewModel.TotalPosts = 0;

    try
    {
      viewModel.ForumGroups = await this.DNNMigrationManager.ListDnnForumGroupsByPortal(portalId);

      foreach (Models.DNN.Modules.Forum forum in viewModel.ForumGroups.SelectMany(group => group.Forums))
      {
        forum.PostCount = await this.DNNMigrationManager.CountForumPosts(forum.ForumId);
        viewModel.TotalPosts += forum.PostCount;
      }

      this.DNNMigrationManager.ClearMigrationEngines();
      await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.Modules.Forum>(this.HttpContext.RequestServices, viewModel.ForumGroups.SelectMany(group => group.Forums).ToList());

      foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
      {
        await engine.Validate();
      }
    }
    catch (Exception ex)
    {
      if (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
      {
        viewModel.ForumsNotInstalled = true;
      }
      else
      {
        throw;
      }
    }

    return viewModel;
  }

  private async Task<ViewModels.Page> BuildPagesViewModel(int portalId)
  {
    ViewModels.Page viewModel = new();
    viewModel.PortalId = portalId;

    viewModel.Pages = await this.DNNMigrationManager.ListDnnPages(portalId);

    viewModel.DNNSkins = await this.DNNMigrationManager.ListSkins(viewModel.PortalId);
    viewModel.Layouts = await this.LayoutManager.List();

    foreach(Models.DNN.Skin skin in viewModel.DNNSkins)
    {
      skin.AssignedLayoutId = await FindBestMatch(skin, viewModel.Layouts);
    }

    viewModel.DNNContainers = await this.DNNMigrationManager.ListContainers(viewModel.PortalId);
    viewModel.Containers = await this.ContainerManager.List();

    foreach (Models.DNN.Container container in viewModel.DNNContainers)
    {
      container.AssignedContainerId = FindBestMatch(container, viewModel.Containers);
    }

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

    viewModel.Users = await this.DNNMigrationManager.ListDnnUsers(portalId);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<Models.DNN.User>(this.HttpContext.RequestServices, viewModel.Users);

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }

    return viewModel;
  }

  private async Task<ViewModels.Notify> BuildNotifyUsersViewModel()
  {
    ViewModels.Notify viewModel = new();
    viewModel.Users = new();

    // we have to call UserManager.Get because .List does not include the secrets property.  We have to loop 
    // instead of using linq because you can't call an async method from within a linq .Where clause.
    foreach (User user in await this.UserManager.List(this.Context.Site))
    {
      if (await IncludeUser(user))
        viewModel.Users.Add(new NotifyUser() { User = user, CanSelect = true, IsSelected = true });
    }

    viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site);

    viewModel.IsMailConfigured = !String.IsNullOrEmpty(this.Context.Site.GetSiteMailSettings().HostName);

    this.DNNMigrationManager.ClearMigrationEngines();
    await this.DNNMigrationManager.CreateMigrationEngine<NotifyUser>(this.HttpContext.RequestServices, viewModel.Users);

    foreach (MigrationEngineBase engine in this.DNNMigrationManager.GetMigrationEngines)
    {
      await engine.Validate();
    }

    return viewModel;
  }

  /// <summary>
  /// Read full user details and return a value to indicate whether the user needs a notification email.
  /// </summary>
  /// <param name="user"></param>
  /// <returns></returns>
  private async Task<Boolean> IncludeUser(User user)
  {
    User fullUser = await this.UserManager.Get(user.Id);
    return (fullUser.Secrets == null || fullUser.Secrets.PasswordHash == null) && !fullUser.Verified;
  }


  private async Task<Guid> FindBestMatch(Models.DNN.Skin skin, IList<Nucleus.Abstractions.Models.LayoutDefinition> layouts)
  {
    Guid bestLayoutId = Guid.Empty;
    double bestLayoutWeight = 0;

    foreach (Nucleus.Abstractions.Models.LayoutDefinition layout in layouts)
    {
      // starting weight is based on similarity of "friendly names"
      double weight = (int)(FindSimilarity(skin.FriendlyName(), layout.FriendlyName) * 10);

      IEnumerable<string> layoutPanes = await this.LayoutManager.ListLayoutPanes(layout);

      // add/subtract from weight based on how many panes they have in common
      foreach (string sourcePane in skin.Panes)
      {
        Boolean found = false;
        foreach (string layoutPane in layoutPanes)
        {
          if (sourcePane.Equals(layoutPane, StringComparison.OrdinalIgnoreCase))
          {
            found = true;
          }
        }

        // if a matching pane was found in the layout, increase the weight - but if the pane is missing from the layout
        // reduce the weight.  This is so that layouts with the least missing panes get a higher weight
        if (found)
        {
          weight++;
        }
        else
        {
          weight--;
        }
      }

      if (weight > bestLayoutWeight)
      {
        bestLayoutId = layout.Id;
        bestLayoutWeight = weight;
      }
    }

    return bestLayoutId;
  }


  private static int GetEditDistance(string X, string Y)
  {
    // https://www.techiedelight.com/calculate-similarity-between-two-strings-in-csharp/#:~:text=We%20can%20use%20the%20Edit,convert%20one%20string%20to%20another.
    int m = X.Length;
    int n = Y.Length;

    int[][] T = new int[m + 1][];
    for (int i = 0; i < m + 1; ++i)
    {
      T[i] = new int[n + 1];
    }

    for (int i = 1; i <= m; i++)
    {
      T[i][0] = i;
    }
    for (int j = 1; j <= n; j++)
    {
      T[0][j] = j;
    }

    int cost;
    for (int i = 1; i <= m; i++)
    {
      for (int j = 1; j <= n; j++)
      {
        cost = X[i - 1] == Y[j - 1] ? 0 : 1;
        T[i][j] = Math.Min(Math.Min(T[i - 1][j] + 1, T[i][j - 1] + 1),
                T[i - 1][j - 1] + cost);
      }
    }

    return T[m][n];
  }

  public static double FindSimilarity(string x, string y)
  {
    if (x == null || y == null)
    {
      throw new ArgumentException("Strings must not be null");
    }

    double maxLength = Math.Max(x.Length, y.Length);
    if (maxLength > 0)
    {
      // optionally ignore case if needed
      return (maxLength - GetEditDistance(x, y)) / maxLength;
    }
    return 1.0;
  }

  private Guid FindBestMatch(Models.DNN.Container dnnContainer, IList<Nucleus.Abstractions.Models.ContainerDefinition> containers)
  {
    Guid bestContainerId = Guid.Empty;
    double bestContainerWeight = 0;

    foreach (Nucleus.Abstractions.Models.ContainerDefinition container in containers)
    {
      // weight is based on similarity of "friendly names"
      double weight = (int)(FindSimilarity(dnnContainer.FriendlyName(), container.FriendlyName) * 10);

      if (weight > bestContainerWeight)
      {
        bestContainerId = container.Id;
        bestContainerWeight = weight;
      }
    }

    return bestContainerId;
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
