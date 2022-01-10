using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class ExtensionsController : Controller
	{
		private IExtensionManager ExtensionManager { get; }
		private IHostApplicationLifetime HostApplicationLifetime { get; }
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		public ExtensionsController(IHostApplicationLifetime hostApplicationLifetime, IExtensionManager extensionManager, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			this.HostApplicationLifetime = hostApplicationLifetime;
			this.ExtensionManager = extensionManager;
			this.FolderOptions = folderOptions;
		}

		/// <summary>
		/// Display the "extensions" admin page
		/// </summary>
		[HttpGet]
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		[RequestSizeLimit(67108864)] // 64mb		
		public async Task<ActionResult> Upload([FromForm] IFormFile extensionFile)
		{
			ViewModels.Admin.Extensions viewModel = new();

			if (ControllerContext.ModelState.IsValid)
			{
				if (extensionFile != null)
				{
					if (System.IO.Path.GetExtension(extensionFile.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							using (Stream package = extensionFile.OpenReadStream())
							{
								Abstractions.Models.Extensions.PackageResult result = await this.ExtensionManager.ValidatePackage(package);

								if (result.IsValid)
								{
									viewModel.FileId = result.FileId;
									viewModel.Package = result.Package;
									viewModel.Readme = result.Readme;
								}
								else
								{
									SetMessages(viewModel, result.Messages);
									return View("Complete", viewModel);
								}
								//using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(package))
								//{

								//if (await installer.IsValid())
								//{
								//	viewModel.FileId = await installer.SaveTempFile();// await ExtensionManager.SaveTempFile(installer.GetArchiveFileStream());
								//	viewModel.Package = await installer.GetPackage();
								//	System.IO.Stream readmeStream = await installer.GetFileStream("readme.txt");

								//	if (readmeStream != null)
								//	{
								//		readmeStream.Position = 0;
								//		using (StreamReader reader = new(readmeStream))
								//		{
								//			viewModel.Readme = reader.ReadToEnd();
								//		}
								//	}
								//}
								//else
								//{
								//	SetMessages(viewModel, installer.ModelState);
								//	return View("Complete", viewModel);
								//}
								//}
							}
						}
						catch (Exception ex)
						{
							return BadRequest(ex.Message);
						}
					}
					else
					{
						return BadRequest("File does not have a valid extension.");
					}

				}
				else
				{
					return BadRequest("No file was received.");
				}
			}
			else
			{
				// When there is a problem receiving the uploaded file, the error is in ModelState
				//return BadRequest(ControllerContext.ModelState);
				SetMessages(viewModel, ControllerContext.ModelState);
				return View("Complete", viewModel);
			}

			return View("Wizard", viewModel);
		}

		private static void SetMessages(ViewModels.Admin.Extensions viewModel, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
		{
			if (!modelState.IsValid)
			{
				foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry entry in modelState.Values)
				{
					foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in entry.Errors)
					{
						viewModel.Messages.Add(error.ErrorMessage);
					}
				}
			}
		}

		[HttpPost]
		public async Task<ActionResult> Install(ViewModels.Admin.Extensions viewModel)
		{
			if (String.IsNullOrEmpty(viewModel.FileId))
			{
				return BadRequest();
			}
			else
			{
				try
				{
					await this.ExtensionManager.InstallExtension(viewModel.FileId);
					ExtensionManager.DeleteTempFile(viewModel.FileId);
					//using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(viewModel.FileId))
					//{
					//	await installer.InstallExtension();
					//	ExtensionManager.DeleteTempFile(viewModel.FileId);
					//}
				}
				catch (Exception ex)
				{
					return BadRequest(ex.Message);
				}
			}

			this.HostApplicationLifetime.StopApplication();

			return View("Complete", viewModel);
		}

		[HttpPost]
		public ActionResult Uninstall(Guid id)
		{
			Abstractions.Models.Extensions.package uninstallPackage = null;

			// find the package manifest with the specified id
			foreach (string extensionFolder in System.IO.Directory.GetDirectories(this.FolderOptions.Value.GetExtensionsFolder()))
			{
				string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, IExtensionManager.PACKAGE_MANIFEST_FILENAME);
				Abstractions.Models.Extensions.package package;

				if (System.IO.File.Exists(extensionPackageFilename))
				{
					System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.package));
					using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
					{
						package = (Abstractions.Models.Extensions.package)serializer.Deserialize(stream);
					}

					if (Guid.Parse(package.id) == id)
					{
						uninstallPackage = package;
						break;
					}
				}
			}

			if (uninstallPackage == null)
			{
				return BadRequest();
			}

			try
			{
				this.ExtensionManager.UninstallExtension(uninstallPackage);
				//using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(uninstallPackage))
				//{
				//	installer.UninstallExtension();
				//}
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return View("Complete", BuildViewModel());
		}

		private ViewModels.Admin.Extensions BuildViewModel()
		{
			ViewModels.Admin.Extensions viewModel = new();
			viewModel.InstalledExtensions = new();

			foreach (string extensionFolder in System.IO.Directory.GetDirectories(this.FolderOptions.Value.GetExtensionsFolder()))
			{
				string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, IExtensionManager.PACKAGE_MANIFEST_FILENAME);
				Abstractions.Models.Extensions.package package;

				if (System.IO.File.Exists(extensionPackageFilename))
				{
					System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.package));
					using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
					{
						package = (Abstractions.Models.Extensions.package)serializer.Deserialize(stream);
					}

					viewModel.InstalledExtensions.Add(package);
				}
			}

			return viewModel;
		}

	}
}
