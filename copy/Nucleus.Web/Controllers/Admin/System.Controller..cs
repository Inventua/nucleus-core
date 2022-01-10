using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class SystemController : Controller
	{
		private string LogFolderPath { get; }
		private RunningTaskQueue RunningTaskQueue { get; }
		private IConfiguration Configuration { get; }
		public SystemController(RunningTaskQueue runningTaskQueue, ILoggerProvider logProvider, IConfiguration configuration)
		{
			Nucleus.Core.Logging.TextFileLoggingProvider provider = logProvider as Nucleus.Core.Logging.TextFileLoggingProvider;
			if (provider != null)
			{
				this.LogFolderPath = provider.Options.Path;
				this.RunningTaskQueue = runningTaskQueue;
			}
			this.Configuration = configuration;
		}

		/// <summary>
		/// Display System information
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[HttpPost]
		public ActionResult Index(ViewModels.Admin.SystemIndex viewModelInput)
		{
			ViewModels.Admin.SystemIndex viewModelOutput = new ViewModels.Admin.SystemIndex()
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

			System.IO.DirectoryInfo logFolder = new(this.LogFolderPath);
			//viewModelOutput.LogFiles = logFolder.EnumerateFiles().OrderByDescending(file => file.CreationTimeUtc);

			List<Nucleus.Web.ViewModels.Shared.LogFileInfo> logs = new();
			foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
			{
				DateTime logDate;
				if (DateTime.TryParseExact(System.IO.Path.GetFileNameWithoutExtension(file.Name), Constants.DATE_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out logDate))
				{
					logs.Add(new Nucleus.Web.ViewModels.Shared.LogFileInfo()
					{
						Filename = file.Name,
						Title = logDate.ToLocalTime().ToString("dd MMM yyyy"),
						LogDate = logDate
					});
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

			viewModelOutput.Configuration = (this.Configuration as IConfigurationRoot).GetDebugView();
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
					//return File( System.IO.File.ReadAllBytes(logFilePath),"text/plain");
					return File(System.Text.Encoding.UTF8.GetBytes(System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath))), "text/plain");
				}
			}
			
			return BadRequest();			
		}
	}
}
