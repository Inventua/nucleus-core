Scheduled tasks perform their work periodically and are generally used to perform system maintenance, like expiring cache entries or updating a search 
index.  Scheduled Tasks are typically added to an existing extension to perform actions which relate to the extension.

To create a new scheduled task, create a class which inherits 
[Nucleus.Abstractions.IScheduledTask](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.IScheduledTask/#mnu-Nucleus-Abstractions-IScheduledTask).  Nucleus 
automatically detects implementations of `IScheduledTask` and adds them to the dependency injection container during startup.  As with other .NET core classes 
which participate in dependency injection, you should add parameters to your constructor to get references to services that your scheduled task requires.

#### Sample Scheduled Task
```
using System;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;

public class MyScheduledTask : IScheduledTask
{
  private IMyExtensionManager MyExtensionManager { get; }
  private  ILogger<MyScheduledTask> Logger { get; }

  public CollectSessionsScheduledTask(IMyExtensionManager myExtensionManager, ILogger<MyScheduledTask> logger)
  {
    this.MyExtensionManager = myExtensionManager;
    this.Logger = logger;
  }

  public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
  {
    return Task.Run(async () => await CollectExpiredSessions(progress));      
  }
    
  private async Task CollectExpiredSessions(IProgress<ScheduledTaskProgress> progress)
  {
    this.Logger.LogInformation("My scheduled task has started");
    var count = await this.MyExtensionManager.DoSomething();
    this.Logger.LogInformation("{count} items were updated.", count);
    progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
  }
}
```

Scheduled Tasks implement the `InvokeAsync` method to perform their work.  The implementation of `InvokeAsync` should execute asynchronously 
(use Task.Run to call a function then return immediately) so that scheduled tasks can run in parallel.  The progress object should be used 
to report success or failure by calling `progress.Report`.

> If your scheduled task uses logging and your site is configured to use the Nucleus Text File Logger, it automatically detects log entries 
which originate from a scheduled task and writes log output to a file system path which is specific to your scheduled task, 
`[LogFolder}\]Scheduled Tasks\[schedule-name]`.  Log output is also written to the main Nucleus log.  Site administrators can review 
scheduled task logs in isolation from the rest of the system logs.

> **_NOTE:_**   Nucleus automatically detects scheduled tasks in installed assemblies, so you don't need to add anything special to your
extension manifest (other than including the assembly which contains your class).  Nucleus does not automatically configure a 
schedule for installed scheduled Tasks - your users must use the [Task Scheduler](/manage/task-scheduler/) to create a schedule.






