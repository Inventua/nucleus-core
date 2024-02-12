using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Services.HealthChecks;

public class DatabaseConnectionsHealthCheck : IHealthCheck
{
  private IOptions<DatabaseOptions> DatabaseOptions { get; }

  public DatabaseConnectionsHealthCheck(IOptions<DatabaseOptions> databaseOptions)
  {
    this.DatabaseOptions = databaseOptions;
  }

  /// <summary>
  /// Check database connections.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    Dictionary<string, object> databaseCheckResults = new();
    Boolean coreDatabaseFailed = false;

    foreach (DatabaseSchema schema in this.DatabaseOptions.Value.Schemas)
    {
      DatabaseConnectionOption connection = this.DatabaseOptions.Value.GetDatabaseConnection(schema.Name);

      if (connection != null)
      {
        try
        {
          Nucleus.Data.Common.DataProviderExtensions.GetDataProviderInformation(this.DatabaseOptions.Value, schema.Name);
        }
        catch (Exception ex)
        {
          databaseCheckResults.Add("Connection Error", $"Schema '{schema.Name}': {ex.Message}");
          if (schema.Name == "*") coreDatabaseFailed = true;
        }
      }
      else
      {
        databaseCheckResults.Add("Database configuration Error", $"Schema '{schema.Name}' not Found");
        if (schema.Name == "*") coreDatabaseFailed = true;
      }
    }

    if (databaseCheckResults.Any())
    {
      // if the core database has a connection problem, report Unhealthy, but if the database belongs to a specific set of extensions (isn't "*")
      // then the connection failure would only affect those extensions, so report Degraded.
      return Task.FromResult(new HealthCheckResult(coreDatabaseFailed ? HealthStatus.Unhealthy : HealthStatus.Degraded, "One or more database connection tests failed.", data: databaseCheckResults));
    }
    else
    {
      return Task.FromResult(HealthCheckResult.Healthy());
    }
  }
}
