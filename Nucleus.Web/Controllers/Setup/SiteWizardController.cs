using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Extensions;
using Nucleus.Web.ViewModels.Setup;
using static Nucleus.Web.ViewModels.Setup.SiteWizard;

namespace Nucleus.Web.Controllers.Setup;

[Area("Setup")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_WIZARD_POLICY)]
public class SiteWizardController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private IHostApplicationLifetime HostApplicationLifetime { get; }

  private IExtensionManager ExtensionManager { get; }
  private ISiteManager SiteManager { get; }
  private IUserManager UserManager { get; }
  private IPreflight PreFlight { get; }
  private IFileSystemManager FileSystemManager { get; }

  private Application Application { get; }
  private ILogger<SiteWizardController> Logger { get; }

  private static readonly Guid HTML_MODULE_PACKAGE_ID = Guid.Parse("4036fdb6-6114-4740-b3c4-d3dbc8f37540");
  private static readonly Guid SMTP_CLIENT_PACKAGE_ID = Guid.Parse("2cefdc41-bb7c-48c7-9dd0-9f1975bcd502");
  private static readonly Guid STANDARD_LAYOUTS_CONTAINERS_PACKAGE_ID = Guid.Parse("c71695ec-111a-4ee6-9ba6-cdce64a6afd5");

  private static readonly Guid[] REQUIRED_EXTENSIONS = [HTML_MODULE_PACKAGE_ID, SMTP_CLIENT_PACKAGE_ID];
  private static readonly Guid[] RECOMMENDED_EXTENSIONS = [STANDARD_LAYOUTS_CONTAINERS_PACKAGE_ID];

  private static readonly FileSystemType LOCAL_FILES = new()
  {
    PackageId = Guid.NewGuid(),  // local file system is in the core, so it will never match an extension package id
    FriendlyName = "Local File System",
    ProviderType = "Nucleus.Core.FileSystemProviders.LocalFileSystemProvider,Nucleus.Core",
    DefaultKey = "local",
    DefaultName = "Local"
  };

  private static readonly FileSystemType AMAZON_S3 = new()
  {
    PackageId = Guid.Parse("28f54611-c14a-4a98-af7c-6a5a3d62b2e0"),
    FriendlyName = "Amazon S3",
    Properties =
      [
        new("Access Key", "AccessKey", ""),
        new("Secret", "Secret", ""),
        new("Service Url", "ServiceUrl", ""),
        new("Root Path", "RootPath", "")
      ],
    ProviderType = "Nucleus.Extensions.AmazonS3FileSystemProvider.FileSystemProvider,Nucleus.Extensions.AmazonS3FileSystemProvider",
    DefaultKey = "amazon",
    DefaultName = "Amazon"
  };

  private static readonly FileSystemType AZURE_STORAGE = new()
  {
    PackageId = Guid.Parse("e27c5782-df19-462f-806c-9b6897dd8ae9"),
    FriendlyName = "Azure Storage",
    Properties = [new("Connection String", "ConnectionString", "")],
    ProviderType = "Nucleus.Extensions.AzureBlobStorageFileSystemProvider.FileSystemProvider,Nucleus.Extensions.AzureBlobStorageFileSystemProvider",
    DefaultKey = "azure",
    DefaultName = "Azure"
  };

  public SiteWizardController(IWebHostEnvironment webHostEnvironment, IHostApplicationLifetime hostApplicationLifetime, Abstractions.Models.Application application, IPreflight preFlight, IExtensionManager extensionManager, ISiteManager siteManager, IFileSystemManager fileSystemManager, IUserManager userManager, ILogger<SiteWizardController> logger)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Logger = logger;
    this.HostApplicationLifetime = hostApplicationLifetime;
    this.Application = application;
    this.PreFlight = preFlight;
    this.ExtensionManager = extensionManager;
    this.FileSystemManager = fileSystemManager;
    this.SiteManager = siteManager;
    this.UserManager = userManager;
  }

  [HttpGet]
  public async Task<IActionResult> Index()
  {
    if (this.Application.IsInstalled && await this.UserManager.CountSystemAdministrators() != 0)
    {
      return BadRequest();
    }

    // pre-flight checks
    IPreflight.ValidationResults results = this.PreFlight.Validate();

    return View("Index", await BuildViewModel(results));
  }

  [HttpPost]
  public async Task<IActionResult> Index(ViewModels.Setup.SiteWizard viewModel)
  {
    if (await this.UserManager.CountSystemAdministrators() != 0)
    {
      return BadRequest();
    }

    // pre-flight checks
    IPreflight.ValidationResults results = this.PreFlight.Validate();

    viewModel.Preflight = results;
    return View("Index", await BuildViewModel(viewModel, ReadFlags.All));
  }

  [HttpPost]
  public async Task<IActionResult> Select(ViewModels.Setup.SiteWizard viewModel)
  {
    if (viewModel.DatabaseProvider == "Sqlite")
    {
      viewModel.DatabaseConnectionString = CreateConnectionString(viewModel, false);
    }
    else
    {
      viewModel.DatabaseConnectionString = "";
    }

    ModelState.Clear();
    return View("_Database", await BuildViewModel(viewModel, ReadFlags.DatabaseProviders));
  }

  [HttpPost]
  public async Task<IActionResult> RefreshDatabases(ViewModels.Setup.SiteWizard viewModel)
  {
    if (String.IsNullOrEmpty(viewModel.DatabaseServer))
    {
      Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = new();
      modelState.AddModelError(nameof(ViewModels.Setup.SiteWizard.DatabaseServer), "Please enter your server name.");

      return BadRequest(modelState);
    }

    viewModel.Databases = ListDatabases(viewModel);
    return View("_Database", await BuildViewModel(viewModel, ReadFlags.DatabaseProviders));
  }

  [HttpPost]
  public async Task<IActionResult> ToggleAuthentication(ViewModels.Setup.SiteWizard viewModel)
  {
    return View("_Database", await BuildViewModel(viewModel, ReadFlags.DatabaseProviders));
  }

  [HttpPost]
  public async Task<IActionResult> GenerateConnectionString(ViewModels.Setup.SiteWizard viewModel)
  {
    if (!String.IsNullOrEmpty(viewModel.DatabaseServer))
    {
      viewModel.Databases = ListDatabases(viewModel);
    }

    if (viewModel.DatabaseProvider == "Sqlite" || (!String.IsNullOrEmpty(viewModel.DatabaseName) && viewModel.DatabaseName != ViewModels.Setup.SiteWizard.REFRESH_DATABASES))
    {
      viewModel.DatabaseConnectionString = CreateConnectionString(viewModel, false);
    }
    else
    {
      viewModel.DatabaseConnectionString = "";
    }

    ModelState.Clear();
    return View("_Database", await BuildViewModel(viewModel, ReadFlags.DatabaseProviders));
  }

  [HttpPost]
  public async Task<IActionResult> SaveDatabaseSettings(ViewModels.Setup.SiteWizard viewModel)
  {
    if (String.IsNullOrEmpty(viewModel.DatabaseConnectionString))
    {
      Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = new();
      modelState.AddModelError(nameof(ViewModels.Setup.SiteWizard.DatabaseConnectionString), "Connection string is required.");
      return BadRequest(modelState);
    }

    // test the database connection by using the full connection string to list databases.  We don't test the connection for 
    // Sqlite because it is a local file.  File permissions are tested by the "preflight" checks in the next step.
    if (viewModel.DatabaseProvider != "Sqlite")
    {
      CreateProvider(viewModel.DatabaseProvider).TestConnection(viewModel.DatabaseConnectionString);
    }

    // write database configuration to the databaseSettings.[environment].json file
    Extensions.Configuration.ConfigurationFile config = new(Extensions.Configuration.ConfigurationFile.KnownConfigurationFiles.databaseSettings, this.WebHostEnvironment.EnvironmentName);

    Newtonsoft.Json.Linq.JArray schemas = config.GetArray("Nucleus", "Database", "Schemas");

    Newtonsoft.Json.Linq.JObject schema = config.GetObject(schemas, "Name", "*");
    config.Set(schema, "Name", "*");
    config.Set(schema, "ConnectionKey", this.WebHostEnvironment.EnvironmentName);

    Newtonsoft.Json.Linq.JArray connections = config.GetArray("Nucleus", "Database", "Connections");

    Newtonsoft.Json.Linq.JObject connection = config.GetObject(connections, "Key", this.WebHostEnvironment.EnvironmentName);

    config.Set(connection, "Key", this.WebHostEnvironment.EnvironmentName);
    config.Set(connection, "Type", viewModel.DatabaseProvider);
    config.Set(connection, "ConnectionString", viewModel.DatabaseConnectionString);

    // prepare the output viewmodel before saving changes because in IIS, the update will trigger a restart
    viewModel = await BuildViewModel(viewModel, ReadFlags.General);

    // save changes
    config.CommitChanges();

    // Restart. IIS and nginx will automatically restart Nucleus after the app stops.
    this.HostApplicationLifetime.StopApplication();

    return View("Restarting", viewModel);
  }

  [HttpPost]
  public async Task<IActionResult> AddFileSystem(ViewModels.Setup.SiteWizard viewModel)
  {
    if (viewModel.AddFileSystemType == null)
    {
      return BadRequest("Please select a file system type.");
    }

    if (viewModel.AddFileSystemType?.ProviderType == LOCAL_FILES.ProviderType && viewModel.SelectedFileSystems.Where(fileSystem => fileSystem.FileSystemType.ProviderType == LOCAL_FILES.ProviderType).Any())
    {
      return Json(new { Title = "Add File System", Message = "A local file system is already selected.  You can only add one local file system.", icon = "alert" });
    }

    // repopulate available file system values, which are not deserialized by MVC
    AddAvailableFileSystems(viewModel);
    viewModel.AddFileSystemType = viewModel.AvailableFileSystemTypes
      .Where(item => item.ProviderType == viewModel.AddFileSystemType.ProviderType)
      .FirstOrDefault();

    if (viewModel.AddFileSystemType == null)
    {
      return BadRequest("Invalid file system type.");
    }

    // add a new file system
    int existingCount = viewModel.SelectedFileSystems.Where(fileSystem => !fileSystem.IsRemoved && fileSystem.FileSystemType.ProviderType == viewModel.AddFileSystemType.ProviderType).Count();

    SelectedFileSystem newFileSystem = new()
    {
      FileSystemType = viewModel.AddFileSystemType,
      Key = viewModel.AddFileSystemType.DefaultKey + (existingCount == 0 ? "" : existingCount.ToString()),
      Name = viewModel.AddFileSystemType.DefaultName + (existingCount == 0 ? "" : existingCount.ToString()),
    };
    foreach (FileSystemProperty prop in newFileSystem.FileSystemType.Properties)
    {
      newFileSystem.Values.Add(prop);
    }

    viewModel.SelectedFileSystems.Add(newFileSystem);

    viewModel.AddFileSystemType = null;
    viewModel.ScrollTo = $"#{newFileSystem.Key}";
    return View("_FileSystem", await BuildViewModel(viewModel, ReadFlags.FileSystems));
  }

  [HttpPost]
  public async Task<IActionResult> RemoveFileSystem(ViewModels.Setup.SiteWizard viewModel, string key)
  {
    // mark file system as removed
    SelectedFileSystem systemToRemove = viewModel.SelectedFileSystems.Where(fileSystem => fileSystem.Key == key).FirstOrDefault();
    if (systemToRemove == null)
    {
      return BadRequest();
    }
    systemToRemove.IsRemoved = true;

    return View("_FileSystem", await BuildViewModel(viewModel, ReadFlags.FileSystems));
  }

  [HttpPost]
  public async Task<IActionResult> RefreshExtensions(ViewModels.Setup.SiteWizard viewModel)
  {
    ModelState.Clear();
    return View("_Extensions", await BuildViewModel(viewModel, ReadFlags.Extensions));
    
  }

  [HttpPost]
  public async Task<IActionResult> Install(ViewModels.Setup.SiteWizard viewModel)
  {
    if (await this.UserManager.CountSystemAdministrators() != 0)
    {
      return BadRequest();
    }

    if (!String.IsNullOrEmpty(viewModel.SiteAdminUserName) && viewModel.SiteAdminUserName == viewModel.SystemAdminUserName)
    {
      ModelState.AddModelError(nameof(ViewModels.Setup.SiteWizard.SiteAdminUserName), "Please enter different user names for the system admin and site admin users.");
    }

    if (viewModel.SelectedFileSystems?.Any() != true)
    {      
      ModelState.AddModelError($"{nameof(ViewModels.Setup.SiteWizard.AddFileSystemType)}.{nameof(ViewModels.Setup.SiteWizard.AddFileSystemType.ProviderType)}", "Please select at least one file system provider.");
    }

    if (ModelState.IsValid)
    {
      if (await this.UserManager.CountSystemAdministrators() == 0)
      {
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.SystemAdminPassword), viewModel.SystemAdminPassword);
        if (!modelState.IsValid)
        {
          return BadRequest(modelState);
        }
      }

      {
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.SiteAdminPassword), viewModel.SiteAdminPassword);
        if (!modelState.IsValid)
        {
          return BadRequest(modelState);
        }
      }

      {
        // Validate the site home directory.  
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = viewModel.Site.ValidateHomeDirectory($"{nameof(viewModel.Site)}.{nameof(viewModel.Site.HomeDirectory)}");
        if (!modelState.IsValid)
        {
          return BadRequest(modelState);
        }
      }

      // Build the site
      await BuildSite(viewModel);

      // write a file to record installation information
      this.Application.SetInstalled();

      // Wait 3 seconds after returning and restart
      Task restartTask = Task.Run(async () =>
      {
        await Task.Delay(3000);
        this.HostApplicationLifetime.StopApplication();
      });

      return View("complete", new ViewModels.Setup.SiteWizardComplete() { SiteUrl = Url.Content(Request.Scheme + System.Uri.SchemeDelimiter + viewModel.Site.Aliases.First().Alias) });
    }
    else
    {
      return BadRequest(ModelState);
    }
  }

  private async Task BuildSite(ViewModels.Setup.SiteWizard viewModel)
  {
    // save file system config
    SaveFileSystemSettings(viewModel);

    // install extensions
    foreach (ViewModels.Setup.SiteWizard.InstallableExtension selectedExtension in viewModel.InstallableExtensions.Where(ext => ext.IsSelected))
    {
      using (System.IO.FileStream extensionStream = System.IO.File.OpenRead(System.IO.Path.Combine(InstallableExtensionsFolder().FullName, selectedExtension.Filename)))
      {
        await this.ExtensionManager.InstallExtension(await this.ExtensionManager.SaveTempFile(extensionStream));
      }
    }

    // create site
    Nucleus.Abstractions.Models.Export.SiteTemplate template = await this.SiteManager.ReadTemplateTempFile(viewModel.TemplateTempFileName);

    template.Site.Name = viewModel.Site.Name;

    viewModel.Site.DefaultSiteAlias.Id = Guid.NewGuid();

    if (viewModel.Site.DefaultSiteAlias.Alias.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
      viewModel.Site.DefaultSiteAlias.Alias = viewModel.Site.DefaultSiteAlias.Alias.Substring("http://".Length);
    }
    else if (viewModel.Site.DefaultSiteAlias.Alias.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
      viewModel.Site.DefaultSiteAlias.Alias = viewModel.Site.DefaultSiteAlias.Alias.Substring("https://".Length);
    }

    template.Site.Aliases = [viewModel.Site.DefaultSiteAlias];
    template.Site.DefaultSiteAlias = viewModel.Site.DefaultSiteAlias;

    template.Site.AdministratorsRole = viewModel.Site.AdministratorsRole;
    template.Site.AllUsersRole = viewModel.Site.AllUsersRole;
    template.Site.AnonymousUsersRole = viewModel.Site.AnonymousUsersRole;
    template.Site.RegisteredUsersRole = viewModel.Site.RegisteredUsersRole;

    //template.Site.DefaultContainerDefinition = viewModel.Site.DefaultContainerDefinition;
    //template.Site.DefaultLayoutDefinition = viewModel.Site.DefaultLayoutDefinition;
    template.Site.HomeDirectory = viewModel.Site.HomeDirectory;

    viewModel.Site = await this.SiteManager.Import(template);

    // create users

    // only create a system admin user if there isn't already one in the database
    if (await this.UserManager.CountSystemAdministrators() == 0)
    {
      Abstractions.Models.User sysAdminUser = new()
      {
        UserName = viewModel.SystemAdminUserName,
        IsSystemAdministrator = true,
        Secrets = new()
      };

      sysAdminUser.Secrets.SetPassword(viewModel.SystemAdminPassword);
      sysAdminUser.Approved = true;
      sysAdminUser.Verified = true;

      await this.UserManager.SaveSystemAdministrator(sysAdminUser);
    }

    // create site admin user
    User siteAdminUser = new()
    {
      UserName = viewModel.SiteAdminUserName,
      Secrets = new(),
      Roles = [viewModel.Site.AdministratorsRole]
    };

    siteAdminUser.Secrets.SetPassword(viewModel.SiteAdminPassword);
    siteAdminUser.Approved = true;
    siteAdminUser.Verified = true;

    await this.UserManager.Save(viewModel.Site, siteAdminUser);
  }

  public void SaveFileSystemSettings(ViewModels.Setup.SiteWizard viewModel)
  {
    // write database configuration to the databaseSettings.[environment].json file
    Extensions.Configuration.ConfigurationFile config = new(Extensions.Configuration.ConfigurationFile.KnownConfigurationFiles.appSettings, this.WebHostEnvironment.EnvironmentName);

    Newtonsoft.Json.Linq.JArray fileSystems = config.GetArray("Nucleus", "FileSystems", "Providers");

    foreach (var selectedFileSystem in viewModel.SelectedFileSystems)
    {
      if (selectedFileSystem.IsRemoved)
      {
        Newtonsoft.Json.Linq.JObject fileSystemToDelete = config.RemoveObject(fileSystems, "Key", selectedFileSystem.Key);
      }
      else
      {
        // create or replace
        Newtonsoft.Json.Linq.JObject fileSystemToCreate = config.GetObject(fileSystems, "Key", selectedFileSystem.Key);
        config.Set(fileSystemToCreate, "Key", selectedFileSystem.Key);
        config.Set(fileSystemToCreate, "Name", selectedFileSystem.Name);
        config.Set(fileSystemToCreate, "ProviderType", selectedFileSystem.FileSystemType.ProviderType);

        foreach (FileSystemProperty property in selectedFileSystem.Values)
        {
          config.Set(fileSystemToCreate, property.Key, property.Value);
        }
      }
    }

    config.CommitChanges();
  }

  [Flags]
  private enum ReadFlags
  {
    General,
    Templates,
    DatabaseProviders,
    FileSystems,
    Extensions,
    All = -1
  }

  private async Task<ViewModels.Setup.SiteWizard> BuildViewModel(ViewModels.Setup.SiteWizard viewModel, ReadFlags flags)
  {
    List<ModuleDefinition> modulesInTemplate = [];
    List<LayoutDefinition> layoutsInTemplate = [];
    List<ContainerDefinition> containersInTemplate = [];

    List<ViewModels.Setup.SiteWizard.InstallableExtension> installableExtensions = [];

    List<string> otherWarnings = [];
    List<string> missingExtensionWarnings = [];

    if (flags.HasFlag(ReadFlags.Extensions))
    {
      // reading extensions requires processing the template
      flags = flags | ReadFlags.Templates;
    }

    viewModel.Url = $"{(String.IsNullOrEmpty(HttpContext.Request.PathBase) ? "" : HttpContext.Request.PathBase + "/")}";

    if (flags.HasFlag(ReadFlags.DatabaseProviders))
    {
      // database settings
      Extensions.Configuration.ConfigurationFile config = new(Extensions.Configuration.ConfigurationFile.KnownConfigurationFiles.databaseSettings, this.WebHostEnvironment.EnvironmentName);
      Newtonsoft.Json.Linq.JToken schema = config.GetToken("Nucleus.Database.Schemas[?(@.Name == '*')]");
      viewModel.IsDatabaseConfigured = (schema != null);

      if (!viewModel.IsDatabaseConfigured)
      {
        viewModel.DatabaseProviders = System.Runtime.Loader.AssemblyLoadContext.All
        .SelectMany(context => context.Assemblies)
        .SelectMany(assm => GetTypes(assm)
          .Where(type => typeof(IDatabaseProvider).IsAssignableFrom(type) && !type.Equals(typeof(IDatabaseProvider))))
        .Select(type => Activator.CreateInstance(type) as IDatabaseProvider)
        .Select(instance => instance.TypeKey());
      }
    }

    // we always need the list of available file systems, regardless of the flags setting
    AddAvailableFileSystems(viewModel);

    if (flags.HasFlag(ReadFlags.FileSystems))
    {
      // file systems
      if (viewModel.SelectedFileSystems?.Any() != true)
      {
        ReadConfiguredFileSystems(viewModel);
      }

      if (flags.HasFlag(ReadFlags.All) && viewModel.SelectedFileSystems?.Any() != true)
      {
        // if there are no file systems selected and ReadFlags.All (meaning that this is the first call to BuildViewModel), add a default
        // file system.

        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
        {
          // if we are running in Azure, add a default Azure blob storage provider
          viewModel.SelectedFileSystems.Add(new()
          {
            FileSystemType = AZURE_STORAGE,
            Key = AZURE_STORAGE.DefaultKey,
            Name = AZURE_STORAGE.DefaultName,
            Values = AZURE_STORAGE.Properties
          });
        }
        else
        {
          // otherwise, add a default local file system provider
          viewModel.SelectedFileSystems.Add(new()
          {
            FileSystemType = LOCAL_FILES,
            Key = LOCAL_FILES.DefaultKey,
            Name = LOCAL_FILES.DefaultName,
            Values = LOCAL_FILES.Properties
          });
        }
      }
    }

    // re-populate selected file system values.  We always do this, regardless of the flags setting because when we
    // need the values when determining whether file system extensions need to be set to IsRequired (below)
    foreach (SelectedFileSystem selectedFileSystem in viewModel.SelectedFileSystems)
    {
      selectedFileSystem.FileSystemType = viewModel.AvailableFileSystemTypes
        .Where(item => item.ProviderType == selectedFileSystem.FileSystemType.ProviderType)
        .FirstOrDefault();
    }

    viewModel.CreateSystemAdministratorUser = await this.UserManager.CountSystemAdministrators() == 0;

    if (flags.HasFlag(ReadFlags.Templates))
    {
      viewModel.Templates = [];
      foreach (FileInfo templateFile in TemplatesFolder().EnumerateFiles("*.xml").OrderBy(file => file.Name).ToList())
      {
        using (System.IO.Stream stream = templateFile.OpenRead())
        {
          Nucleus.Abstractions.Models.Export.SiteTemplate template = await this.SiteManager.ParseTemplate(stream);

          if (String.IsNullOrEmpty(template.Name))
          {
            template.Name = System.IO.Path.GetFileNameWithoutExtension(templateFile.Name).Replace('-', ' ');
          }

          SiteWizard.SiteTemplate templateItem = new(template.Name, template.Description, templateFile.Name);

          // templates are sorted by file name, but the default template is always first
          if (templateFile.Name.Equals("default-site-template.xml", StringComparison.Ordinal))
          {
            viewModel.Templates.Insert(0, templateItem);
          }
          else
          {
            viewModel.Templates.Add(templateItem);
          }
        }
      }

      if (String.IsNullOrEmpty(viewModel.SelectedTemplate) && viewModel.Templates.Count == 1)
      {
        viewModel.SelectedTemplate = viewModel.Templates[0].FileName;
      }

      if (!String.IsNullOrEmpty(viewModel.SelectedTemplate))
      {
        using (Stream templateFile = System.IO.File.OpenRead(System.IO.Path.Combine(TemplatesFolder().FullName, viewModel.SelectedTemplate)))
        {
          Nucleus.Abstractions.Models.Export.SiteTemplate template = await this.SiteManager.ParseTemplate(templateFile);

          viewModel.Site = template.Site;

          if (viewModel.Site != null)
          {
            viewModel.Site.DefaultSiteAlias = new SiteAlias() { Alias = $"{ControllerContext.HttpContext.Request.Host}{ControllerContext.HttpContext.Request.PathBase}" };
            if (ControllerContext.HttpContext.Request.Host.Port.HasValue && !viewModel.Site.DefaultSiteAlias.Alias.Contains(':'))
            {
              viewModel.Site.DefaultSiteAlias.Alias += $":{ControllerContext.HttpContext.Request.Host.Port}";
            }
          }

          modulesInTemplate = template.Pages
            .SelectMany(page => page.Modules)
            .Select(module => module.ModuleDefinition)
            .Distinct()
            .ToList();

          layoutsInTemplate = template.Pages
            .Where(page => page.LayoutDefinition != null)
            .Select(page => page.LayoutDefinition)
            .Distinct()
            .ToList();

          if (template.Site.DefaultLayoutDefinition != null)
          {
            layoutsInTemplate = layoutsInTemplate.Concat
            (
              [template.Site.DefaultLayoutDefinition]
            )
            .ToList();
          }

          containersInTemplate =
            (
              template.Pages
              .Where(page => page.DefaultContainerDefinition != null)
              .Select(page => page.DefaultContainerDefinition)
              .Distinct()
            ).Concat
            (
              template.Pages
                .SelectMany(page => page.Modules)
                .Where(module => module.ContainerDefinition != null)
                .Select(module => module.ContainerDefinition)
                .Distinct()
            )
            .ToList();

          // save parsed template (with Guids generated) so that the Guids stay the same when we build the site
          viewModel.TemplateTempFileName = await this.SiteManager.SaveTemplateTempFile(template);
        }

        foreach (FileInfo extensionPackageFile in InstallableExtensionsFolder().EnumerateFiles("*.zip"))
        {
          using (Stream extensionStream = extensionPackageFile.OpenRead())
          {
            this.Logger?.LogInformation("Validating '{fileName}'.", extensionPackageFile.FullName);

            try
            {
              Nucleus.Abstractions.Models.Extensions.PackageResult extensionResult = await this.ExtensionManager.ValidatePackage(extensionStream);
              if (extensionResult.IsValid)
              {
                ViewModels.Setup.SiteWizard.InstallableExtension installableExtension = new(extensionPackageFile.Name, extensionResult);

                // check for duplicate extensions (different versions in /setup/extensions)
                ViewModels.Setup.SiteWizard.InstallableExtension existing = installableExtensions.Where(extension => extension.PackageId == installableExtension.PackageId).FirstOrDefault();
                if (existing != null)
                {
                  if (existing.PackageVersion > installableExtension.PackageVersion)
                  {
                    // this extension has a later version, replace
                    installableExtensions.Remove(existing);
                  }
                  else
                  {
                    // this extension does not have a later version, skip
                    break;
                  }
                }

                installableExtension.ModulesInPackage = extensionResult.Package.components
                  .SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.ModuleDefinition>())
                  .Select(moduleDefinition => Guid.Parse(moduleDefinition.id))
                  .Distinct();

                installableExtension.LayoutsInPackage = extensionResult.Package.components
                  .SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.LayoutDefinition>())
                  .Select(layoutDefinition => Guid.Parse(layoutDefinition.id))
                  .Distinct();

                installableExtension.ContainersInPackage = extensionResult.Package.components
                  .SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.ContainerDefinition>())
                  .Select(containerDefinition => Guid.Parse(containerDefinition.id))
                  .Distinct();

                if
                  (
                    REQUIRED_EXTENSIONS.Contains(installableExtension.PackageId) ||
                    (modulesInTemplate != null && modulesInTemplate.Where(moduleDef => installableExtension.ModulesInPackage.Contains(moduleDef.Id)).Any()) ||
                    (layoutsInTemplate != null && layoutsInTemplate.Where(layoutDef => installableExtension.LayoutsInPackage.Contains(layoutDef.Id)).Any()) ||
                    (containersInTemplate != null && containersInTemplate.Where(containerDef => installableExtension.ContainersInPackage.Contains(containerDef.Id)).Any())
                  )
                {
                  // extension is a required component, or the selected template contains one or more modules/layouts/containers that are in this
                  // extension (so the extension is required by the template)
                  installableExtension.IsSelected = true;
                  installableExtension.IsRequired = true;
                }

                // only apply default extension selections once (that is, not from the RefreshExtensions action)
                if (flags.HasFlag(ReadFlags.All))
                {
                  if (RECOMMENDED_EXTENSIONS.Contains(installableExtension.PackageId))
                  {
                    installableExtension.IsSelected = true;
                  }
                }
                else
                {
                  // retain user selection
                  InstallableExtension existingExtensionSelection = viewModel.InstallableExtensions.Where(extension => extension.PackageId == installableExtension.PackageId).FirstOrDefault();
                  if (existingExtensionSelection != null)
                  {
                    installableExtension.IsSelected = existingExtensionSelection.IsSelected || installableExtension.IsRequired;
                  }
                }

                installableExtensions.Add(installableExtension);
              }
            }
            catch (Exception ex)
            {
              otherWarnings.Add($"Invalid extension: {extensionPackageFile.Name}: {ex.Message}");
            }
          }
        }
      }

      if (flags.HasFlag(ReadFlags.Extensions))
      {
        // mark file system providers as selected and required if they match selected file systems
        foreach (SelectedFileSystem fileSystem in viewModel.SelectedFileSystems)
        {
          InstallableExtension fileSystemExtension = installableExtensions.Where(extension => extension.PackageId == fileSystem.FileSystemType.PackageId).FirstOrDefault();

          if (fileSystemExtension != null)
          {
            if (fileSystem.IsRemoved)
            {
              // for removed file systems, if there are no other file systems which use the file system provider, mark it as un-selected
              if (!viewModel.SelectedFileSystems.ToList().Where(selected => selected.Key != fileSystem.Key && !selected.IsRemoved && selected.FileSystemType.PackageId == fileSystem.FileSystemType.PackageId).Any())
              {
                fileSystemExtension.IsSelected = false;
                fileSystemExtension.IsRequired = false;
              }
            }
            else
            {
              fileSystemExtension.IsSelected = true;
              fileSystemExtension.IsRequired = true;
            }
          }
        }
      }

      if (flags.HasFlag(ReadFlags.Extensions))
      {
        // check for missing modules					
        foreach (ModuleDefinition moduleDefinition in modulesInTemplate)
        {
          if (!installableExtensions.Where(installableExtension => installableExtension.ModulesInPackage.Contains(moduleDefinition.Id)).Any())
          {
            // module is missing
            missingExtensionWarnings.Add($"Module '{moduleDefinition.FriendlyName}'.");
          }
        }

        // check for missing layouts					
        foreach (LayoutDefinition layoutDefinition in layoutsInTemplate)
        {
          if (!installableExtensions.Where(installableExtension => installableExtension.LayoutsInPackage.Contains(layoutDefinition.Id)).Any())
          {
            // layout is missing
            missingExtensionWarnings.Add($"Layout '{layoutDefinition.FriendlyName}'.");
          }
        }

        // check for missing containers					
        foreach (ContainerDefinition containerDefinition in containersInTemplate)
        {
          if (!installableExtensions.Where(installableExtension => installableExtension.ContainersInPackage.Contains(containerDefinition.Id)).Any())
          {
            // container is missing
            missingExtensionWarnings.Add($"Container '{containerDefinition.FriendlyName}'.");
          }
        }
      }

      viewModel.MissingExtensionWarnings = missingExtensionWarnings.Distinct();
      viewModel.OtherWarnings = otherWarnings.Distinct();

      viewModel.InstallableExtensions = installableExtensions.OrderBy(ext => ext.Name).ToList();
    }

    return viewModel;
  }

  private async Task<ViewModels.Setup.SiteWizard> BuildViewModel()
  {
    ViewModels.Setup.SiteWizard viewModel = await BuildViewModel(new ViewModels.Setup.SiteWizard(), ReadFlags.All);

    return viewModel;
  }

  private async Task<ViewModels.Setup.SiteWizard> BuildViewModel(IPreflight.ValidationResults results)
  {
    ViewModels.Setup.SiteWizard viewModel;

    if (results.IsValid())
    {
      viewModel = await BuildViewModel();
    }
    else
    {
      viewModel = new();
    }

    viewModel.Preflight = results;

    return viewModel;
  }

  private void ReadConfiguredFileSystems(ViewModels.Setup.SiteWizard viewModel)
  {
    foreach (Abstractions.FileSystemProviders.FileSystemProviderInfo provider in this.FileSystemManager.ListProviders())
    {
      SelectedFileSystem selectedFileSystem = new()
      {
        Name = provider.Name,
        Key = provider.Key,
        FileSystemType = viewModel.AvailableFileSystemTypes.Where(item => item.ProviderType.Equals(provider.ProviderType, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()
      };

      // skip invalid entries
      if (selectedFileSystem.FileSystemType == null) break;

      // get property values  
      foreach (FileSystemProperty prop in selectedFileSystem.FileSystemType.Properties)
      {
        Extensions.Configuration.ConfigurationFile config = new(Extensions.Configuration.ConfigurationFile.KnownConfigurationFiles.appSettings, this.WebHostEnvironment.EnvironmentName);
        Newtonsoft.Json.Linq.JToken value = config.GetToken($"Nucleus.FileSystems.Providers[?(@.Key == '{provider.Key}')]");
        selectedFileSystem.Values.Add(new()
        {
          FriendlyName = prop.FriendlyName,
          Key = prop.Key,
          Value = value?.Value<string>(prop.Key)
        });
      }

      viewModel.SelectedFileSystems.Add(selectedFileSystem);
    }
  }


  /// <summary>
  /// add available file system types.  We have to hard-code these, because the file system provider(s) won't be installed yet, so we can't use reflection
  /// to find them.
  /// </summary>
  /// <param name="viewModel"></param>
  private static void AddAvailableFileSystems(ViewModels.Setup.SiteWizard viewModel)
  {
    viewModel.AvailableFileSystemTypes.Clear();

    viewModel.AvailableFileSystemTypes.Add(LOCAL_FILES);
    viewModel.AvailableFileSystemTypes.Add(AZURE_STORAGE);
    viewModel.AvailableFileSystemTypes.Add(AMAZON_S3);
  }

  private DirectoryInfo TemplatesFolder()
  {
    string path = Path.Combine(Path.Combine(System.IO.Path.Combine(this.WebHostEnvironment.ContentRootPath, "Setup"), "Templates"), "Site");
    return new DirectoryInfo(path);
  }

  private DirectoryInfo InstallableExtensionsFolder()
  {
    string path = Path.Combine(System.IO.Path.Combine(this.WebHostEnvironment.ContentRootPath, "Setup"), "Extensions");
    DirectoryInfo result = new(path);
    if (!result.Exists)
    {
      result.Create();
    }
    return result;
  }

  private static IEnumerable<string> ListDatabases(ViewModels.Setup.SiteWizard viewModel)
  {
    IDatabaseProvider provider = CreateProvider(viewModel.DatabaseProvider);
    return provider.ListDatabases(CreateConnectionString(viewModel, true)).OrderBy(name => name);
  }

  private static string CreateConnectionString(ViewModels.Setup.SiteWizard viewModel, Boolean skipDatabaseName)
  {
    string database = "";
    string identity = "";

    // special case for Sqlite, which doesn't need or support integrated security, authentication, server name
    if (viewModel.DatabaseProvider == "Sqlite")
    {
      return "Data Source={DataFolder}/Data/Nucleus.db";
    }

    if (viewModel.DatabaseUseIntegratedSecurity)
    {
      switch (viewModel.DatabaseProvider)
      {
        case "SqlServer":
          identity = "Integrated Security=true;";
          break;

        case "ProgresSql":
          identity = "Integrated Security=True;";
          break;

        case "MySql":
          identity = "Integrated Security=yes;";
          break;

        case "Sqlite":
          // Sqlite does not support trusted connections (or username/password either), so the trusted connection selection has no meaning
          identity = "";
          break;
      }
    }
    else
    {
      switch (viewModel.DatabaseProvider)
      {
        case "Sqlite":
          // Sqlite does not support trusted connections (or username/password either), so the trusted connection selection has no meaning
          identity = "";
          break;

        case "MySql":
          identity = $"uid={viewModel.DatabaseUserName};pwd={viewModel.DatabasePassword};";
          break;

        default:
          identity = $"User Id={viewModel.DatabaseUserName};Password={viewModel.DatabasePassword};";
          break;
      }
    }

    if (!skipDatabaseName && !String.IsNullOrEmpty(viewModel.DatabaseName))
    {
      database = $"database={viewModel.DatabaseName};";
    }

    return $"server={viewModel.DatabaseServer};{database}{identity}";
  }

  private static IDatabaseProvider CreateProvider(string providerType)
  {
    return System.Runtime.Loader.AssemblyLoadContext.All
      .SelectMany(context => context.Assemblies)
      .SelectMany(assm => GetTypes(assm)
        .Where(type => typeof(IDatabaseProvider).IsAssignableFrom(type) && !type.Equals(typeof(IDatabaseProvider))))
      .Select(type => Activator.CreateInstance(type) as IDatabaseProvider)
      .Where(instance => instance.TypeKey().Equals(providerType, StringComparison.OrdinalIgnoreCase))
      .FirstOrDefault();
  }

  private static Type[] GetTypes(System.Reflection.Assembly assembly)
  {
    try
    {
      return assembly.GetExportedTypes();
    }
    catch (System.Reflection.ReflectionTypeLoadException)
    {
      return [];
    }
  }
}
