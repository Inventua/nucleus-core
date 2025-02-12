﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Extensions;
using Nucleus.Extensions.Excel;
using Nucleus.Extensions.Logging;

namespace Nucleus.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
public class SystemController : Controller
{
  private string LogFolderPath { get; }
  private RunningTaskQueue RunningTaskQueue { get; }
  private IConfiguration Configuration { get; }
  private ILogger<SystemController> Logger { get; }
  private IOptions<DatabaseOptions> DatabaseOptions { get; }
  private ISessionManager SessionManager { get; }
  private Context Context { get; }
  private ICacheManager CacheManager { get; }
  private IWebHostEnvironment HostingEnvironment { get; }
  private IHostApplicationLifetime HostApplicationLifetime { get; }
  private IResourceMonitor Monitor { get; }

  private const string LINUX_OS_INFO_FILE = "/etc/os-release";


  public SystemController(IHostApplicationLifetime hostApplicationLifetime, IWebHostEnvironment hostingEnvironment, IResourceMonitor monitor, Context context, RunningTaskQueue runningTaskQueue, ICacheManager cacheManager, ILogger<SystemController> logger, IOptions<DatabaseOptions> databaseOptions, IOptions<TextFileLoggerOptions> options, IConfiguration configuration, ISessionManager sessionManager)
  {
    this.HostApplicationLifetime = hostApplicationLifetime;
    this.HostingEnvironment = hostingEnvironment;
    this.Monitor = monitor;
    this.Context = context;
    this.RunningTaskQueue = runningTaskQueue;
    this.CacheManager = cacheManager;
    this.Logger = logger;
    this.DatabaseOptions = databaseOptions;
    this.Configuration = configuration;
    this.SessionManager = sessionManager;

    if (options.Value != null)
    {
      this.LogFolderPath = options.Value.Path;
    }
  }

  /// <summary>
  /// Display System information
  /// </summary>
  /// <returns></returns>
  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> Index(ViewModels.Admin.SystemIndex viewModelInput)
  {
    return View("Index", await BuildViewModel(viewModelInput));
  }

  /// <summary>
  /// Shudown Nucleus/restart.
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  public ActionResult Restart()
  {
    this.HostApplicationLifetime.StopApplication();
    return View("_Restarting", new ViewModels.Admin.SystemIndexRestartComplete() { SiteUrl = this.Context.Site.AbsoluteUrl("/", Request.Scheme == "https") });
  }

  /// <summary>
  /// Clear cache.
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  public async Task<ActionResult> ClearCache(ViewModels.Admin.SystemIndex viewModel, string cache)
  {
    if (!String.IsNullOrEmpty(cache))
    {
      this.CacheManager.Clear(cache);
    }
    return View("Index", await BuildViewModel(viewModel));
  }

  /// <summary>
  /// Clear cache.
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  public async Task<ActionResult> ClearAllCaches(ViewModels.Admin.SystemIndex viewModel)
  {
    this.CacheManager.ClearAll();
    return View("Index", await BuildViewModel(viewModel));
  }


  private string FormatUptime(TimeSpan value)
  {
    string result = "";

    if (value.Days > 0)
    {
      result += $"{value.Days} days, ";
    }

    if (value.Days > 0 || value.Hours > 0)
    {
      result += $"{value.Hours} hours, ";
    }

    if (value.Days > 0 || value.Hours > 0 || value.Minutes > 0)
    {
      result += $"{value.Minutes} minutes, ";
    }

    result += $"{value.Seconds} seconds.";

    return result;
  }

  private void ReadLogFile(Nucleus.Abstractions.Models.Paging.PagingSettings settings, ViewModels.Admin.SystemIndex.LogSettingsViewModel viewModel)
  {
    List<ViewModels.Admin.SystemIndex.LogEntry> results = ReadLogFile(viewModel.LogFile)
      .Where(log => log.IsValid && log.IsMatch(viewModel))
      .ToList();

    viewModel.LogContent = new(settings);
    viewModel.LogContent.TotalCount = results.Count;

    if (viewModel.LogSortDescending)
    {
      results.Reverse();
    }

    viewModel.LogContent.Items = results
      .Skip(viewModel.LogContent.FirstRowIndex)
      .Take(viewModel.LogContent.PageSize)
      .ToList();

  }

  private List<ViewModels.Admin.SystemIndex.LogEntry> ReadLogFile(string filename)
  {
    List<ViewModels.Admin.SystemIndex.LogEntry> results = new();

    if (!String.IsNullOrEmpty(filename))
    {
      string logFilePath = System.IO.Path.Combine(this.LogFolderPath, filename);
      if (System.IO.File.Exists(logFilePath))
      {
        foreach (string line in System.IO.File.ReadAllLines(logFilePath))
        {
          results.Add(new ViewModels.Admin.SystemIndex.LogEntry(line));
        }
      }
    }

    return results;
  }


  [HttpPost]
  public ActionResult GetLogFile(ViewModels.Admin.SystemIndex.LogSettingsViewModel viewModel)
  {
    ReadLogFile(viewModel.LogContent, viewModel);

    return View("_Log", viewModel);
  }

  [HttpPost]
  public async Task<ActionResult> UpdateLogLevel(ViewModels.Admin.SystemIndex viewModel)
  {
    JObject appSettings = LoadConfiguration();

    if (appSettings != null)
    {

      if (!String.IsNullOrEmpty(viewModel.NewSetting?.Category))
      {
        // validate that the entered 'name' is a valid class name or namespace
        if (IsValidNamespaceOrClassName(viewModel.NewSetting.Category))
        {
          viewModel.LoggingSettingsConfiguration.Add(viewModel.NewSetting);
          viewModel.NewSetting = new() { Level = LogLevel.None };
        }
        else
        {
          return Json(new { Title = "Add Logging Setting", Message = $"The category '{viewModel.NewSetting.Category}' is not valid.  Logging categories must be 'Default', or match a namespace or type name.", Icon = "alert" });
        }
      }
    }

    foreach (ViewModels.Admin.SystemIndex.LogSetting setting in viewModel.LoggingSettingsConfiguration)
    {
      JObject loggingSection = appSettings["Logging"]["LogLevel"] as JObject;

      if (loggingSection != null)
      {
        string settingName = $"['{setting.Category}']";

        JToken settingToken = loggingSection.SelectToken(settingName);

        if (settingToken == null)
        {
          loggingSection.Add(setting.Category, JToken.FromObject(setting.Level.ToString()));
        }
        else
        {
          settingToken.Replace(setting.Level.ToString());
        }
      }
      else
      {
        return BadRequest();
      }
    }

    System.IO.File.WriteAllText(BuildConfigurationSettingsFilename(), appSettings.ToString());

    // wait a moment for the config to update
    await Task.Delay(TimeSpan.FromSeconds(1));

    ModelState.Clear();
    return View("_LogLevels", await BuildViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> RemoveLogLevel(ViewModels.Admin.SystemIndex viewModel, string name)
  {
    JObject appSettings = LoadConfiguration();

    if (appSettings == null)
    { 
      return BadRequest();
    }
    
    JObject loggingSection = appSettings["Logging"]["LogLevel"] as JObject;

    if (loggingSection != null)
    {
      JProperty setting = loggingSection.Properties().Where(item => item.Name == name).FirstOrDefault();
      setting?.Remove();
    }
    else
    {
      return BadRequest();
    }

    System.IO.File.WriteAllText(BuildConfigurationSettingsFilename(), appSettings.ToString());

    // wait a moment for the config to update
    await Task.Delay(TimeSpan.FromSeconds(1));
    ModelState.Clear();

    return View("_LogLevels", await BuildViewModel(viewModel));
  }
  
  private string BuildConfigurationSettingsFilename()
  {
    return $"appSettings.{this.HostingEnvironment.EnvironmentName}.json";
  }

  private JObject LoadConfiguration()
  {
    string appSettingsFileName = BuildConfigurationSettingsFilename();

    if (System.IO.File.Exists(appSettingsFileName))
    {
      JsonLoadSettings settings = new()
      {
        CommentHandling = CommentHandling.Load
      };

      return JObject.Parse(System.IO.File.ReadAllText(appSettingsFileName), settings) as JObject;
    }

    return null;
  }

  private Boolean IsInEnvironmentAppSettingsConfig(string category)
  {
    JObject appSettings = LoadConfiguration();

    if (appSettings != null)
    {
      JObject loggingSection = appSettings["Logging"]["LogLevel"] as JObject;

      if (loggingSection != null)
      {
        JProperty setting = loggingSection.Properties().Where(item => item.Name == category).FirstOrDefault();
        return setting != null;
      }
    }

    return false;
  }

  /// <summary>
  /// Return whether the specified value can be used in the Logging.LogLevel section of a configuration file.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  /// <remarks>
  /// Checks whether the value is a valid namespace or class name or a reserved special value that is valid in the Logging.LogLevel section.
  /// </remarks>
  private Boolean IsValidNamespaceOrClassName(string value)
  {
    if (value == "Default") return true;
    if (String.IsNullOrEmpty(value)) return false;

    return Nucleus.Core.Plugins.AssemblyLoader.GetTypes()
      .Where(type => type.Namespace?.Equals(value) == true || type.FullName.Equals(value))
      .Any();
  }

  [HttpGet]
  public ActionResult DownloadLogFile(string logFile, string format)
  {
    if (String.IsNullOrEmpty(logFile))
    {
      return BadRequest();
    }
    else
    {
      List<ViewModels.Admin.SystemIndex.LogEntry> data = ReadLogFile(logFile);

      switch (format)
      {
        case "excel":
          var exporter = new ExcelWriter<ViewModels.Admin.SystemIndex.LogEntry>(ExcelWorksheet.Modes.AutoDetect, nameof(ViewModels.Admin.SystemIndex.LogEntry.IsValid));
          string worksheetName = System.IO.Path.GetFileNameWithoutExtension(logFile);
          exporter.Worksheet.Name = worksheetName.Substring(0, worksheetName.Length > 32 ? 32 : worksheetName.Length);
          exporter.Export(data);
          return File(exporter.GetOutputStream(), ExcelWorksheet.MIMETYPE_EXCEL, $"{exporter.Worksheet.Name}.xlsx");

        default:
          byte[] content = System.Text.Encoding.UTF8.GetBytes(String.Join("\r\n", data));
          return File(content, "text/plain", logFile);
      }
    }
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

  private ResourceUtilization GetUtilization()
  {
    try
    {
      return this.Monitor.GetUtilization(TimeSpan.FromSeconds(5));
    }
    catch (Exception)
    {
      return default;
    }
  }

  private async Task<ViewModels.Admin.SystemIndex> BuildViewModel(ViewModels.Admin.SystemIndex viewModelInput)
  {
    // this technique for detecting Azure App Service is from https://github.com/dotnet/aspnetcore/blob/52aecd223059d0c846599904e5d4bf0f324037bc/src/Logging.AzureAppServices/src/WebAppContext.cs
    Boolean isRunningInAzureAppService = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOME")) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

    string operatingSystem = Environment.OSVersion.ToString();

    if (isRunningInAzureAppService)
    {
      operatingSystem += $" (Azure App Service {Environment.GetEnvironmentVariable("WEBSITE_PLATFORM_VERSION")})";
    }

    System.Diagnostics.Process.GetCurrentProcess().Refresh();
    TimeSpan uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
    ResourceUtilization resourceUtilization = GetUtilization();

    ViewModels.Admin.SystemIndex viewModelOutput = new()
    {
      Server = Environment.MachineName,
      Product = this.GetType().Assembly.Product(),
      Company = this.GetType().Assembly.Company(),
      Copyright = this.GetType().Assembly.Copyright(),
      License = ReadLicense(),
      Version = this.GetType().Assembly.Version(),
      Framework = Environment.Version.ToString(),
      EnvironmentName = this.HostingEnvironment.EnvironmentName,
      OperatingSystem = operatingSystem,
      OperatingSystemUser = $"{Environment.UserDomainName}/{Environment.UserName}",
      StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
      CpuUsedPercentage = resourceUtilization.CpuUsedPercentage,
      MemoryUsedPercentage = resourceUtilization.MemoryUsedPercentage,
      MemoryUsedBytes = resourceUtilization.MemoryUsedInBytes,
      Uptime = FormatUptime(uptime)
    };

    // For Linux, see if there is an /etc/os-release file and get more information.  /etc/os-release is fairly standard on Linux and contains
    // the "proper" OS Name and version
    if (Environment.OSVersion.Platform == PlatformID.Unix)
    {
      try
      {
        if (System.IO.File.Exists(LINUX_OS_INFO_FILE))
        {
          string osInfo = System.IO.File.ReadAllText(LINUX_OS_INFO_FILE);
          foreach (string osFileLine in osInfo.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
          {
            string[] parts = osFileLine.Split(new char[] { '=' });
            if (parts.Length == 2)
            {
              if (parts.First().Equals("PRETTY_NAME", StringComparison.OrdinalIgnoreCase))
              {
                viewModelOutput.OperatingSystem = parts.Last().Replace("\"", "");
              }
            }
          }
        }
      }
      catch
      {
        // this is not a critical operation.  Suppress exceptions.
      }
    }

    IConfigurationSection loggingConfig = this.Configuration.GetSection("Logging:LogLevel");
    foreach (IConfigurationSection configItem in loggingConfig.GetChildren())
    {
      viewModelOutput.LoggingSettingsConfiguration.Add(new()
      {
        Category = configItem.Key.Replace("Logging:LogLevel:", ""),
        Level = System.Enum.Parse<Microsoft.Extensions.Logging.LogLevel>(configItem.Value),
        IsEditable = configItem.Key != "Default" && IsInEnvironmentAppSettingsConfig(configItem.Key)
      });
    }

    // put the "system" (not editable) settings from appSettings.json at the top
    viewModelOutput.LoggingSettingsConfiguration = 
      viewModelOutput.LoggingSettingsConfiguration
      .OrderBy(item => item.IsEditable)
      .ThenBy(item => item.Category)
      .ToList();

    viewModelOutput.LogSettings.LogFile = viewModelInput.LogSettings.LogFile;
    viewModelOutput.LogSettings.LogMessage = "";

    if (!String.IsNullOrEmpty(this.LogFolderPath) && System.IO.Directory.Exists(this.LogFolderPath))
    {
      System.IO.DirectoryInfo logFolder = new(this.LogFolderPath);
      Logger.LogTrace("Log folder {logFolderPath} exists, and contains {count} .log files.", this.LogFolderPath, logFolder.EnumerateFiles("*.log").Count());

      List<Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo> logs = [];
      foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
      {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(file.Name, LogFileConstants.LOGFILE_REGEX);

        if (match.Success && match.Groups.Count >= 3)
        {
          Logger.LogTrace("Log file {file.Name} matches the expected pattern.", file.Name);
          string machineName = match.Groups["computername"].Value;

          if (DateTime.TryParseExact(match.Groups["logdate"].Value, LogFileConstants.DATE_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime logDate))
          {
            string startTime = $"{logDate.ToLocalTime():dd MMM yyyy hh:mm tt}";
            // use start + 23h 59m unless it would be in the future, otherwise "now"
            string finishTime = logDate > DateTime.UtcNow ? "now" : $"{logDate.AddDays(1).AddSeconds(-1).ToLocalTime():dd MMM yyyy hh:mm tt}";

            logs.Add(new Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo()
            {
              Filename = file.Name,
              // the date embedded in the file name is UTC. We convert to local time and show a date/time range (start/finish time) in the dropdown to
              // make it clear what the date range of log entries in the file is.
              Title = $"{machineName} {startTime} - {finishTime}",
              LogDate = logDate
            });
          }
          else
          {
            Logger.LogTrace("Could not parse the date for log file {file.Name}.", file.Name);
          }
        }
        else
        {
          Logger.LogTrace("Log file {file.Name} does not match the expected pattern.", file.Name);
        }
      }

      viewModelOutput.LogSettings.LogFiles = logs.OrderByDescending(log => log.LogDate).ToList();

      ReadLogFile(viewModelInput.LogSettings.LogContent, viewModelOutput.LogSettings);
    }
    else
    {
      viewModelOutput.LogSettings.LogFiles = [];
      if (String.IsNullOrEmpty(this.LogFolderPath))
      {
        viewModelOutput.LogSettings.LogMessage = $"The text file log folder is not set.";
      }
      else if (!System.IO.Directory.Exists(this.LogFolderPath))
      {
        viewModelOutput.LogSettings.LogMessage = $"The log folder {this.LogFolderPath} does not exist.";
      }
    }

    // Removed.  Config data can contain unsafe data.
    //viewModelOutput.Configuration = Sanitize((this.Configuration as IConfigurationRoot).GetDebugView());
    viewModelOutput.RunningTasks = this.RunningTaskQueue.ToList();

    viewModelOutput.ExtensionLoadContexts = Nucleus.Core.Plugins.AssemblyLoader.ListExtensionLoadContexts().OrderBy(loadContext => loadContext.Name).ToArray();
    viewModelOutput.ContentRootPath = this.HostingEnvironment.ContentRootPath;
    viewModelOutput.CacheReport = this.CacheManager.Report().OrderBy(cacheReport => cacheReport.Name).ToList();

    List<ViewModels.Admin.SystemIndex.DatabaseConnection> connections = new();

    foreach (DatabaseSchema schema in this.DatabaseOptions.Value.Schemas)
    {
      DatabaseConnectionOption connection = this.DatabaseOptions.Value.GetDatabaseConnection(schema.Name);

      if (connection != null)
      {
        ViewModels.Admin.SystemIndex.DatabaseConnection databaseConnection = new() { Schema = schema.Name, DatabaseType = connection.Type, ConnectionString = Sanitize(connection.ConnectionString) };
        try
        {
          databaseConnection.DatabaseInformation = Nucleus.Data.Common.DataProviderExtensions.GetDataProviderInformation(this.DatabaseOptions.Value, schema.Name);
        }
        catch (Exception ex)
        {
          databaseConnection.DatabaseInformation = new Dictionary<string, string>() { { "Connection Error", ex.Message } };
        }
        connections.Add(databaseConnection);
      }
      else
      {
        connections.Add(new ViewModels.Admin.SystemIndex.DatabaseConnection() { Schema = schema.Name, DatabaseType = "Not Found" });
      }
    }

    viewModelOutput.DatabaseConnections = connections;

    viewModelOutput.UsersOnline = await this.SessionManager.CountUsersOnline(this.Context.Site);

    //IServerVariablesFeature serverVars = HttpContext.Features.Get<IServerVariablesFeature>();
    //if (serverVars != null)
    //{				
    //	viewModelOutput.WebServerInformation.Add("Server", serverVars["SERVER_NAME"]);
    //	viewModelOutput.WebServerInformation.Add("Software", serverVars["SERVER_SOFTWARE"]);
    //	viewModelOutput.WebServerInformation.Add("Path", serverVars["APPL_PHYSICAL_PATH"]);				
    //}

    //AzureCounterData azureCounters = ViewModels.Admin.SystemIndex.AzureCounterData.Create();
    //if (azureCounters != null)
    //{
    //	viewModelOutput.WebServerInformation.Add("Processes", azureCounters.app.processes.ToString());
    //	viewModelOutput.WebServerInformation.Add("Process Limit", azureCounters.app.processLimit.ToString());

    //	viewModelOutput.WebServerInformation.Add("Threads", azureCounters.app.threads.ToString());
    //	viewModelOutput.WebServerInformation.Add("Thread Limit", azureCounters.app.threadLimit.ToString());

    //	viewModelOutput.WebServerInformation.Add("Connections", azureCounters.app.connections.ToString());
    //	viewModelOutput.WebServerInformation.Add("Connection Limit", azureCounters.app.connectionLimit.ToString());

    //	viewModelOutput.WebServerInformation.Add("Bytes In All Heaps", azureCounters.clr.bytesInAllHeaps.FormatFileSize());

    //	viewModelOutput.WebServerInformation.Add("aspnet", azureCounters.aspNetTest);
    //	viewModelOutput.WebServerInformation.Add("app", azureCounters.appTest);
    //	viewModelOutput.WebServerInformation.Add("clr", azureCounters.clrTest);
    //}
    return viewModelOutput;
  }

  private string ReadLicense()
  {
    try
    {
      string licenseFilePath = Path.Join(FolderOptions.GetWebRootFolder(), "license.txt");
      if (System.IO.File.Exists(licenseFilePath))
      {
        return System.IO.File.ReadAllText(licenseFilePath).ToHtml("text/plain");
      }
      else
      {
        return "license.txt not found.";
      }
    }
    catch
    {
      return "Unable to read license.txt";
    }
  }
}