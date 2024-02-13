using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.Services.Instrumentation;

/// <summary>
/// Class used to create and populate OpenTelemetry instrumentation.  This class
/// - Supplements the meters published by OpenTelemetry.Instrumentation.Process / .AddProcessInstrumentation by adding a process.cpu.utilization Gauge
/// - Publishes Nucleus application data (restart date, environment name and users online)/// 
/// </summary>
/// <remarks>
/// Nucleus also enables OpenTelemetry instrumentation provided by OpenTelemetry.Instrumentation Nuget packages, <seealso cref="Extensions"/>.  Nucleus also
/// publishes page visit information in <seealso cref="TelemetryMiddleware"/>.
/// </remarks>
public class TelemetryMonitor : IHostedService
{
  private static DateTime StartDate = DateTime.UtcNow;

  private IResourceMonitor Monitor { get; }
  private IHostEnvironment Environment { get; }

  private ISessionManager SessionManager { get; }
  private ISiteManager SiteManager { get; }

  private Meter ApplicationMeter { get; }
  private Meter ResourceMeter { get; }

  private static ObservableGauge<int> ApplicationGauge { get; set; }

  private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(5);

  public TelemetryMonitor(IMeterFactory meterFactory, IResourceMonitor monitor, IHostEnvironment environment, ISessionManager sessionManager, ISiteManager siteManager)
  {
    this.Monitor = monitor;
    this.Environment = environment;
    this.SessionManager = sessionManager;
    this.SiteManager = siteManager;

    this.ResourceMeter = meterFactory.Create("process.cpu", typeof(TelemetryMonitor).Assembly.GetName().Version.ToString());
    this.ApplicationMeter = meterFactory.Create("nucleus.application", typeof(TelemetryMonitor).Assembly.GetName().Version.ToString());
  }

  /// <summary>
  /// Create OpenTelemetry counters and gauges.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public Task StartAsync(CancellationToken cancellationToken)
  {
    this.ResourceMeter.CreateObservableGauge("process.cpu.utilization", () => new Measurement<double>(GetUtilization().CpuUsedPercentage), description: "The CPU used by the process as a percentage of the CPU units available in the system.");
    this.ApplicationMeter.CreateObservableGauge<int>("nucleus.application.environment", ReportApplicationEnvironment, description: "Nucleus application data.");

    this.ApplicationMeter.CreateObservableGauge<long>("nucleus.application.users_online", () => ReportUsersOnline().ToBlockingEnumerable(), description: "Nucleus users online.");

    return Task.CompletedTask;
  }

  /// <summary>
  /// Report Nucleus application environment data.
  /// </summary>
  /// <returns></returns>
  private Measurement<int> ReportApplicationEnvironment()
  {
    return new Measurement<int>(1, new List<KeyValuePair<string, object>>()
    {
      new KeyValuePair<string, object>("start_date", StartDate),
      new KeyValuePair<string, object>("environment_name", this.Environment.EnvironmentName)
    });
  }

  /// <summary>
  /// Report users online for each site.
  /// </summary>
  /// <returns></returns>
  private async IAsyncEnumerable<Measurement<long>> ReportUsersOnline()
  {   
    foreach (Site site in await this.SiteManager.List())
    {
      yield return new Measurement<long>(await this.SessionManager.CountUsersOnline(site), new List<KeyValuePair<string, object>>()
      {
        new KeyValuePair<string, object>("site_name", site.Name),
        new KeyValuePair<string, object>("site_id", site.Id)
      });
    }
  }

  private ResourceUtilization GetUtilization()
  {
    return Monitor.GetUtilization(Threshold);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}
