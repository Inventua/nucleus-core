# Health Checks
Health checks are exposed by Nucleus as HTTP endpoints. Health checks can be used by load balancers and other infrastructure components to check the 
status of Nucleus on a server.

> Health checks are not enabled by default.  See the [Configuration](/manage/operations/health-checks/#configuration) section below for instructions.

Three health checks are available:

{.table-25-75}
|                      |                                                                                               |
|----------------------|-----------------------------------------------------------------------------------------------|
| Live                 | Checks that Nucleus can respond to HTTP requests.  The default endpoint is `/_live`. The *Live* check doesn't check anything, and always returns a `Healthy` status if Nucleus is running.             |  
| Ready                | Checks that Nucleus has completed its startup process. The default endpoint is `/_ready`. The *Ready* check returns a `Degraded` status while Nucleus is starting, and a `Healthy` status once Nucleus has started.      |  
| Health               | Performs a variety of health checks on Nucleus components. The default endpoint is `/_health`. The *Health* check calls all implementations of the [IHealthCheck](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.ihealthcheck) interface.  Nucleus Core implements a number of health checks and extensions may provide additional health checks. |  

## Output
The output from health check endpoints depends on the value of the HTTP request `Accept` header.

{.table-25-75}
| Header Value         |                                                                                               |
|----------------------|-----------------------------------------------------------------------------------------------|
| text/html            | The output is a simple status (Healthy, Degraded or Unhealthy) in HTML - that is, wrapped with html and body tags.                                    |  
| application/health+json or application/json | The output is in JSON format and includes a detailed list of failed checks (see below). |  
| any other value      | The output is a simple status (Healthy, Degraded or Unhealthy) in text/plain format.           |  

> If you want to view the output from the Health endpoint, you will have to use a tool like [Postman](https://www.postman.com/), so that you can set
the value of the `Accept` header in your HTTP request. You can also use the `curl` command: 
<kbd>
curl https://your-site/_health --header "Accept:application/json"
</kbd>

> The application/health+json output is intended to conform with the draft specification [draft-inadarei-api-health-check-06](https://datatracker.ietf.org/doc/html/draft-inadarei-api-health-check-06). This 
specification has expired, but is the closest thing to a standard available. If the status is Degraded or Unhealthy, each check may contain a "additional-info" 
array with one or more elements which each contain a "summary" and "output" property.  This is consistent with the specification, which allows for 
"Additional Keys" in section 4.10.  

### Sample Output
```
{
  "status": "warn",
  "description": "Health of www.my-site.com",
  "version": "1",
  "releaseId": "1.4.0.0",
  "checks": {
    "Nucleus.Core.Services.HealthChecks.ApplicationReadyHealthCheck": [
      {
        "status": "pass",
        "componentType": "component",
        "time": "2024-02-14T04:00:10.0805868Z"
      }
    ],
    "Nucleus.Core.Services.HealthChecks.DatabaseConnectionsHealthCheck": [
      {
        "status": "pass",
        "componentType": "component",
        "time": "2024-02-14T04:00:10.0824630Z"
      }
    ],
    "Nucleus.Core.Services.HealthChecks.SearchProvidersHealthCheck": [
      {
        "status": "warn",
        "componentType": "component",
        "time": "2024-02-14T04:00:10.0824794Z",
        "output": "One or more search index provider connection tests failed.",
        "additional-info": [
          {
            "summary": "Connection Error",
            "output": "Search index provider 'Nucleus.Extensions.ElasticSearch.SearchIndexManager' returned an error when connecting using the settings for site 'Second Site', and will not receive data."
          }
        ]
      }
    ]
  }
}
```

## Built-in Health Checks

### Database Connections
The Database Connections health check tests database connectivity for each configured database.  If the connection test fails:
-  If the database belongs to the default schema `*`, an `Unhealthy` status is returned.  This is because Nucleus cannot function when the default 
database is not working.
-  If the database belongs to another schema `*`, a `Degraded` status is returned.  This is because Nucleus can generally function when a database that 
belongs to a specific extension (or set of extensions) is not working, although some parts of the system are likely to fail.

### Search Index Providers
The Search Index health check performs a connection test for each configured search index provider.  If the connection test fails,
a `Degraded` status is returned.  This is because Nucleus can function when search is not working.

## Configuration
Health checks must be enabled in order to function.  To enable health checks with default settings edit your `appSettings.Production.json`{.file-name} 
file, and add a section to the existing Nucleus section.

```
"Nucleus" {
  "HealthChecks": {
      "Enabled": true
    },
  ...
}
```

See also: [Configuration Files](https://www.nucleus-cms.com/configuration-files/)

Other (optional) settings for `Nucleus:HealthChecks` are:

{.table-25-75}
|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| LiveCheckEndPoint                | (string)  The local path of the live check endpoint.  The default is `/_live`.       |  
| ReadyCheckEndPoint               | (string)  The local path of the ready check endpoint.  The default is `/_ready`.     |  
| HealthCheckEndPoint              | (string)  The local path of the health check endpoint.  The default is `/_health`.   |  
| RequireRoles                     | (string)  A comma-delimited list of roles which can access health checks.  The default value is not set, which allows anonymous access. | 

> In order to use authentication with the RequireRoles setting, the caller must sign their HTTP request.  The [Api Keys](https://www.nucleus-cms.com/api-keys/) page
has more information.

```
"Nucleus" {
  "HealthChecks": {
    "Enabled": true,
    "HealthCheckEndPoint": "/_health",
    "LiveCheckEndPoint": "/_live",
    "ReadyCheckEndPoint": "/_ready",
    "RequireRoles": "System Monitors,Load Balancers"
    },
  ...
}
```