using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.ObjectModel;
using System.Runtime.Loader;
using Nucleus.Abstractions.Models.Cache;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class SystemIndex
	{
		public string Server { get; set; }
		public string Product { get; set; }

		public string Version { get; set; }

		public string Company { get; set; }

		public string Copyright { get; set; }

		public string Framework { get; set; }

		public string OperatingSystem { get; set; }

		public string OperatingSystemUser { get; set; }

		public DateTime StartTime { get; set; }
		public string Uptime { get; set; }
		public string EnvironmentName { get; set; }
    public double CpuUsedPercentage { get; set; }
    public double MemoryUsedPercentage { get; set; }

    public LogSettingsViewModel LogSettings { get; set; } = new() { HasLoggingConfiguration = true };

		public string Configuration { get; set; }

		public IEnumerable<Nucleus.Abstractions.Models.TaskScheduler.RunningTask> RunningTasks { get; set; }

    public AssemblyLoadContext[] ExtensionLoadContexts { get; set; }
    public string ContentRootPath { get; set; }
    public List<CacheReport> CacheReport { get; set; }

    public IEnumerable<DatabaseConnection> DatabaseConnections { get; set; }
		public long UsersOnline { get; set; }

		public Dictionary<string, string> WebServerInformation { get; set; } = new();

		public List<LogSetting> LoggingSettingsConfiguration { get; set; } = new();
    public LogSetting NewSetting { get; set; } = new() { Level = LogLevel.None };


    public SystemIndex()
		{
		}

    public class LogSetting
    {
      public string Category { get; set; }
      public Microsoft.Extensions.Logging.LogLevel Level { get; set; }
    }

		public class LogSettingsViewModel
		{
			public string ControllerName { get; set; }
			public Boolean HasLoggingConfiguration { get; set; } = false;


      public Boolean LogSortDescending { get; set; } = true;
			public string LogFilterTerm { get; set; }
			public Boolean LogIncludeInformation { get; set; } = true;
			public Boolean LogIncludeWarning { get; set; } = true;
      public Boolean LogIncludeDebug { get; set; } = true;

      public Boolean LogIncludeTrace { get; set; } = true;
			public Boolean LogIncludeError { get; set; } = true;
      
      public List<KeyValuePair<Boolean, string>> LogSortOrders { get; } = new() { new(true, "Sort Descending"), new(false, "Sort Ascending") };

			public List<Shared.LogFileInfo> LogFiles { get; set; }

			public string LogFile { get; set; }

			public Nucleus.Abstractions.Models.Paging.PagedResult<LogEntry> LogContent { get; set; } = new() { PageSize = 100, PageSizes = new List<int>() { 100, 250, 500 } };

			public string LogMessage { get; set; }
		}

		public class LogEntry
		{
			public DateTime Date { get; set; }
			public string Level { get; set; }
			public string Category { get; set; }
			public string Message { get; set; }
			public Boolean IsValid { get; set; } = true;
			private string Raw { get; set; }

			public LogEntry(string input)
			{
				this.Raw=input;	

				string[] parts = input.Split(',');

				if (parts.Length >= 4)
				{
					if (DateTime.TryParse(parts[0], out DateTime result))
					{
						this.Date = result;
					}
					this.Level = parts[1];
					this.Category = parts[2];

					// Message could contain commas, so we just use the rest of the string
					this.Message = input.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 3);
				}
				else
				{
					// Prevent lines that aren't in the correct delimited form from being displayed.
					this.IsValid = false;
				}
			}

			public Boolean IsMatch(ViewModels.Admin.SystemIndex.LogSettingsViewModel logSettings)
			{
				if (!IsLevelSelected(logSettings))
				{
					return false;
				}
				else
				{
					return
						String.IsNullOrEmpty(logSettings.LogFilterTerm) ||
						this.Category?.Contains(logSettings.LogFilterTerm, StringComparison.OrdinalIgnoreCase) == true ||
						this.Message?.Contains(logSettings.LogFilterTerm, StringComparison.OrdinalIgnoreCase) == true;
				}
			}

			private Boolean IsLevelSelected(ViewModels.Admin.SystemIndex.LogSettingsViewModel logSettings)
			{
				switch (this.Level)
				{
					case nameof(LogLevel.Trace):
						return logSettings.LogIncludeTrace;
					case nameof(LogLevel.Information):
						return logSettings.LogIncludeInformation;
					case nameof(LogLevel.Debug):
            return logSettings.LogIncludeDebug;
          case nameof(LogLevel.Error):
						return logSettings.LogIncludeError;
					case nameof(LogLevel.Warning):
						return logSettings.LogIncludeWarning;
          default:
						return true;
				}
			}

			public override string ToString()
			{
				return this.Raw;
			}
		}

		public class DatabaseConnection
		{
			public string Schema { get; set; }

			public string DatabaseType { get; set; }

			public string ConnectionString { get; set; }

			public Dictionary<string, string> DatabaseInformation { get; set; } = new();
		}

		// https://github.com/projectkudu/kudu/wiki/Perf-Counters-exposed-as-environment-variables
		// Azure seems to be populating the counter environment variables, but the values are all zero, so not useful.

		//public class AzureCounterData
		//{
		//	

		//	public AspNet aspNet { get; set; }
		//	public App app { get; set; }
		//	public Clr clr { get; set; }
		//	public string aspNetTest { get; set; } = System.Environment.GetEnvironmentVariable("WEBSITE_COUNTERS_ASPNET");
		//	public string appTest { get; set; } = System.Environment.GetEnvironmentVariable("WEBSITE_COUNTERS_APP");
		//	public string clrTest { get; set; } = System.Environment.GetEnvironmentVariable("WEBSITE_COUNTERS_CLR");

		//	public	static AzureCounterData Create()
		//	{
		//		AzureCounterData result = new();
		//		result.aspNet = Deserialize<AspNet>("WEBSITE_COUNTERS_ASPNET");
		//		result.app = Deserialize<App>("WEBSITE_COUNTERS_APP");
		//		result.clr = Deserialize<Clr>("WEBSITE_COUNTERS_CLR");

		//		return (result.aspNet == null || result.app == null || result.clr == null) ? null : result;				
		//	}

		//	private static T Deserialize<T>(string environmentVariableName)
		//	{
		//		string counterData = System.Environment.GetEnvironmentVariable(environmentVariableName);

		//		if (!String.IsNullOrEmpty(counterData))
		//		{
		//			return System.Text.Json.JsonSerializer.Deserialize<T>(counterData);
		//		}

		//		return default(T);
		//	}

		//	public class App
		//	{
		//		public long userTime { get; set; }
		//		public long kernelTime { get; set; }
		//		public long pageFaults { get; set; }
		//		public long processes { get; set; }
		//		public long processLimit { get; set; }
		//		public long threads { get; set; }
		//		public long threadLimit { get; set; }
		//		public long connections { get; set; }
		//		public long connectionLimit { get; set; }
		//		public long sections { get; set; }
		//		public long sectionLimit { get; set; }
		//		public long namedPipes { get; set; }
		//		public long namedPipeLimit { get; set; }
		//		public long readIoOperations { get; set; }
		//		public long writeIoOperations { get; set; }
		//		public long otherIoOperations { get; set; }
		//		public long readIoBytes { get; set; }
		//		public long writeIoBytes { get; set; }
		//		public long otherIoBytes { get; set; }
		//		public long privateBytes { get; set; }
		//		public long handles { get; set; }
		//		public long contextSwitches { get; set; }
		//		public long remoteOpens { get; set; }
		//		public long remoteWrites { get; set; }
		//		public long remoteWriteKBs { get; set; }
		//	}

		//	public class AspNet
		//	{
		//		public long applicationRestarts { get; set; }
		//		public long applicationsRunning { get; set; }
		//		public long requestsDisconnected { get; set; }
		//		public long requestExecutionTime { get; set; }
		//		public long requestsRejected { get; set; }
		//		public long requestsQueued { get; set; }
		//		public long wpsRunning { get; set; }
		//		public long wpsRestarts { get; set; }
		//		public long requestWaitTime { get; set; }
		//		public long requestsCurrent { get; set; }
		//		public long globalAuditSuccess { get; set; }
		//		public long globalAuditFail { get; set; }
		//		public long globalEventsError { get; set; }
		//		public long globalEventsHttpReqError { get; set; }
		//		public long globalEventsHttpInfraError { get; set; }
		//		public long requestsInNativeQueue { get; set; }
		//		public long anonymousRequests { get; set; }
		//		public long totalCacheEntries { get; set; }
		//		public long totalCacheTurnoverRate { get; set; }
		//		public long totalCacheHits { get; set; }
		//		public long totalCacheMisses { get; set; }
		//		public long totalCacheRatioBase { get; set; }
		//		public long apiCacheEntries { get; set; }
		//		public long apiCacheTurnoverRate { get; set; }
		//		public long apiCacheHits { get; set; }
		//		public long apiCacheMisses { get; set; }
		//		public long apiCacheRatioBase { get; set; }
		//		public long outputCacheEntries { get; set; }
		//		public long outputCacheTurnoverRate { get; set; }
		//		public long outputCacheHits { get; set; }
		//		public long outputCacheMisses { get; set; }
		//		public long outputCacheRatioBase { get; set; }
		//		public long compilations { get; set; }
		//		public long debuggingRequests { get; set; }
		//		public long errorsPreProcessing { get; set; }
		//		public long errorsCompiling { get; set; }
		//		public long errorsDuringRequest { get; set; }
		//		public long errorsUnhandled { get; set; }
		//		public long errorsTotal { get; set; }
		//		public long pipelines { get; set; }
		//		public long requestBytesIn { get; set; }
		//		public long requestBytesOut { get; set; }
		//		public long requestsExecuting { get; set; }
		//		public long requestsFailed { get; set; }
		//		public long requestsNotFound { get; set; }
		//		public long requestsNotAuthorized { get; set; }
		//		public long requestsInApplicationQueue { get; set; }
		//		public long requestsTimedOut { get; set; }
		//		public long requestsSucceded { get; set; }
		//		public long requestsTotal { get; set; }
		//		public long sessionsActive { get; set; }
		//		public long sessionsAbandoned { get; set; }
		//		public long sessionsTimedOut { get; set; }
		//		public long sessionsTotal { get; set; }
		//		public long transactionsAborted { get; set; }
		//		public long transactionsCommitted { get; set; }
		//		public long transactionsPending { get; set; }
		//		public long transactionsTotal { get; set; }
		//		public long sessionStateServerConnections { get; set; }
		//		public long sessionSqlServerConnections { get; set; }
		//		public long eventsTotal { get; set; }
		//		public long eventsApp { get; set; }
		//		public long eventsError { get; set; }
		//		public long eventsHttpReqError { get; set; }
		//		public long eventsHttpInfraError { get; set; }
		//		public long eventsWebReq { get; set; }
		//		public long auditSuccess { get; set; }
		//		public long auditFail { get; set; }
		//		public long memberSuccess { get; set; }
		//		public long memberFail { get; set; }
		//		public long formsAuthSuccess { get; set; }
		//		public long formsAuthFail { get; set; }
		//		public long viewstateMacFail { get; set; }
		//		public long appRequestExecTime { get; set; }
		//		public long appRequestDisconnected { get; set; }
		//		public long appRequestsRejected { get; set; }
		//		public long appRequestWaitTime { get; set; }
		//		public long cachePercentMachMemLimitUsed { get; set; }
		//		public long cachePercentMachMemLimitUsedBase { get; set; }
		//		public long cachePercentProcMemLimitUsed { get; set; }
		//		public long cachePercentProcMemLimitUsedBase { get; set; }
		//		public long cacheTotalTrims { get; set; }
		//		public long cacheApiTrims { get; set; }
		//		public long cacheOutputTrims { get; set; }
		//		public long appCpuUsed { get; set; }
		//		public long appCpuUsedBase { get; set; }
		//		public long appMemoryUsed { get; set; }
		//		public long requestBytesInWebsockets { get; set; }
		//		public long requestBytesOutWebsockets { get; set; }
		//		public long requestsExecutingWebsockets { get; set; }
		//		public long requestsFailedWebsockets { get; set; }
		//		public long requestsSucceededWebsockets { get; set; }
		//		public long requestsTotalWebsockets { get; set; }
		//	}

		//	public class Clr
		//	{
		//		public long bytesInAllHeaps { get; set; }
		//		public long gcHandles { get; set; }
		//		public long gen0Collections { get; set; }
		//		public long gen1Collections { get; set; }
		//		public long gen2Collections { get; set; }
		//		public long inducedGC { get; set; }
		//		public long pinnedObjects { get; set; }
		//		public long committedBytes { get; set; }
		//		public long reservedBytes { get; set; }
		//		public long timeInGC { get; set; }
		//		public long timeInGCBase { get; set; }
		//		public long allocatedBytes { get; set; }
		//		public long gen0HeapSize { get; set; }
		//		public long gen1HeapSize { get; set; }
		//		public long gen2HeapSize { get; set; }
		//		public long largeObjectHeapSize { get; set; }
		//		public long currentAssemblies { get; set; }
		//		public long currentClassesLoaded { get; set; }
		//		public long exceptionsThrown { get; set; }
		//		public long appDomains { get; set; }
		//		public long appDomainsUnloaded { get; set; }
		//	}

		//}


	}
}
