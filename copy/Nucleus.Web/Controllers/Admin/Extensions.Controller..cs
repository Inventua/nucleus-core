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
using Nucleus.Core;

//[Authorize(Policy = "")]
namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
	public class ExtensionsController : Controller
	{
		private ExtensionManager ExtensionManager { get; }

		public ExtensionsController(ExtensionManager extensionManager)
		{
			this.ExtensionManager = extensionManager;
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
		[RequestSizeLimit(6710886400)] // 6400mb		
																	 //[RequestSizeLimit(67108864)] // 64mb		
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
								using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(package))
								{

									if (await installer.IsValid())
									{
										viewModel.FileId = await installer.SaveTempFile();// await ExtensionManager.SaveTempFile(installer.GetArchiveFileStream());
										viewModel.Package = await installer.GetPackage();
										System.IO.Stream readmeStream = await installer.GetFileStream("readme.txt");

										if (readmeStream != null)
										{
											readmeStream.Position = 0;
											using (StreamReader reader = new(readmeStream))
											{
												viewModel.Readme = reader.ReadToEnd();
											}
										}
									}
									else
									{
										SetMessages(viewModel, installer.ModelState);
										return View("Complete", viewModel);
									}
								}
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
					using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(viewModel.FileId))
					{
						await installer.InstallExtension();
						ExtensionManager.DeleteTempFile(viewModel.FileId);
					}
				}
				catch (Exception ex)
				{
					return BadRequest(ex.Message);
				}
			}

			return View("Complete", viewModel);
		}

		[HttpPost]
		public ActionResult Uninstall(Guid id)
		{
			Abstractions.Models.Manifest.package uninstallPackage = null;

			// find the package manifest with the specified id
			foreach (string extensionFolder in System.IO.Directory.GetDirectories(Folders.GetExtensionsFolder()))
			{
				string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, Nucleus.Core.ExtensionInstaller.PACKAGE_FILENAME);
				Abstractions.Models.Manifest.package package;

				if (System.IO.File.Exists(extensionPackageFilename))
				{
					System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Manifest.package));
					using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
					{
						package = (Abstractions.Models.Manifest.package)serializer.Deserialize(stream);
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
				using (ExtensionInstaller installer = this.ExtensionManager.CreateInstaller(uninstallPackage))
				{
					installer.UninstallExtension();
				}
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

			foreach (string extensionFolder in System.IO.Directory.GetDirectories(Folders.GetExtensionsFolder()))
			{
				string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, Nucleus.Core.ExtensionInstaller.PACKAGE_FILENAME);
				Abstractions.Models.Manifest.package package;

				if (System.IO.File.Exists(extensionPackageFilename))
				{
					System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Manifest.package));
					using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
					{
						package = (Abstractions.Models.Manifest.package)serializer.Deserialize(stream);
					}

					viewModel.InstalledExtensions.Add(package);
				}
			}

			return viewModel;
		}

	}
}
