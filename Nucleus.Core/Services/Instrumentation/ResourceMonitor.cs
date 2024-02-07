using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Cache;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Linq.Expressions;

namespace Nucleus.Core.Services.Instrumentation;

// Supplement the meters published by OpenTelemetry.Instrumentation.Process / .AddProcessInstrumentation

public class ResourceMonitor : IHostedService
{
  private IResourceMonitor Monitor { get; }

  private Meter ResourceMeter { get; }

  private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(5);

  public ResourceMonitor(IMeterFactory meterFactory, IResourceMonitor monitor)
  {
    Monitor = monitor;
    ResourceMeter = meterFactory.Create("process.cpu", typeof(ResourceMonitor).Assembly.GetName().Version.ToString());
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    ResourceMeter.CreateObservableGauge("process.cpu.utilization", () => new Measurement<double>(GetUtilization().CpuUsedPercentage), description: "The CPU used by the process as a percentage of the CPU units available in the system.");

    return Task.CompletedTask;
  }

  private ResourceUtilization GetUtilization()
  {
    return Monitor.GetUtilization(Threshold);
  }

  ////private IEnumerable<Measurement<double>> GetResources()
  ////{ResourceMeter.CreateObservableGauge("nucleus.resources", GetResources, description: "Resource utilization for Nucleus.");

  ////  ResourceUtilization utilization = Monitor.GetUtilization(TimeSpan.FromSeconds(3));

  ////  yield return new Measurement<double>(utilization.CpuUsedPercentage, new KeyValuePair<string, object>("nucleus.resources.utilization", "cpu_used_percentage"));
  ////  yield return new Measurement<double>(utilization.MemoryUsedPercentage, new KeyValuePair<string, object>("nucleus.resources.utilization", "memory_used_percentage"));
  ////  yield return new Measurement<double>(utilization.MemoryUsedInBytes, new KeyValuePair<string, object>("nucleus.resources.utilization", "memory_used_bytes"));

  ////  yield return new Measurement<double>(utilization.SystemResources.GuaranteedMemoryInBytes, new KeyValuePair<string, object>("nucleus.resources.utilization", "guaranteed_memory_bytes"));
  ////  yield return new Measurement<double>(utilization.SystemResources.MaximumMemoryInBytes, new KeyValuePair<string, object>("nucleus.resources.utilization", "maximum_memory_bytes"));
  ////}

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}
