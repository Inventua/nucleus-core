using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Services.HealthChecks;

/// <summary>
/// Health check which returns whether the application has started.
/// </summary>
public class ApplicationReadyHealthCheck : IHealthCheck, IHostedService
{
  private static Boolean _IsReady { get; set; } = false;

  public ApplicationReadyHealthCheck()
  {
 
  }

  /// <summary>
  /// Set a flag when the application has started.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public Task StartAsync(CancellationToken cancellationToken)
  {
    _IsReady = true;
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _IsReady = false;
    return Task.CompletedTask;
  }

  /// <summary>
  /// Return whether the application has started.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded());    
  }
}
