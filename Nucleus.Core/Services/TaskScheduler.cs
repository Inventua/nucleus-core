﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Services
{
  /// <summary>
  /// Manages scheduling and execution of scheduled tasks.
  /// </summary>
  public class TaskScheduler : IHostedService, IDisposable, Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<ScheduledTask, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>
  {
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio

    private Timer Timer { get; set; }
    private ILogger<TaskScheduler> Logger { get; }
    private IScheduledTaskManager ScheduledTaskManager { get; }
    private IServiceProvider Services { get; }
    public RunningTaskQueue Queue { get; }
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    private DateTime SchedulerStartTime = DateTime.UtcNow;

    public TaskScheduler(IServiceProvider services, ILogger<TaskScheduler> logger, IScheduledTaskManager scheduledTaskManager, RunningTaskQueue queue)
    {
      this.Services = services;
      this.Logger = logger;
      this.ScheduledTaskManager = scheduledTaskManager;
      this.Queue = queue;
    }

    /// <summary>
    /// Initialize the timer to start executing scheduled tasks after 60 seconds.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
      this.Logger.LogInformation("The task scheduler is starting.");
      this.Timer = new(DoWork, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

      return Task.CompletedTask;
    }

    /// <summary>
    /// Shut down
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
      this.Logger.LogInformation("The task scheduler is stopping.");
      this.Timer?.Change(Timeout.Infinite, 0);
      this.CancellationTokenSource.Cancel();

      return Task.CompletedTask;
    }

    public void Dispose()
    {
      this.Timer?.Dispose();
      this.Timer = null;
    }

    private void DoWork(object state)
    {
      DoWorkAsync(state).Wait();
    }

    /// <summary>
    /// Check for scheduled tasks that are ready to be invoked and start them.
    /// </summary>
    /// <param name="state"></param>
    private async Task DoWorkAsync(object state)
    {
      try
      {
        await Collect();

        foreach (ScheduledTask task in await this.ScheduledTaskManager.List())
        {
          await CheckAndRun(task, false);
        }
      }
      catch (Exception e)
      {
        Logger.LogError(e, "Checking scheduled tasks.");
      }
    }

    private async Task CheckAndRun(ScheduledTask task, Boolean ignoreSchedule)
    {
      if (await ShouldRunNow(task, ignoreSchedule))
      {
        await StartScheduledTask(task);
      }
    }

    private async Task<Boolean> ShouldRunNow(ScheduledTask task, Boolean ignoreSchedule)
    {
      if (IsTaskEnabled(task) || ignoreSchedule)
      {
        ScheduledTaskHistory history = await this.ScheduledTaskManager.GetMostRecentHistory(task, Environment.MachineName);

        // if "run now", or the task has not run before
        if (ignoreSchedule || history == null || !history.NextScheduledRun.HasValue)
        {
          return !this.Queue.Contains(task);
        }

        switch (task.IntervalType)
        {
          case ScheduledTask.Intervals.Startup:
            return (this.SchedulerStartTime > history.FinishDate) && !this.Queue.Contains(task);
          default:
            return (DateTime.UtcNow > history.NextScheduledRun && !this.Queue.Contains(task));
        }
      }
      return false;
    }

    private static Boolean IsTaskEnabled(ScheduledTask task)
    {
      return task.Enabled && task.Interval > 0 && task.IntervalType != ScheduledTask.Intervals.None;
    }

    /// <summary>
    /// This implementation of ISystemEventHandler[ScheduledTask, Update] causes the updated scheduled task to be evaluated for 
    /// execution immediately.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task Invoke(ScheduledTask task)
    {
      if (IsTaskEnabled(task))
      {
        await CheckAndRun(task, true);
      }
    }

    /// <summary>
    /// Check for completed tasks in the queue: reschedule then and remove them from the queue.
    /// </summary>
    private async Task Collect()
    {
      foreach (RunningTask queueItem in this.Queue.ToList())
      {
        // queueItem.Task can be null if the implementation of .InvokeAsync doesn't return anything
        if (queueItem.Task == null || queueItem.Task.IsCompleted)
        {
          await SignalCompleted(queueItem);
        }
      }
    }

    private async Task SignalCompleted(RunningTask runningTask)
    {
      runningTask.History.FinishDate = DateTime.UtcNow;
      runningTask.History.NextScheduledRun = CalculateInterval(runningTask.StartDate, runningTask.ScheduledTask.IntervalType, runningTask.ScheduledTask.Interval);

      runningTask.Progress.Status = ScheduledTaskProgress.State.Succeeded;
      await LogTaskSucceeded(runningTask.History);

      this.Queue.Remove(runningTask.ScheduledTask);
    }

    private async Task SignalFailed(RunningTask runningTask)
    {
      runningTask.History.FinishDate = DateTime.UtcNow;
      runningTask.History.NextScheduledRun = CalculateInterval(runningTask.StartDate, runningTask.ScheduledTask.IntervalType, runningTask.ScheduledTask.Interval);

      runningTask.Progress.Status = ScheduledTaskProgress.State.Error;
      await LogTaskFailed(runningTask.History);

      this.Queue.Remove(runningTask.ScheduledTask);
    }

    private DateTime CalculateInterval(DateTime thisRunDateTime, ScheduledTask.Intervals intervalType, int interval)
    {
      switch (intervalType)
      {
        case ScheduledTask.Intervals.Minutes:
          return thisRunDateTime.AddMinutes(interval);

        case ScheduledTask.Intervals.Hours:
          return thisRunDateTime.AddHours(interval);

        case ScheduledTask.Intervals.Days:
          return thisRunDateTime.AddDays(interval);

        case ScheduledTask.Intervals.Weeks:
          return thisRunDateTime.AddDays(interval * 7);

        case ScheduledTask.Intervals.Months:
          return thisRunDateTime.AddMonths(interval);

        case ScheduledTask.Intervals.Years:
          return thisRunDateTime.AddYears(interval);

        case ScheduledTask.Intervals.Startup:
          return thisRunDateTime;

        default:  // case ScheduledTask.IntervalTypes.None:
          return thisRunDateTime;
      }
    }

    private async Task LogTaskFailed(ScheduledTaskHistory history)
    {
      history.Status = ScheduledTaskProgress.State.Error;
      await this.ScheduledTaskManager.SaveHistory(history);
    }

    private async Task LogTaskRunning(ScheduledTaskHistory history)
    {
      history.Status = ScheduledTaskProgress.State.Running;
      await this.ScheduledTaskManager.SaveHistory(history);
    }

    private async Task LogTaskSucceeded(ScheduledTaskHistory history)
    {
      history.Status = ScheduledTaskProgress.State.Succeeded;
      await this.ScheduledTaskManager.SaveHistory(history);
    }

    /// <summary>
    /// Start the <see cref="IScheduledTask"/> represented by the specified <see cref="ScheduledTask"/>.
    /// </summary>
    /// <param name="task"></param>
    async Task StartScheduledTask(ScheduledTask task)
    {
      using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(task.TypeName))
      {
        ScheduledTaskHistory history = new();
        history.ScheduledTaskId = task.Id;
        history.Server = Environment.MachineName;
        history.StartDate = DateTime.UtcNow;
        history.Status = ScheduledTaskProgress.State.None;

        await this.ScheduledTaskManager.SaveHistory(history);

        System.Type serviceType = Type.GetType(task.TypeName);

        if (serviceType == null)
        {
          this.Logger.LogError("Unable to find type '{typeName}'", task.TypeName);
          await LogTaskFailed(history);
          return;
        }

        object service = this.Services.GetService(serviceType);

        if (service == null)
        {
          this.Logger.LogError("Unable to create an instance of '{typeName}'", task.TypeName);
          await LogTaskFailed(history);
          return;
        }

        IScheduledTask taskService = service as IScheduledTask;

        if (taskService == null)
        {
          this.Logger.LogError("Unable to create an instance of '{TypeName}' because it does not implement IScheduledTask", task.TypeName);
          await LogTaskFailed(history);
          return;
        }

        RunningTask runningTask = this.Queue.Add(task, history);
        runningTask.OnProgress += HandleProgress;

        runningTask.Progress.Status = ScheduledTaskProgress.State.Running;
        await LogTaskRunning(history);

        runningTask.Task = Task.Run(async () =>
        {
          this.Logger.LogInformation("The task scheduler has started the '{scheduledTaskName}' task.", runningTask.ScheduledTask.Name);
          using (this.Logger.BeginScope(runningTask))
          {
            try
            {
              await taskService.InvokeAsync(runningTask, runningTask.ProgressCallback, this.CancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
              this.Logger.LogError(ex, "Error starting scheduled task '{typeName}'", task.TypeName);
              await SignalFailed(runningTask);
            }
          }
        });
      }
    }

    async Task HandleProgress(RunningTask sender, ScheduledTaskProgress progress)
    {
      switch (progress.Status)
      {
        case ScheduledTaskProgress.State.Succeeded:
          await SignalCompleted(sender);
          this.Logger.LogInformation("The '{scheduledTaskName}' task succeeded.", sender.ScheduledTask.Name);
          break;
        case ScheduledTaskProgress.State.Error:
          this.Logger.LogInformation("The '{scheduledTaskName}' task failed: {message}.", sender.ScheduledTask.Name, progress.Message);
          break;
      }
    }
  }
}
