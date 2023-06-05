using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using System.IO;
using Nucleus.Abstractions;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Options;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides management functions for extensions.
	/// </summary>
	public class ExtensionManager : IExtensionManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ILogger<IExtensionManager> Logger { get; }
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		public ExtensionManager(IDataProviderFactory dataProviderFactory, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions, ILogger<IExtensionManager> logger)
		{
			this.DataProviderFactory = dataProviderFactory;
			this.FolderOptions = folderOptions;
			this.Logger = logger;
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a package (manifest) object as input.
		/// </summary>
		/// <param name="package"></param>
		/// <returns></returns>
		private ExtensionInstaller CreateInstaller(Abstractions.Models.Extensions.package package)
		{
			return new ExtensionInstaller(package, this, this.Logger, this.FolderOptions);			
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a stream as input. 
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private ExtensionInstaller CreateInstaller(Stream input)
		{
			return new ExtensionInstaller(input, this, this.Logger, this.FolderOptions);
		}

    public async Task<Nucleus.Abstractions.Models.Extensions.PackageResult> ValidatePackageContent(Stream input)
    {
      Nucleus.Abstractions.Models.Extensions.PackageResult result = new();

      using (ExtensionInstaller installer = CreateInstaller(input))
      {
        installer.DisableLocalChecks = true;
        if (await installer.IsValid())
        {
          result.IsValid = true;
          result.FileId = await installer.SaveTempFile();
          result.Package = await installer.GetPackage();

          result.Readme = await GetDocumentationFileContents(installer, "readme.txt", "readme.htm", "readme.md", "readme");
          result.License = await GetDocumentationFileContents(installer, "license.txt", "license.htm", "license.md", "license");
        }
        else
        {
          result.IsValid = false;
          result.Messages = installer.ModelState;
        }
      }

      return result;
    }

    /// <summary>
    /// Validate a package.
    /// </summary>
    /// <param name="tempFileName">
    /// The file name of a zip file to install.  The file must be stored in the Nucleus "Temp" folder.  tempFileName must not contain a path.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// The readme property in the returned <see cref="PackageResult"/> object is populated as a side-effect of this function, if a file named 
    /// readme.txt exists in the package.
    /// </remarks>
    public async Task<Nucleus.Abstractions.Models.Extensions.PackageResult> ValidatePackage(Stream input)
		{
			Nucleus.Abstractions.Models.Extensions.PackageResult result = new();

			using (ExtensionInstaller installer = CreateInstaller(input))
			{
				if (await installer.IsValid())
				{
					result.IsValid = true;
					result.FileId = await installer.SaveTempFile();
					result.Package = await installer.GetPackage();

					result.Readme = await GetDocumentationFileContents(installer, "readme.txt", "readme.htm", "readme.md", "readme");
					result.License = await GetDocumentationFileContents(installer, "license.txt", "license.htm", "license.md", "license");
				}
				else
				{
					result.IsValid = false;
					result.Messages = installer.ModelState;					
				}
			}

			return result;
		}

		private async Task<string> GetDocumentationFileContents(ExtensionInstaller installer, params string[] filenames)
		{
			foreach (string filename in filenames)
			{
				if (installer.FileExists(filename))
				{
					System.IO.Stream readmeStream = await installer.GetFileStream(filename);

					if (readmeStream != null)
					{
						string contentType;
						switch (System.IO.Path.GetExtension(filename).ToLower())
						{
							case ".md":
								contentType = "text/markdown";
								break;

							case ".txt":
							case "":
								contentType = "text/plain";
								break;

							case ".htm":
							case ".html":
							default:
								contentType = "text/html";
								break;
						}

						readmeStream.Position = 0;
						using (StreamReader reader = new(readmeStream))
						{
							return Nucleus.Extensions.ContentExtensions.ToHtml(reader.ReadToEnd(), contentType);							
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a temp file as input.
		/// </summary>
		/// <param name="tempFileName">
		/// The file name of a zip file to install.  The file must be stored in the Nucleus "Temp" folder.  tempFileName must not contain a path.
		/// </param>
		/// <returns></returns>
		public async Task InstallExtension(string tempFileName)
		{
			if (PathUtils.HasInvalidFileChars(tempFileName))
			{
				throw new ArgumentException("tempFileName has invalid characters.  tempFileName must be a simple filename and must not incude path information.", nameof(tempFileName));
			}

			using (Stream input = File.OpenRead(Path.Combine(this.FolderOptions.Value.GetTempFolder(), tempFileName)))
			{
				using (ExtensionInstaller installer = CreateInstaller(input))
				{
					await installer.InstallExtension();
				}
			}
		}

		public void UninstallExtension(Abstractions.Models.Extensions.package package)
		{
			using (ExtensionInstaller installer = CreateInstaller(package))
			{
				installer.UninstallExtension();
			}
		}


		/// <summary>
		/// Create or update a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public async Task SaveModuleDefinition(ModuleDefinition moduleDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SaveModuleDefinition(moduleDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="ModuleDefinition"></param>
		public async Task DeleteModuleDefinition(ModuleDefinition moduleDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeleteModuleDefinition(moduleDefinition);
			}
		}

		/// <summary>
		/// List the modules which use the specified module definition.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public async Task<IEnumerable<PageModule>> ListPageModules(ModuleDefinition moduleDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListPageModules(moduleDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public async Task SaveLayoutDefinition(LayoutDefinition layoutDefinition)
		{
      layoutDefinition.RelativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(layoutDefinition.RelativePath);

      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SaveLayoutDefinition(layoutDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public async Task DeleteLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeleteLayoutDefinition(layoutDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public async Task SaveContainerDefinition(ContainerDefinition containerDefinition)
		{
      containerDefinition.RelativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(containerDefinition.RelativePath);

      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider .SaveContainerDefinition(containerDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public async Task DeleteContainerDefinition(ContainerDefinition containerDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeleteContainerDefinition(containerDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="controlPanelExtensionDefinition"/> database record.
		/// </summary>
		/// <param name="controlPanelExtensionDefinition"></param>
		public async Task SaveControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SaveControlPanelExtensionDefinition(controlPanelExtensionDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="controlPanelExtensionDefinition"/> database record.
		/// </summary>
		/// <param name="controlPanelExtensionDefinition"></param>
		public async Task DeleteControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeleteControlPanelExtensionDefinition(controlPanelExtensionDefinition);
			}
		}

		public async Task<IEnumerable<ControlPanelExtensionDefinition>> ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes scope)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return (await provider.ListControlPanelExtensionDefinitions()).Where(ext => ext.Scope == scope);
			}
		}

		/// <summary>
		/// Delete assembly backups from all extension folders.
		/// </summary>
		public static void CleanupBackups(ILogger logger)
		{
			string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);

			if (System.IO.Directory.Exists(extensionsFolder))
			{
				foreach (string extensionFolder in System.IO.Directory.GetDirectories(extensionsFolder))
				{
					CleanupBackups(extensionFolder, logger);
				}
			}
		}

		/// <summary>
		/// Delete assembly backups from the specified extension folder.
		/// </summary>
		/// <param name="componentFolder"></param>
		private static void CleanupBackups(string componentFolder, ILogger logger)
		{
			foreach (string filename in System.IO.Directory.GetFiles(componentFolder))
			{
				if (System.IO.Path.GetExtension(filename).Equals(IExtensionManager.BACKUP_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						logger?.LogInformation("Deleting backup made during extension installation: {0}", filename);
						System.IO.File.Delete(filename);
					}
					catch (Exception ex)
					{
						// ignore non-critical error
						logger?.LogError(ex,"Unable to delete backup made during extension installation: {0}.", filename);
					}
				}
			}

			foreach (string foldername in System.IO.Directory.GetDirectories(componentFolder))
			{
				CleanupBackups(foldername, logger);
			}

			if (!System.IO.Directory.GetFileSystemEntries(componentFolder).Any())
			{
				// folder is empty, delete it
				System.IO.Directory.Delete(componentFolder);
			}
		}

		/// <summary>
		/// Save a stream to a randomly generated filename in the temp folder.
		/// </summary>
		/// <param name="fileStream"></param>
		/// <returns>The randomly-generated file name.</returns>
		public async Task<string> SaveTempFile(Stream fileStream)
		{
			string tempFileName = Guid.NewGuid().ToString();

			using (Stream output = File.OpenWrite(Path.Combine(this.FolderOptions.Value.GetTempFolder(), tempFileName)))
			{
				fileStream.Position = 0;
				await fileStream.CopyToAsync(output);
			}

			// clear out any old files
			foreach (string fileName in Directory.GetFiles(this.FolderOptions.Value.GetTempFolder()))
			{
				if (new FileInfo(fileName).LastWriteTime < DateTime.Now.AddHours(-1))
				{
					try
					{
						File.Delete(fileName);
					}
					catch (Exception ex)
					{
						// non-critical error
						this.Logger.LogInformation($"Unable to delete temp file {fileName}: {ex.Message}.");
					}
				}
			}

			return tempFileName;
		}

		/// <summary>
		/// Delete the specified file from the temp folder.
		/// </summary>
		/// <param name="tempFileName"></param>
		public void DeleteTempFile(string tempFileName)
		{			
			File.Delete(Path.Combine(this.FolderOptions.Value.GetTempFolder(), tempFileName));
		}

	}
}
