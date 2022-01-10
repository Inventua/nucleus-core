using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.Extensions.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
	public class ScheduledTasksController : Controller
	{
		private ILogger<ScheduledTasksController> Logger { get; }
		private ScheduledTaskManager ScheduledTaskManager { get; }
		private Context Context { get; }
		private string LogFolderPath { get; }

		public ScheduledTasksController(Context context, ILogger<ScheduledTasksController> logger, ILoggerProvider logProvider, ScheduledTaskManager scheduledTaskManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.ScheduledTaskManager = scheduledTaskManager;

			Nucleus.Core.Logging.TextFileLoggingProvider provider = logProvider as Nucleus.Core.Logging.TextFileLoggingProvider;
			if (provider != null)
			{
				this.LogFolderPath = System.IO.Path.Combine(provider.Options.Path, Constants.SCHEDULED_TASKS_LOG_SUBPATH);
			}
		}

		/// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.ScheduledTaskEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? ScheduledTaskManager.CreateNew() : ScheduledTaskManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddScheduledTask()
		{
			return View("Editor", BuildViewModel(new ScheduledTask()));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.ScheduledTaskManager.Save(viewModel.ScheduledTask);
			
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult RunNow(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			viewModel.ScheduledTask.NextScheduledRun = DateTime.UtcNow;
			this.ScheduledTaskManager.Save(viewModel.ScheduledTask);

			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteScheduledTask(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			this.ScheduledTaskManager.Delete(viewModel.ScheduledTask);
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult GetLogFile(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			if (!String.IsNullOrEmpty(viewModel.LogFile))
			{
				ScheduledTask existing = this.ScheduledTaskManager.Get(viewModel.ScheduledTask.Id);
				string logFilePath = System.IO.Path.Combine(LogFolder(existing).FullName, viewModel.LogFile);
				if (System.IO.File.Exists(logFilePath))
				{
					//return File( System.IO.File.ReadAllBytes(logFilePath),"text/plain");
					return File(System.Text.Encoding.UTF8.GetBytes(System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath))), "text/plain");
				}
			}

			return BadRequest();
		}

		private System.IO.DirectoryInfo LogFolder(ScheduledTask scheduledTask)
		{
			return new(System.IO.Path.Combine(this.LogFolderPath, scheduledTask.Name));
		}

		private ViewModels.Admin.ScheduledTaskIndex BuildViewModel()
		{
			ViewModels.Admin.ScheduledTaskIndex viewModel = new();

			viewModel.ScheduledTasks = this.ScheduledTaskManager.List();
			
			return viewModel;
		}

		private ViewModels.Admin.ScheduledTaskEditor BuildViewModel(ScheduledTask scheduledTask)
		{
			ViewModels.Admin.ScheduledTaskEditor viewModel = new();

			viewModel.ScheduledTask = scheduledTask;
			viewModel.AvailableServiceTypes = this.ScheduledTaskManager.ListBackgroundServices();
			viewModel.History = this.ScheduledTaskManager.ListHistory(scheduledTask);

			if (scheduledTask.Id != Guid.Empty)
			{
				System.IO.DirectoryInfo logFolder = LogFolder(scheduledTask);
				if (logFolder.Exists)
				{
					List<Nucleus.Web.ViewModels.Shared.LogFileInfo> logs = new();
					foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
					{
						DateTime logDate;
						if (DateTime.TryParseExact(System.IO.Path.GetFileNameWithoutExtension(file.Name), Constants.DATETIME_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal,out logDate))
						{
							logs.Add(new Nucleus.Web.ViewModels.Shared.LogFileInfo()
							{
								Filename = file.Name,
								Title = logDate.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
								LogDate = logDate
							});
						}
					}

					viewModel.LogFiles = logs.OrderByDescending(log => log.LogDate).ToList();
				}
			}
			
			if (!String.IsNullOrEmpty(viewModel.LogFile))
			{
				string logFilePath = System.IO.Path.Combine(this.LogFolderPath, viewModel.LogFile);
				if (System.IO.File.Exists(logFilePath))
				{
					viewModel.LogContent = System.Net.WebUtility.HtmlEncode(System.IO.File.ReadAllText(logFilePath));
				}
			}

			return viewModel;
		}
	}
}
