using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.Extensions.Configuration;

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

		public SystemController(RunningTaskQueue runningTaskQueue, ILogger<SystemController> logger, ILoggerProvider logProvider, IConfiguration configuration)
		{
			this.RunningTaskQueue = runningTaskQueue;
			this.Logger = logger;
			this.Configuration = configuration;

			if (logProvider is Nucleus.Core.Logging.TextFileLoggingProvider provider)
			{
				this.LogFolderPath = provider.Options.Path;
			}
		}

		/// <summary>
		/// Display System information
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[HttpPost]
		public ActionResult Index(ViewModels.Admin.SystemIndex viewModelInput)
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

			if (!String.IsNullOrEmpty(this.LogFolderPath) && System.IO.Directory.Exists(this.LogFolderPath))
			{
				System.IO.DirectoryInfo logFolder = new(this.LogFolderPath);

				List<Nucleus.Web.ViewModels.Shared.LogFileInfo> logs = new();
				foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
				{
					System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(file.Name, LogFileConstants.LOGFILE_REGEX);

					if (match.Success && match.Groups.Count >= 3)
					{
						if (DateTime.TryParseExact(match.Groups[1].Value, LogFileConstants.DATE_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime logDate))
						{
							logs.Add(new Nucleus.Web.ViewModels.Shared.LogFileInfo()
							{
								Filename = file.Name,
								Title = $"{logDate.ToLocalTime():dd MMM yyyy} [{match.Groups[2].Value}]",
								LogDate = logDate
							});
						}
					}
				}

				viewModelOutput.LogFiles = logs.OrderByDescending(log => log.LogDate).ToList();

				if (!String.IsNullOrEmpty(viewModelInput.LogFile))
				{
					string logFilePath = System.IO.Path.Combine(this.LogFolderPath, viewModelInput.LogFile);
					if (System.IO.File.Exists(logFilePath))
					{
						viewModelOutput.LogContent = System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath));
					}
				}
			}
			else
			{
				viewModelOutput.LogFiles = new();
			}

			viewModelOutput.Configuration = Sanitize((this.Configuration as IConfigurationRoot).GetDebugView());
			viewModelOutput.RunningTasks = this.RunningTaskQueue.ToList();
			
			return View("Index", viewModelOutput);
		}

		[HttpPost]
		public ActionResult GetLogFile(ViewModels.Admin.SystemIndex viewModelInput)
		{
			if (!String.IsNullOrEmpty(viewModelInput.LogFile))
			{
				string logFilePath = System.IO.Path.Combine(this.LogFolderPath, viewModelInput.LogFile);
				if (System.IO.File.Exists(logFilePath))
				{
					return File(System.Text.Encoding.UTF8.GetBytes(System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath))), "text/plain");
				}
			}
			
			return BadRequest();			
		}

		/// <summary>
		/// Look for connection strings and passwords and hide them
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string Sanitize(string value)
		{
			value = System.Text.RegularExpressions.Regex.Replace(value, "Password[:-=](.*)[ ,;]{0,1}.*$", "Password=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
			value = System.Text.RegularExpressions.Regex.Replace(value, "ConnectionString=(.*).*$", "ConnectionString=********", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

			return value;
		}

	}
}
