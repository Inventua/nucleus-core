using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class ScheduledTasksController : Controller
	{
		private ILogger<ScheduledTasksController> Logger { get; }
		private IScheduledTaskManager ScheduledTaskManager { get; }
		private string LogFolderPath { get; }

		public ScheduledTasksController(ILogger<ScheduledTasksController> logger, ILoggerProvider logProvider, IScheduledTaskManager scheduledTaskManager)
		{
			this.Logger = logger;
			this.ScheduledTaskManager = scheduledTaskManager;

			if (logProvider is Nucleus.Core.Logging.TextFileLoggingProvider provider)
			{
				this.LogFolderPath = System.IO.Path.Combine(provider.Options.Path, ScheduledTask.SCHEDULED_TASKS_LOG_SUBPATH);
			}
		}

		/// <summary>
		/// Display the tasks list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the tasks list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.ScheduledTaskIndex viewModel)
		{
			return View("_ScheduledTasksList", await BuildViewModel(viewModel));
		}

		/// <summary>
		/// Display the task editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.ScheduledTaskEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await ScheduledTaskManager.CreateNew() : await ScheduledTaskManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddScheduledTask()
		{
			return View("Editor", await BuildViewModel(new ScheduledTask()));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.ScheduledTaskManager.Save(viewModel.ScheduledTask);
			
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> RunNow(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}
	
			ScheduledTaskHistory history =	await this.ScheduledTaskManager.GetMostRecentHistory(viewModel.ScheduledTask, Environment.MachineName);
			history.NextScheduledRun = null;
			await this.ScheduledTaskManager.SaveHistory(history);

			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteScheduledTask(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			await this.ScheduledTaskManager.Delete(viewModel.ScheduledTask);
			return View("Index", await BuildViewModel());
		}

		private async Task ReadLogFile(Nucleus.Abstractions.Models.Paging.PagingSettings settings, ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			List<ViewModels.Admin.SystemIndex.LogEntry> results = new();

			if (!String.IsNullOrEmpty(viewModel.LogFile))
			{
				ScheduledTask existing = await this.ScheduledTaskManager.Get(viewModel.ScheduledTask.Id);
				string logFilePath = System.IO.Path.Combine(LogFolder(existing).FullName, viewModel.LogFile);

				if (System.IO.File.Exists(logFilePath))
				{
					foreach (string line in System.IO.File.ReadAllLines(logFilePath))
					{
						results.Add(new ViewModels.Admin.SystemIndex.LogEntry(line));
					}
				}
			}

			viewModel.LogContent = new(settings);
			viewModel.LogContent.TotalCount = results.Count;

			viewModel.LogContent.Items = results
				.Skip(viewModel.LogContent.FirstRowIndex)
				.Take(viewModel.LogContent.PageSize)
				.ToList();
		}

		[HttpPost]
		public async Task<ActionResult> GetLogFile(ViewModels.Admin.ScheduledTaskEditor viewModel)
		{
			await ReadLogFile(viewModel.LogContent, viewModel);

			return View("_Log", viewModel);
		}

		private System.IO.DirectoryInfo LogFolder(ScheduledTask scheduledTask)
		{
			return new(System.IO.Path.Combine(this.LogFolderPath, scheduledTask.Name));			
		}

		private async Task<ViewModels.Admin.ScheduledTaskIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.ScheduledTaskIndex ());
		}

		private async Task<ViewModels.Admin.ScheduledTaskIndex> BuildViewModel(ViewModels.Admin.ScheduledTaskIndex viewModel)
		{
			viewModel.ScheduledTasks = await this.ScheduledTaskManager.List(viewModel.ScheduledTasks);

			return viewModel;
		}

		private async Task<ViewModels.Admin.ScheduledTaskEditor> BuildViewModel(ScheduledTask scheduledTask)
		{
			ViewModels.Admin.ScheduledTaskEditor viewModel = new();

			viewModel.ScheduledTask = scheduledTask;
			viewModel.AvailableServiceTypes = await this.ScheduledTaskManager.ListBackgroundServices();
			viewModel.History = await this.ScheduledTaskManager.ListHistory(scheduledTask);
			viewModel.LatestHistory = await this.ScheduledTaskManager.GetMostRecentHistory(scheduledTask, !scheduledTask.InstanceType.HasValue || scheduledTask.InstanceType == ScheduledTask.InstanceTypes.PerInstance ? null : Environment.MachineName);

			if (scheduledTask.Id != Guid.Empty && !String.IsNullOrEmpty(this.LogFolderPath))
			{
				System.IO.DirectoryInfo logFolder = LogFolder(scheduledTask);
				if (logFolder.Exists)
				{
					List<Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo> logs = new();
					foreach (System.IO.FileInfo file in logFolder.EnumerateFiles("*.log"))
					{
						System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(file.Name, LogFileConstants.LOGFILE_REGEX);

						if (match.Success && match.Groups.Count >= 3)
						{
							Logger.LogTrace("Log file {file.Name} matches the expected pattern.", file.Name);

							if (DateTime.TryParseExact(match.Groups[1].Value, LogFileConstants.DATETIME_FILENAME_FORMAT, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime logDate))
							{
								logs.Add(new Nucleus.Web.ViewModels.Admin.Shared.LogFileInfo()
								{
									Filename = file.Name,
									Title = $"{logDate.ToLocalTime():dd MMM yyyy HH:mm} [{match.Groups[2].Value}]",
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

					viewModel.LogFiles = logs.OrderByDescending(log => log.LogDate).ToList();
				}
			}
			
			if (!String.IsNullOrEmpty(viewModel.LogFile))
			{
				await ReadLogFile(viewModel.LogContent, viewModel);
			}

			return viewModel;
		}
	}
}
