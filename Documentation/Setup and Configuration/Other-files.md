# Logging
Osa App Core includes a debug logger and a text file logger.  The debug logger write logs to an attached debugger.  

The Text File logger writes text file logs, which are stored in the `C:\ProgramData\Nucleus\Logs` folder by default, in files named dd-MMM-yyyy.log.  
Text file logs are automatically deleted after 7 days by default.

Scheduled task logs are intercepted by the text file logger and are also written to the `C:\ProgramData\Nucleus\Logs\Scheduled Tasks\{Task Name}` folder.

Logging is configured in `appSettings.json`.  Refer to the [Configure Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging#configure-logging)
section of the Microsoft ASP.NET core documentation for details.

# Caching
Some elements of Nucleus store cached results on disk, in order to improve performance.  Cached files are stored in the `C:\ProgramData\Nucleus\Cache`
folder, Cache files are deleted during startup, and are refreshed "on demand" when you run the application.