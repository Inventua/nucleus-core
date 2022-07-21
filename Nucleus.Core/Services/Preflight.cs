using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Core.Services
{
	/// <summary>
	/// Class used to execute validation steps for the Nucleus environment.
	/// </summary>
	/// <internal>This is an internal class used by the setup wizard.</internal>
	public class Preflight : IPreflight
	{
		private IWebHostEnvironment WebHostEnvironment { get; }
		private Nucleus.Abstractions.Models.Configuration.FolderOptions FolderOptions { get; }
		private Nucleus.Abstractions.Models.Configuration.DatabaseOptions DatabaseOptions { get; }
		private IServiceProvider ServiceProvider { get; }

		/// <summary>
		/// Constructor used by dependency injection.
		/// </summary>
		public Preflight(IWebHostEnvironment webHostEnvironment, IServiceProvider serviceProvider, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions, IOptions<Nucleus.Abstractions.Models.Configuration.DatabaseOptions> databaseOptions)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.ServiceProvider = serviceProvider;
			this.FolderOptions = folderOptions.Value;
			this.DatabaseOptions = databaseOptions.Value;
		}

		/// <summary>
		/// Validate the environment and return validation results.
		/// </summary>
		/// <returns></returns>
		public IPreflight.ValidationResults Validate()
		{
			IPreflight.ValidationResults results = new();

			// check permissions on log, cache folders
			results.Add(CheckFolder("FOLDER-DATA", this.FolderOptions.GetDataFolder()));
			results.Add(CheckFolder("FOLDER-LOGS", this.FolderOptions.GetLogFolder(false)));
			results.Add(CheckFolder("FOLDER-CACHE", this.FolderOptions.GetCacheFolder(false)));
			results.Add(CheckFolder("FOLDER-TEMP", this.FolderOptions.GetTempFolder(false)));

			// check database connection
			results.AddRange(CheckDatabaseConnections());

			return results;
		}

		private string GetUserName()
		{
			return $"{System.Environment.UserDomainName}/{System.Environment.UserName}";
		}

		private IPreflight.ValidationResult CheckFolder(string code, string path)
		{
			Boolean folderExists;
			// ensure that the folder exists / create it
			try
			{
				folderExists = System.IO.Directory.Exists(path);
			}
			catch (Exception ex)
			{
				return new IPreflight.ValidationResult(code, IPreflight.Status.Error, $"Unable to check whether folder '{path}' exists: [user: {GetUserName()}]: {ex.Message}");
			}

			try
			{
				if (!folderExists)
				{
					System.IO.Directory.CreateDirectory(path);
				}
			}
			catch (Exception ex)
			{
				return new IPreflight.ValidationResult(code, IPreflight.Status.Error, $"Unable to create folder '{path}': [user: {GetUserName()}]: {ex.Message}");
			}

			// check read/write/delete
			try
			{
				System.IO.File.WriteAllText(System.IO.Path.Combine(path, "permissions-test.txt"), "ABCDEFG");
			}
			catch (Exception ex)
			{
				return new IPreflight.ValidationResult(code, IPreflight.Status.Error, $"Unable to write a file to folder '{path}': [user: {GetUserName()}]: {ex.Message}");
			}

			try
			{
				System.IO.File.Delete(System.IO.Path.Combine(path, "permissions-test.txt"));
			}
			catch (Exception ex)
			{
				return new IPreflight.ValidationResult(code, IPreflight.Status.Error, $"Unable to delete file 'permissions-test.txt' from folder '{path}': [user: {GetUserName()}]: {ex.Message}");
			}

			return new IPreflight.ValidationResult(code, IPreflight.Status.OK, $"Read, Write and Delete permissions for folder '{path}' OK.");
		}

		public IEnumerable<IPreflight.ValidationResult> CheckDatabaseConnections()
		{
			IEnumerable<Nucleus.Data.Common.DataProvider> dataProviders = this.ServiceProvider.GetServices<Nucleus.Data.Common.DataProvider>();
			List<IPreflight.ValidationResult> results = new();

			foreach (Nucleus.Data.Common.DataProvider dataProvider in dataProviders.DistinctBy(provider => provider.GetDatabaseKey()))
			{
				try
				{
					dataProvider.CheckConnection();
					results.Add(new IPreflight.ValidationResult("DATABASE", IPreflight.Status.OK, $"Connected to database {dataProvider.GetDatabaseKey()} OK."));
				}
				catch (Exception ex)
				{
					results.Add(new IPreflight.ValidationResult("DATABASE", IPreflight.Status.Error, $"Unable to connect to database '{dataProvider.GetDatabaseKey()}': {ex.Message} {ex.InnerException?.Message}"));
				}
			}

			return results;
		}
	}
}
