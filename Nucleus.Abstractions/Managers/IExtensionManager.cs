using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides management functions for extensions.
	/// </summary>
	/// <internal/>
	public interface IExtensionManager
	{
		/// <summary>
		/// Pacakge manifest filename
		/// </summary>
		public const string PACKAGE_MANIFEST_FILENAME = "package.xml";

		/// <summary>
		/// Backup file extension, used for renaming dlls during upgrades.
		/// </summary>
		public const string BACKUP_FILE_EXTENSION = ".backup";

		/// <summary>
		/// Validate a package.
		/// </summary>
		/// <param name="input">
		/// The file name of a zip file to install.  The file must be stored in the Nucleus "Temp" folder.  tempFileName must not contain a path.
		/// </param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Extensions.PackageResult> ValidatePackage(Stream input);

		/// <summary>
		/// Install a package, using a temp file as input.
		/// </summary>
		/// <param name="tempFileName">
		/// The file name of a zip file to install.  The file must be stored in the Nucleus "Temp" folder.  tempFileName must not contain a path.
		/// </param>
		/// <returns></returns>
		public Task InstallExtension(string tempFileName);

		/// <summary>
		/// Uninstall the extension specified by <paramref name="package"/>.
		/// </summary>
		/// <param name="package"></param>
		public void UninstallExtension(Abstractions.Models.Extensions.package package);

		/// <summary>
		/// Create or update a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public Task SaveModuleDefinition(ModuleDefinition moduleDefinition);

		/// <summary>
		/// Delete a <see cref="ModuleDefinition"/> database record.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public Task DeleteModuleDefinition(ModuleDefinition moduleDefinition);

		/// <summary>
		/// List the modules which use the specified module definition.
		/// </summary>
		/// <param name="moduleDefinition"></param>
		public Task<IEnumerable<PageModule>> ListPageModules(ModuleDefinition moduleDefinition);

		/// <summary>
		/// Create or update a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public Task SaveLayoutDefinition(LayoutDefinition layoutDefinition);

		/// <summary>
		/// Delete a <see cref="LayoutDefinition"/> database record.
		/// </summary>
		/// <param name="layoutDefinition"></param>
		public Task DeleteLayoutDefinition(LayoutDefinition layoutDefinition);

		/// <summary>
		/// Create or update a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public Task SaveContainerDefinition(ContainerDefinition containerDefinition);

		/// <summary>
		/// Delete a <see cref="ContainerDefinition"/> database record.
		/// </summary>
		/// <param name="containerDefinition"></param>
		public Task DeleteContainerDefinition(ContainerDefinition containerDefinition);

		/// <summary>
		/// Create or update a <see cref="ControlPanelExtensionDefinition"/> database record.
		/// </summary>
		/// <param name="controlPanelExtensionDefinition"></param>
		public Task SaveControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition);

		/// <summary>
		/// Delete a <see cref="ControlPanelExtensionDefinition"/> database record.
		/// </summary>
		/// <param name="controlPanelExtensionDefinition"></param>
		public Task DeleteControlPanelExtensionDefinition(ControlPanelExtensionDefinition controlPanelExtensionDefinition);

		/// <summary>
		/// List installed control panel extensions.
		/// </summary>
		/// <param name="scope"></param>
		/// <returns></returns>
		public Task<IEnumerable<ControlPanelExtensionDefinition>> ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes scope);

		/// <summary>
		/// Save a stream to a randomly generated filename in the temp folder.
		/// </summary>
		/// <param name="fileStream"></param>
		/// <returns>The randomly-generated file name.</returns>
		public Task<string> SaveTempFile(Stream fileStream);

		/// <summary>
		/// Delete the specified file from the temp folder.
		/// </summary>
		/// <param name="tempFileName"></param>
		public void DeleteTempFile(string tempFileName);

	}
}
