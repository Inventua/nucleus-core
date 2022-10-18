using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml;
using Nucleus.Extensions;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides methods to validate and install an extension.
	/// </summary>
	public class ExtensionInstaller : IDisposable
	{		
		private ZipArchive Archive { get; }
		private Stream ArchiveStream { get; }
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }
		private Abstractions.Models.Extensions.package Package { get; set; }
		private ILogger<IExtensionManager> Logger { get; }

		public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get; } = new() { MaxAllowedErrors = int.MaxValue};

		private IExtensionManager ExtensionManager { get; }

		/// <summary>
		/// Create a new installer 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="extensionManager"></param>
		/// <remarks>
		/// This constructor is called by the <see cref="ExtensionManager"/> class.  
		/// </remarks>
		internal ExtensionInstaller(Stream input, IExtensionManager extensionManager, ILogger<IExtensionManager> logger, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			this.FolderOptions = folderOptions;

			this.ArchiveStream = new MemoryStream();
			input.CopyTo(this.ArchiveStream);
			this.ArchiveStream.Position = 0;

			this.Archive = OpenZipArchive(this.ArchiveStream);

			this.ExtensionManager = extensionManager;
			this.Logger = logger;
		}

		/// <summary>
		/// Create a new installer 
		/// </summary>
		/// <param name="package"></param>
		/// <param name="extensionManager"></param>
		/// <remarks>
		/// This constructor is called by the <see cref="ExtensionManager"/> class.  
		/// </remarks>
		internal ExtensionInstaller(Abstractions.Models.Extensions.package package, IExtensionManager extensionManager, ILogger<IExtensionManager> logger)
		{
			this.Package = package;
			this.ExtensionManager = extensionManager;
			this.Logger = logger;
		}

		private static ZipArchive OpenZipArchive(Stream input)
		{
			return new ZipArchive(input);
		}

		public async Task<string> SaveTempFile()
		{
			return await this.ExtensionManager.SaveTempFile(GetArchiveFileStream());
		}

		/// <summary>
		/// Get the copy of the archive stream made when the constructor was called.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// A copy is made in the constructor so that we can save the stream after validation.  The passed-in IFormFile stream
		/// does not support seek operations, so it can't be "re-used" after being read once.
		/// </remarks>
		private Stream GetArchiveFileStream()
		{
			this.ArchiveStream.Position = 0;
			return this.ArchiveStream;
		}

		/// <summary>
		/// Return true if the file exists in the package, false otherwise
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public Boolean FileExists(string fileName)
		{
			return this.Archive.Entries.Where(entry => entry.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)).Any();
		}

		/// <summary>
		/// Get the manifest file (package.xml) from the archive.
		/// </summary>
		/// <returns></returns>
		public async Task<Stream> GetFileStream(string fileName)
		{
			System.IO.MemoryStream output = new();

			// find the manifest			
			ZipArchiveEntry manifestEntry = null;

			manifestEntry = this.Archive.Entries.Where(entry => entry.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			
			if (manifestEntry == null)
			{
				this.ModelState.AddModelError($"missingfile:{fileName}", $"File '{fileName}' not found in package.");
				throw new InvalidDataException($"{fileName} not found.");
			}
			else
			{
				using (Stream input = manifestEntry.Open())
				{
					await input.CopyToAsync(output);
					output.Position = 0;
				}
			}			

			return output;
		}

		/// <summary>
		/// Validate the archive by checking that the manifest file is valid and all of the files and folders references by the manifest
		/// are present in the archive.
		/// </summary>
		/// <returns></returns>
		public async Task<Boolean> IsValid()
		{
			if (this.Package != null) return true;

			if (await this.IsManifestValid())
			{
				return (this.IsPackageValid(await this.GetPackage()));
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Validate that the manifest (package.xml) conforms to the package.xsd XML schema.
		/// </summary>
		/// <returns></returns>
		private async Task<Boolean> IsManifestValid()
		{
			System.Xml.XmlDocument manifestDocument;
			System.IO.Stream manifestStream = await GetFileStream(IExtensionManager.PACKAGE_MANIFEST_FILENAME);

			manifestDocument = new XmlDocument();
			manifestDocument.Schemas.Add("urn:nucleus/schemas/package/1.0", "package.xsd");

			manifestStream.Position = 0;
			manifestDocument.Load(manifestStream);

			try
			{
				manifestDocument.Validate(null);
				return true;
			}
			catch (System.Xml.Schema.XmlSchemaValidationException validationEx)
			{
				this.ModelState.AddModelError("manifest-notvalid", $"Manifest validation error: {validationEx.Message}.");
				return false;
			}
		}

		/// <summary>
		/// Parse the manifest and return it as a package object.
		/// </summary>
		/// <returns></returns>
		public async Task<Abstractions.Models.Extensions.package> GetPackage()
		{
			if (this.Package == null)
			{
				System.IO.Stream manifestStream = await GetFileStream(IExtensionManager.PACKAGE_MANIFEST_FILENAME);
				System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.package));

				manifestStream.Position = 0;
				this.Package = (Abstractions.Models.Extensions.package)serializer.Deserialize(manifestStream);
			}

			return this.Package;
		}
				
		/// <summary>
		/// Install the extension
		/// </summary>
		/// <returns></returns>
		public async Task<Boolean> InstallExtension()
		{
			// manifest is valid, extract data
			if (await this.IsManifestValid())
			{
				Abstractions.Models.Extensions.package package = await GetPackage();

				if (this.IsPackageValid(package))
				{					
					foreach (Nucleus.Abstractions.Models.Extensions.component component in package.components)
					{
						// install the files
						
						// top level files
						foreach (Nucleus.Abstractions.Models.Extensions.file file in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.file>())
						{
							CopyFile(component.folderName, "", file);
						}

						foreach (Nucleus.Abstractions.Models.Extensions.folder folder in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.folder>())
						{
							CopyFolder(component.folderName, folder.name, folder);
						}

						// The package file is not included in the manifest, so we have to install it separately
						Nucleus.Abstractions.Models.Extensions.file packageFile = new();
						packageFile.name = IExtensionManager.PACKAGE_MANIFEST_FILENAME;
						CopyFile(component.folderName, "", packageFile);

						// apply module definitions
						foreach (Nucleus.Abstractions.Models.Extensions.moduleDefinition moduleDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.moduleDefinition>())
						{							
							ModuleDefinition moduleDefinition = new() 
							{
								Id = Guid.Parse(moduleDef.id),
								FriendlyName = moduleDef.friendlyName,
								Extension = moduleDef.extension,
								ViewController = moduleDef.viewController,
								SettingsController = moduleDef.settingsController,
								ViewAction = moduleDef.viewAction,
								EditAction = moduleDef.editAction,
								Categories = moduleDef.categories
							};

							await this.ExtensionManager.SaveModuleDefinition(moduleDefinition);				
						}

						// apply control panel extensions
						foreach (Nucleus.Abstractions.Models.Extensions.controlPanelExtensionDefinition controlPanelExtensionDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.controlPanelExtensionDefinition>())
						{
							ControlPanelExtensionDefinition extensionDefinition = new()
							{
								Id = Guid.Parse(controlPanelExtensionDef.id),
								FriendlyName = controlPanelExtensionDef.friendlyName,								  
								Description = controlPanelExtensionDef.description,
								ControllerName = controlPanelExtensionDef.controllerName,
								ExtensionName = controlPanelExtensionDef.extensionName,
								Scope = (ControlPanelExtensionDefinition.ControlPanelExtensionScopes)System.Enum.Parse(typeof(ControlPanelExtensionDefinition.ControlPanelExtensionScopes), controlPanelExtensionDef.scope.ToString(), true),
								EditAction = controlPanelExtensionDef.editAction
							};

							await this.ExtensionManager.SaveControlPanelExtensionDefinition(extensionDefinition);
						}

						// apply layouts
						foreach (Nucleus.Abstractions.Models.Extensions.layoutDefinition layoutDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.layoutDefinition>())
						{

							LayoutDefinition layoutDefinition = new()
							{
								Id = Guid.Parse(layoutDef.id),
								FriendlyName = layoutDef.friendlyName,
								RelativePath = ParseRelativePath(component.folderName, layoutDef.relativePath)
							};

							await this.ExtensionManager.SaveLayoutDefinition(layoutDefinition);
						}

						// apply containers
						foreach (Nucleus.Abstractions.Models.Extensions.containerDefinition containerDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.containerDefinition>())
						{

							ContainerDefinition containerDefinition = new()
							{
								Id = Guid.Parse(containerDef.id),
								FriendlyName = containerDef.friendlyName,
								RelativePath = ParseRelativePath(component.folderName, containerDef.relativePath)
							};

							await this.ExtensionManager.SaveContainerDefinition(containerDefinition);
						}

						// apply cleanup steps
						foreach (Nucleus.Abstractions.Models.Extensions.cleanup cleanup in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.cleanup>())
						{
							foreach (Nucleus.Abstractions.Models.Extensions.containerDefinition containerDef in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.containerDefinition>())
							{
								ContainerDefinition containerDefinition = new()
								{
									Id = Guid.Parse(containerDef.id),
									FriendlyName = containerDef.friendlyName,
									RelativePath = ParseRelativePath(component.folderName, containerDef.relativePath)
								};

								await this.ExtensionManager.DeleteContainerDefinition(containerDefinition);
							}

							foreach (Nucleus.Abstractions.Models.Extensions.layoutDefinition layoutDef in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.layoutDefinition>())
							{
								LayoutDefinition layoutDefinition = new()
								{
									Id = Guid.Parse(layoutDef.id),
									FriendlyName = layoutDef.friendlyName,
									RelativePath = ParseRelativePath(component.folderName, layoutDef.relativePath)
								};

								await this.ExtensionManager.DeleteLayoutDefinition(layoutDefinition);
							}

							foreach (Nucleus.Abstractions.Models.Extensions.moduleDefinition moduleDef in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.moduleDefinition>())
							{
								ModuleDefinition moduleDefinition = new()
								{
									Id = Guid.Parse(moduleDef.id),
									FriendlyName = moduleDef.friendlyName,
									Extension = moduleDef.extension,
									ViewController = moduleDef.viewController,
									SettingsController = moduleDef.settingsController,
									ViewAction = moduleDef.viewAction,
									EditAction = moduleDef.editAction,
									Categories = moduleDef.categories
								};

								await this.ExtensionManager.DeleteModuleDefinition(moduleDefinition);
							}
							
							foreach (Nucleus.Abstractions.Models.Extensions.controlPanelExtensionDefinition controlPanelExtensionDef in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.controlPanelExtensionDefinition>())
							{
								ControlPanelExtensionDefinition extensionDefinition = new()
								{
									Id = Guid.Parse(controlPanelExtensionDef.id),
									FriendlyName = controlPanelExtensionDef.friendlyName,
									Description = controlPanelExtensionDef.description,
									ControllerName = controlPanelExtensionDef.controllerName,
									ExtensionName = controlPanelExtensionDef.extensionName,
									Scope = (ControlPanelExtensionDefinition.ControlPanelExtensionScopes)System.Enum.Parse(typeof(ControlPanelExtensionDefinition.ControlPanelExtensionScopes), controlPanelExtensionDef.scope.ToString(), true),
									EditAction = controlPanelExtensionDef.editAction
								};

								await this.ExtensionManager.DeleteControlPanelExtensionDefinition(extensionDefinition);
							}

							foreach (Nucleus.Abstractions.Models.Extensions.file file in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.file>())
							{
								DeleteFile(component.folderName, file, null);
							}

							foreach (Nucleus.Abstractions.Models.Extensions.folder folder in cleanup.Items.OfType<Nucleus.Abstractions.Models.Extensions.folder>())
							{
								DeleteFolder(component.folderName, folder);
							}
						}
					}
				}
			}

			return true;
		}

		private string ParseRelativePath(string extensionFolder, string relativePath)
		{
			if (relativePath.StartsWith("Extensions", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith("Shared", StringComparison.OrdinalIgnoreCase))
			{
				return relativePath;
			}
			else
			{
				return System.IO.Path.Combine(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, extensionFolder, relativePath);
			}
		}

		/// <summary>
		/// Uninstall an extension
		/// </summary>
		/// <param name="Folder"></param>
		/// <remarks>
		/// Assemblies (dlls) are renamed rather than deleted, and the CleanupBackups function runs next time the application starts,
		/// removes the backups, as well as any empty folders.
		/// </remarks>
		public Boolean UninstallExtension()
		{
			foreach (Nucleus.Abstractions.Models.Extensions.component component in this.Package.components)
			{
				// uninstall the files

				// top level files
				foreach (Nucleus.Abstractions.Models.Extensions.file file in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.file>())
				{
					DeleteFile(component.folderName, file, null);
				}

				foreach (Nucleus.Abstractions.Models.Extensions.folder folder in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.folder>())
				{
					DeleteFolder(component.folderName, folder);
				}

				// The package file is not included in the manifest, so we have to delete it separately
				Nucleus.Abstractions.Models.Extensions.file packageFile = new();
				packageFile.name = IExtensionManager.PACKAGE_MANIFEST_FILENAME;
				DeleteFile(component.folderName, packageFile, null);

				// delete module definitions
				foreach (Nucleus.Abstractions.Models.Extensions.moduleDefinition moduleDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.moduleDefinition>())
				{

					ModuleDefinition moduleDefinition = new()
					{
						Id = Guid.Parse(moduleDef.id),
						FriendlyName = moduleDef.friendlyName,
						Extension = moduleDef.extension,
						ViewController = moduleDef.viewController,
						SettingsController = moduleDef.settingsController,
						ViewAction = moduleDef.viewAction,
						EditAction = moduleDef.editAction,
						Categories = moduleDef.categories
					};

					this.ExtensionManager.DeleteModuleDefinition(moduleDefinition);
				}

				// delete layouts
				foreach (Nucleus.Abstractions.Models.Extensions.layoutDefinition layoutDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.layoutDefinition>())
				{

					LayoutDefinition layoutDefinition = new()
					{
						Id = Guid.Parse(layoutDef.id),
						FriendlyName = layoutDef.friendlyName,
						RelativePath = ParseRelativePath(component.folderName, layoutDef.relativePath)
					};

					this.ExtensionManager.DeleteLayoutDefinition(layoutDefinition);
				}

				// delete containers
				foreach (Nucleus.Abstractions.Models.Extensions.containerDefinition containerDef in component.Items.OfType<Nucleus.Abstractions.Models.Extensions.containerDefinition>())
				{

					ContainerDefinition containerDefinition = new()
					{
						Id = Guid.Parse(containerDef.id),
						FriendlyName = containerDef.friendlyName,
						RelativePath = ParseRelativePath(component.folderName, containerDef.relativePath)
					};

					this.ExtensionManager.DeleteContainerDefinition(containerDefinition);
				}
			}


			return true;
		}

		/// <summary>
		/// Validate that all of the files and folders referenced by the manifest are present in the archive.
		/// </summary>
		/// <param name="package"></param>
		/// <returns></returns>
		private Boolean IsPackageValid(Nucleus.Abstractions.Models.Extensions.package package)
		{
			if (package.compatibility != null)
			{
				System.Version appVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

				//Testing
				//var result1 = System.Version.Parse("1.0.0.0").IsGreaterThan("*");
				//var result2 = System.Version.Parse("1.0.0.0").IsLessThan("*");

				//var result3 = System.Version.Parse("2.0.0.0").IsGreaterThan("1.*");
				//var result4 = System.Version.Parse("2.0.0.0").IsLessThan("1.*");
				
				//var result5 = System.Version.Parse("1.0.0.0").IsGreaterThan("2.*");
				//var result6 = System.Version.Parse("1.0.0.0").IsLessThan("2.*");

				// check version compatibility
				if (appVersion.IsLessThan(package.compatibility.minVersion))
				{
					//throw new InvalidDataException($"This extension is not compatible with Nucleus version {appVersion}.");
					this.ModelState.AddModelError("compatibility-minversion", $"This extension is not compatible with Nucleus version {appVersion} (requires version {package.compatibility.minVersion} or later).");
					return false;
				}

				if (!String.IsNullOrEmpty(package.compatibility.maxVersion) && appVersion.IsGreaterThan(package.compatibility.maxVersion))
				{
					this.ModelState.AddModelError("compatibility-maxversion", $"This extension is not compatible with Nucleus version {appVersion} (not compatible with versions after {package.compatibility.maxVersion}).");
					return false;
				}

				// Validate that referenced files are present in the package
				foreach (Nucleus.Abstractions.Models.Extensions.component component in package.components)
				{
					if (System.IO.Path.IsPathRooted(component.folderName) || PathUtils.PathNavigatesAboveRoot(component.folderName) || PathUtils.HasInvalidPathChars(component.folderName))
					{
						throw new InvalidOperationException($"Component folder name '{component.folderName}' is invalid.");
					}

					// top level files
					foreach (Nucleus.Abstractions.Models.Extensions.file file in component.Items.OfType<Abstractions.Models.Extensions.file>())
					{
						ValidateFile(component.folderName, "", file);
					}

					foreach (Nucleus.Abstractions.Models.Extensions.folder folder in component.Items.OfType<Abstractions.Models.Extensions.folder>())
					{
						ValidateFolder(component.folderName, folder.name, folder);						
					}
				}
			}

			return this.ModelState.IsValid;
		}

		/// <summary>
		/// Check that the specified folder's files exist in the archive
		/// </summary>
		/// <param name="componentFolder"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		private Boolean ValidateFolder(string componentFolder, string path, Abstractions.Models.Extensions.folder folder)
		{
			Boolean result = true;

			foreach (Abstractions.Models.Extensions.file file in folder.Items.OfType<Abstractions.Models.Extensions.file>())
			{
				if (!ValidateFile(componentFolder, path, file))
				{
					result = false;
				}
			}

			foreach (Nucleus.Abstractions.Models.Extensions.folder subfolder in folder.Items.OfType<Nucleus.Abstractions.Models.Extensions.folder>())
			{
				if (System.IO.Path.IsPathRooted(subfolder.name) || PathUtils.PathNavigatesAboveRoot(subfolder.name) || PathUtils.HasInvalidPathChars(subfolder.name))
				{
					throw new InvalidOperationException($"Folder name '{subfolder.name}' is invalid.");
				}
				
				ValidateFolder(componentFolder, path + "/" + subfolder.name, subfolder);
			}

			return result;
		}

		/// <summary>
		/// Check that the specified file exists in the archive.
		/// </summary>
		/// <param name="componentFolder"></param>
		/// <param name="file"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		private Boolean ValidateFile(string componentFolder, string path, Abstractions.Models.Extensions.file file)
		{
			string zipFullName;
			ZipArchiveEntry entry;

			if (!String.IsNullOrEmpty(path))
			{
				// Note: Zip file paths (.FullName) always use "/" to separate folder paths, per ".ZIP File Format Specification" 4.4.17.1. [https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT]
				//zipFullName = $"{folder.name}/{file.name}";
				zipFullName = $"{path}/{file.name}";
			}
			else
			{
				zipFullName = file.name;
			}

			entry = this.Archive.Entries.Where((entry) => entry.FullName.Equals(zipFullName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();

			if (entry != null)
			{
				// for assemblies, if the target already exists, make sure we aren't overwriting a newer version
				if (Path.GetExtension(entry.Name).Equals(".dll", StringComparison.OrdinalIgnoreCase))
				{
					if (System.IO.File.Exists(BuildExtensionFilePath(componentFolder, entry.FullName)))
					{
						try
						{
							System.Version newAssemblyVersion;
							System.Version existingAssemblyVersion;

							// Use an assembly load context which we unload after comparing the versions so that the assemblies don't remain loaded 
							Nucleus.Core.Plugins.PluginLoadContext context;

							// Load the new assembly and retrieve its version.
							using (Stream stream = entry.Open())
							{
								context = new("extension-installer-temp1", BuildExtensionFilePath(componentFolder, ""));
								System.Reflection.Assembly newAssembly = context.LoadFromStream(stream);
								newAssemblyVersion = newAssembly.GetName().Version;
								stream.Close();
								context.Unload();
							}

							// Load the existing assembly and retrieve its version
							context = new("extension-installer-temp2", BuildExtensionFilePath(componentFolder, ""));
							System.Reflection.Assembly existingAssembly = context.LoadFromAssemblyPath(BuildExtensionFilePath(componentFolder, entry.FullName));
							existingAssemblyVersion = existingAssembly.GetName().Version;
							context.Unload();
							
							if (existingAssemblyVersion > newAssemblyVersion)
							{
								this.ModelState.AddModelError($"validate-file:{zipFullName}", $"A newer version of the assembly {zipFullName} is already installed.");
								return false;
							}
						}
						catch (Exception)
						{
							// Not all .dll files are assemblies.  If we get an exception trying to read a DLL as an assembly, ignore it
						}
					}
				}
			}
			else
			{
				this.ModelState.AddModelError($"validate-file:{zipFullName}", $"A file is missing from the archive: {zipFullName}.");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Copy a folder and its files to the specified folder.
		/// </summary>
		/// <param name="componentFolder">Source folder.</param>
		/// <param name="folder">Target folder.</param>
		/// <returns></returns>
		private Boolean CopyFolder(string componentFolder, string path, Abstractions.Models.Extensions.folder folder)
		{
			foreach (Abstractions.Models.Extensions.file file in folder.Items.OfType<Nucleus.Abstractions.Models.Extensions.file>())
			{
				if (!CopyFile(componentFolder, path, file))
				{
					return false;
				}
			}

			foreach (Nucleus.Abstractions.Models.Extensions.folder subfolder in folder.Items.OfType<Nucleus.Abstractions.Models.Extensions.folder>())
			{				
				CopyFolder(componentFolder, path + "/" + subfolder.name, subfolder);
			}

			return true;
		}

		/// <summary>
		/// Delete a folder and its files.
		/// </summary>
		/// <param name="componentFolder">Extension root folder.</param>
		/// <param name="folder">Target folder (relative to extension root).</param>
		/// <returns></returns>
		private Boolean DeleteFolder(string componentFolder, Abstractions.Models.Extensions.folder folder)
		{
			// delete specified sub-folders
			foreach (Abstractions.Models.Extensions.folder subFolder in folder.Items.Where(item => item is Abstractions.Models.Extensions.folder))
			{
				if (!DeleteFolder(componentFolder, subFolder))
				{
					return false;
				}
			}

			// delete specified files
			foreach (Abstractions.Models.Extensions.file file in folder.Items.Where(item => item is Abstractions.Models.Extensions.file))
			{
				if (!DeleteFile(componentFolder, file, folder))
				{
					return false;
				}
			}

			// remove directory (folder) if it is empty after deleting specified sub-folders and files
			if (System.IO.Directory.Exists(BuildExtensionFilePath(componentFolder, folder.name)))
			{
				if (!System.IO.Directory.EnumerateFileSystemEntries(BuildExtensionFilePath(componentFolder, folder.name)).Any())
				{
					System.IO.Directory.Delete(BuildExtensionFilePath(componentFolder, folder.name));
				}
			}
			return true;
		}


		/// <summary>
		/// Copy a file to the specified folder/file.
		/// </summary>
		/// <param name="componentFolder">Source file</param>
		/// <param name="file">Target file</param>
		/// <param name="folder">Target folder.  Can be null for root.</param>
		/// <returns></returns>
		private Boolean CopyFile(string componentFolder, string path, Abstractions.Models.Extensions.file file)
		{
			string zipFilePath;
			string localFilePath;
			ZipArchiveEntry entry;

			if (!String.IsNullOrEmpty(path))
			{
				// Note: Zip file paths (.FullName) always use "/" to separate folder paths, per ".ZIP File Format Specification" 4.4.17.1. [https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT]
				zipFilePath = $"{path}/{file.name}";
				localFilePath = $"{path.Replace('/', System.IO.Path.DirectorySeparatorChar)}{System.IO.Path.DirectorySeparatorChar}{file.name}";
			}
			else
			{
				zipFilePath = file.name;
				localFilePath = file.name;
			}

			entry = this.Archive.Entries.Where((entry) => entry.FullName.Equals(zipFilePath, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
			if (entry != null)
			{
				string target = BuildExtensionFilePath(componentFolder, localFilePath, true);
				string renamePath = BuildExtensionFilePath(componentFolder, localFilePath) + IExtensionManager.BACKUP_FILE_EXTENSION;

				// rename DLLs to .backup to prevent a file in use error.  ExtensionManager.Cleanup is called during startup to remove the 
				// backups.   Nucleus.Web.Program watches for changes to files in extensions folders & automatically restarts after a few seconds.
				if (System.IO.File.Exists(BuildExtensionFilePath(componentFolder, localFilePath)) && System.IO.Path.GetExtension(file.name).Equals(".dll", StringComparison.OrdinalIgnoreCase))
				{
					int backupCounter = 1;
					while (System.IO.File.Exists(renamePath))
					{
						renamePath = BuildExtensionFilePath(componentFolder, localFilePath) + "." + backupCounter++.ToString() + IExtensionManager.BACKUP_FILE_EXTENSION;						
					};
					
					this.Logger?.LogInformation("Renaming {target} to {renamePath}.", target, renamePath);
					System.IO.File.Move(target, renamePath);
					
				}

				this.Logger?.LogInformation("Writing {target} from zip.", target);
				entry.ExtractToFile(target, true);
				return true;
			}
			else
			{
				this.Logger?.LogWarning("Cannot find {zipFilePath} in zip.  Installation failed.", zipFilePath);
				return false;
			}
			
		}

		/// <summary>
		/// Delete a file, or rename it to .backup if it is an assembly.
		/// </summary>
		/// <param name="componentFolder">Source file</param>
		/// <param name="file">Target file</param>
		/// <param name="folder">Target folder.  Can be null for root.</param>
		/// <returns></returns>
		private Boolean DeleteFile(string componentFolder, Abstractions.Models.Extensions.file file, Abstractions.Models.Extensions.folder folder)
		{
			string filePath;
			
			if (folder != null)
			{
				filePath = BuildExtensionFilePath(componentFolder, $"{folder.name}{System.IO.Path.DirectorySeparatorChar}{file.name}");
			}
			else
			{
				filePath = BuildExtensionFilePath(componentFolder, file.name);
			}

			
			if (System.IO.File.Exists(filePath)) 
			{
				if (System.IO.Path.GetExtension(file.name).Equals(".dll", StringComparison.OrdinalIgnoreCase))
				{
					// rename DLLs to .backup to prevent a file in use error.  ExtensionManager.Cleanup is called during startup to remove the 
					// backups.   
					System.IO.File.Move(filePath, filePath + IExtensionManager.BACKUP_FILE_EXTENSION);
				}
				else
				{
					System.IO.File.Delete(filePath);
				}
			}
			

			return true;
		}

		/// <summary>
		/// Build a full path within the extensions folder from the specified "relative" folder and file.
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private string BuildExtensionFilePath(string folderPath, string filePath)
		{
			return BuildExtensionFilePath(folderPath, filePath, false);
		}

		/// <summary>
		/// Build a full path within the extensions folder from the specified "relative" folder and file and (optionally) 
		/// create the folder if it does not exist.
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="filePath"></param>
		/// <param name="create"></param>
		/// <returns></returns>
		private string BuildExtensionFilePath(string folderPath, string filePath, Boolean create)
		{
			string path = Path.Combine(this.FolderOptions.Value.GetExtensionFolder(folderPath, create), filePath);

			if (create)
			{
				string folder = System.IO.Path.GetDirectoryName(path);
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
			}
			return path;
		}

		public void Dispose()
		{
			this.ArchiveStream?.Dispose();
			this.Archive?.Dispose();
			this.Package = null;

			GC.SuppressFinalize(this);
		}

	}
}
