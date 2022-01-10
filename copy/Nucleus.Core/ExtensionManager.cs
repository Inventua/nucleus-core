using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using System.IO;
using Nucleus.Abstractions;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides management functions for extensions.
	/// </summary>
	public class ExtensionManager
	{
		private DataProviderFactory DataProviderFactory { get; }
		private ILogger<ExtensionManager> Logger { get; }
		public const string BACKUP_FILE_EXTENSION = ".backup";

		public ExtensionManager(DataProviderFactory dataProviderFactory, ILogger<ExtensionManager> logger)
		{
			this.DataProviderFactory = dataProviderFactory;
			this.Logger = logger;
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a temp file as input.
		/// </summary>
		/// <param name="tempFileName">
		/// The file name of a zip file to install.  The file must be stored in the Nucleus "Temp" folder.  tempFileName must not contain a path.
		/// </param>
		/// <returns></returns>
		public ExtensionInstaller CreateInstaller(string tempFileName)
		{
			if (PathUtils.HasInvalidFileChars(tempFileName))
			{
				throw new ArgumentException("tempFileName has invalid characters.  tempFileName must be a simple filename and must not incude path information.", nameof(tempFileName));
			}

			using (Stream input = File.OpenRead(Path.Combine(Folders.GetDataFolder(Folders.TEMP_FOLDER), tempFileName)))
			{
				return CreateInstaller(input);
			}
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a package (manifest) object as input.
		/// </summary>
		/// <param name="package"></param>
		/// <returns></returns>
		public ExtensionInstaller CreateInstaller(Abstractions.Models.Manifest.package package)
		{
			return new ExtensionInstaller(package, this);			
		}

		/// <summary>
		/// Create a new <see cref="ExtensionInstaller"/> using a stream as input. 
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public ExtensionInstaller CreateInstaller(Stream input)
		{
			return new ExtensionInstaller(input, this);
		}

		/// <summary>
		/// Create or update a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public void SaveModuleDefinition(ModuleDefinition moduleDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveModuleDefinition(moduleDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="ModuleDefinition"></param>
		public void DeleteModuleDefinition(ModuleDefinition moduleDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteModuleDefinition(moduleDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public void SaveLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveLayoutDefinition(layoutDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public void DeleteLayoutDefinition(LayoutDefinition layoutDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteLayoutDefinition(layoutDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public void SaveContainerDefinition(ContainerDefinition containerDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveContainerDefinition(containerDefinition);
			}
		}

		/// <summary>
		/// Create or update a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public void DeleteContainerDefinition(ContainerDefinition containerDefinition)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteContainerDefinition(containerDefinition);
			}
		}



		/// <summary>
		/// Delete assembly backups from all extension folders.
		/// </summary>
		public static void CleanupBackups(ILogger logger)
		{
			foreach (string extensionFolder in System.IO.Directory.GetDirectories(Nucleus.Abstractions.Folders.GetExtensionsFolder()))
			{
				Nucleus.Core.ExtensionManager.CleanupBackups(extensionFolder, logger);
			}
		}

		/// <summary>
		/// Delete assembly backups from the specified extension folder.
		/// </summary>
		/// <param name="componentFolder"></param>
		private static void CleanupBackups(string componentFolder, ILogger logger)
		{
			foreach (string filename in System.IO.Directory.GetFiles(Folders.GetExtensionFolder(componentFolder, false)))
			{
				if (System.IO.Path.GetExtension(filename).Equals(BACKUP_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
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

			foreach (string foldername in System.IO.Directory.GetDirectories(Folders.GetExtensionFolder(componentFolder, false)))
			{
				CleanupBackups(foldername, logger);
			}

			if (System.IO.Directory.GetFileSystemEntries(Folders.GetExtensionFolder(componentFolder, false)).Length==0)
			{
				// folder is empty, delete it
				System.IO.Directory.Delete(Folders.GetExtensionFolder(componentFolder, false));
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

			using (Stream output = File.OpenWrite(Path.Combine(Folders.GetDataFolder(Folders.TEMP_FOLDER), tempFileName)))
			{
				fileStream.Position = 0;
				await fileStream.CopyToAsync(output);
			}

			// clear out any old files
			foreach (string fileName in Directory.GetFiles(Folders.GetDataFolder(Folders.TEMP_FOLDER)))
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
		public static void DeleteTempFile(string tempFileName)
		{			
			File.Delete(Path.Combine(Folders.GetDataFolder(Folders.TEMP_FOLDER), tempFileName));
		}

	}
}
