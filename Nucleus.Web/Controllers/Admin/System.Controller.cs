using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class SystemController : Controller
	{
		private string LogFolderPath { get; }
		private RunningTaskQueue RunningTaskQueue { get; }
		private IConfiguration Configuration { get; }
		private ILogger<SystemController> Logger { get; }
		private IOptions<DatabaseOptions> DatabaseOptions { get; }
		private ISessionManager SessionManager { get; }
		private Context Context { get; }

		public SystemController(Context context, RunningTaskQueue runningTaskQueue, ILogger<SystemController> logger, IOptions<DatabaseOptions> databaseOptions, IOptions<Nucleus.Core.Logging.TextFileLoggerOptions> options, IConfiguration configuration, ISessionManager sessionManager)
		{
			this.Context = context;
			this.RunningTaskQueue = runningTaskQueue;
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
			ViewModels.Admin.SystemIndex viewModelOutput = new()
			{
				Server = Environment.MachineName,
				Product = this.GetType().Assembly.Product(),
				Company = this.GetType().Assembly.Company(),
				Copyright = this.GetType().Assembly.Copyright(),
				Version = $"{this.GetType().Assembly.Version()}",
				Framework = Environment.Version.ToString(),
				OperatingSystem = Environment.OSVersion.ToString(),
				LogFile = viewModelInput.LogFile
			};

			viewModelOutput.LogMessage = "";


			if (!String.IsNullOrEmpty(this.LogFolderPath) && System.IO.Directory.Exists(this.LogFolderPath))
			{
				System.IO.DirectoryInfo logFolder = new(this.LogFolderPath);
				Logger.LogTrace("Log folder {logFolderPath} exists, and contains {count} .log files.", this.LogFolderPath, logFolder.EnumerateFiles("*.log").Count());

				List<Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo> logs = new();
				foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
				{
					System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(file.Name, LogFileConstants.LOGFILE_REGEX);

					if (match.Success && match.Groups.Count >= 3)
					{
						Logger.LogTrace("Log file {file.Name} matches the expected pattern.", file.Name);

						if (DateTime.TryParseExact(match.Groups[1].Value, LogFileConstants.DATE_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime logDate))
						{
							logs.Add(new Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo()
							{
								Filename = file.Name,
								Title = $"{logDate.ToLocalTime():dd MMM yyyy} [{match.Groups[2].Value}]",
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

				viewModelOutput.LogFiles = logs.OrderByDescending(log => log.LogDate).ToList();

				viewModelOutput.LogContent = ReadLogFile(viewModelInput);
			}
			else
			{
				viewModelOutput.LogFiles = new();
				if (String.IsNullOrEmpty(this.LogFolderPath))
				{
					viewModelOutput.LogMessage = $"The text file log folder is not set.";
				}
				else if (!System.IO.Directory.Exists(this.LogFolderPath))
				{
					viewModelOutput.LogMessage = $"The log folder {this.LogFolderPath} does not exist.";
				}
			}

			// Removed.  Config data can contain unsafe data.
			//viewModelOutput.Configuration = Sanitize((this.Configuration as IConfigurationRoot).GetDebugView());
			viewModelOutput.RunningTasks = this.RunningTaskQueue.ToList();

			List<ViewModels.Admin.SystemIndex.DatabaseConnection> connections = new();

			foreach (DatabaseSchema schema in this.DatabaseOptions.Value.Schemas)
			{
				DatabaseConnectionOption connection = this.DatabaseOptions.Value.GetDatabaseConnection(schema.ConnectionKey);

				if (connection != null)
				{
					ViewModels.Admin.SystemIndex.DatabaseConnection databaseConnection = new ViewModels.Admin.SystemIndex.DatabaseConnection() { Schema = schema.Name, DatabaseType = connection.Type, ConnectionString = Sanitize(connection.ConnectionString) };
					databaseConnection.DatabaseInformation = Nucleus.Data.Common.DataProviderExtensions.GetDataProviderInformation(ControllerContext.HttpContext.RequestServices, schema.Name);

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

			return View("Index", viewModelOutput);
		}

		private List<ViewModels.Admin.SystemIndex.LogEntry> ReadLogFile(ViewModels.Admin.SystemIndex viewModelInput)
		{
			List<ViewModels.Admin.SystemIndex.LogEntry> results = new();

			if (!String.IsNullOrEmpty(viewModelInput.LogFile))
			{
				string logFilePath = System.IO.Path.Combine(this.LogFolderPath, viewModelInput.LogFile);
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
		public ActionResult GetLogFile(ViewModels.Admin.SystemIndex viewModelInput)
		{
			viewModelInput.LogContent = ReadLogFile(viewModelInput);

			return View("_Log", viewModelInput);

			//if (!String.IsNullOrEmpty(viewModelInput.LogFile))
			//{
			//	string logFilePath = System.IO.Path.Combine(this.LogFolderPath, viewModelInput.LogFile);
			//	if (System.IO.File.Exists(logFilePath))
			//	{
			//		return File(System.Text.Encoding.UTF8.GetBytes(System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath))), "text/plain");
			//	}
			//}
			//return BadRequest();
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

			//value = System.Text.RegularExpressions.Regex.Replace(value, "Password[:-=](.*)[ ,;]{0,1}.*$", "Password=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//value = System.Text.RegularExpressions.Regex.Replace(value, "Pwd[:-=](.*)[ ,;]{0,1}.*$", "Pwd=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//value = System.Text.RegularExpressions.Regex.Replace(value, "User ID[:-=](.*)[ ,;]{0,1}.*$", "User ID=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//value = System.Text.RegularExpressions.Regex.Replace(value, "UserID[:-=](.*)[ ,;]{0,1}.*$", "UserID=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//value = System.Text.RegularExpressions.Regex.Replace(value, "uid[:-=](.*)[ ,;]{0,1}.*$", "uid=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//value = System.Text.RegularExpressions.Regex.Replace(value, "ConnectionString=(.*).*$", "ConnectionString=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			//return value;
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
}
