using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Extensions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Nucleus.Web.Controllers.Setup
{
	[Area("Setup")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_WIZARD_POLICY)]
	public class SiteWizardController : Controller
	{
		private IWebHostEnvironment WebHostEnvironment { get; }
		private IHostApplicationLifetime HostApplicationLifetime { get; }

		private IExtensionManager ExtensionManager { get; }
		private ISiteManager SiteManager { get; }
		private IUserManager UserManager { get; }
		private ILayoutManager LayoutManager { get; }
		private IContainerManager ContainerManager { get; }

		public SiteWizardController(IWebHostEnvironment webHostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IExtensionManager extensionManager, ISiteManager siteManager, IUserManager userManager, ILayoutManager layoutManager, IContainerManager containerManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.HostApplicationLifetime = hostApplicationLifetime;
			this.ExtensionManager = extensionManager;
			this.SiteManager = siteManager;
			this.UserManager = userManager;
			this.LayoutManager = layoutManager;
			this.ContainerManager = containerManager;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> Index(ViewModels.Setup.SiteWizard viewModel)
		{
			if (await this.UserManager.CountSystemAdministrators() != 0)
			{
				return BadRequest();
			}
			return View("Index", await BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<IActionResult> Install(ViewModels.Setup.SiteWizard viewModel)
		{
			if (await this.UserManager.CountSystemAdministrators() != 0)
			{
				return BadRequest();
				//ModelState.Remove(nameof(viewModel.SystemAdminUserName));
				//ModelState.Remove(nameof(viewModel.SystemAdminPassword));
			}

			if (ModelState.IsValid)
			{
				if (await this.UserManager.CountSystemAdministrators() == 0)
				{
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.SystemAdminPassword), viewModel.SystemAdminPassword);
					if (!modelState.IsValid)
					{
						return BadRequest(modelState);
					}
				}

				{
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.SiteAdminPassword), viewModel.SiteAdminPassword);
					if (!modelState.IsValid)
					{
						return BadRequest(modelState);
					}
				}

				// Build the site
				await BuildSite(viewModel);

				// Wait 3 seconds after returning and restart
				Task restartTask = Task.Run(async () => {
					await Task.Delay(3000);
					this.HostApplicationLifetime.StopApplication();
				});

				return View("complete", new ViewModels.Setup.SiteWizardComplete() { SiteUrl = Url.Content(Request.Scheme + System.Uri.SchemeDelimiter + viewModel.Site.Aliases.First().Alias) });				
			}
			else
			{
				return BadRequest(ModelState);
			}
		}

		private async Task BuildSite(ViewModels.Setup.SiteWizard viewModel)
		{
			// install extensions
			foreach (ViewModels.Setup.SiteWizard.InstallableExtension selectedExtension in viewModel.InstallableExtensions.Where(ext => ext.IsSelected))
			{
				using (System.IO.FileStream extensionStream = System.IO.File.OpenRead(System.IO.Path.Combine(InstallableExtensionsFolder().FullName, selectedExtension.Filename)))
				{
					await this.ExtensionManager.InstallExtension(await this.ExtensionManager.SaveTempFile(extensionStream));
				}
			}

			// create site
			Nucleus.Abstractions.Models.Export.SiteTemplate template = await this.SiteManager.ReadTemplateTempFile(viewModel.TemplateTempFileName);

			template.Site.Name = viewModel.Site.Name;
			
			viewModel.Site.DefaultSiteAlias.Id = Guid.NewGuid();
			template.Site.Aliases = new() { viewModel.Site.DefaultSiteAlias };
			template.Site.DefaultSiteAlias = viewModel.Site.DefaultSiteAlias;

			template.Site.AdministratorsRole = viewModel.Site.AdministratorsRole;
			template.Site.AllUsersRole = viewModel.Site.AllUsersRole;
			template.Site.AnonymousUsersRole = viewModel.Site.AnonymousUsersRole;
			template.Site.RegisteredUsersRole = viewModel.Site.RegisteredUsersRole;

			template.Site.DefaultContainerDefinition = viewModel.Site.DefaultContainerDefinition;
			template.Site.DefaultLayoutDefinition = viewModel.Site.DefaultLayoutDefinition;
			template.Site.HomeDirectory = viewModel.Site.HomeDirectory;

			viewModel.Site = await this.SiteManager.Import(template);
			
			// create users

			// only create a system admin user if there isn't already one in the database
			if (await this.UserManager.CountSystemAdministrators() == 0)
			{
				Abstractions.Models.User sysAdminUser = new()
				{
					UserName = viewModel.SystemAdminUserName,
					IsSystemAdministrator = true
				};
				sysAdminUser.Secrets = new();
				sysAdminUser.Secrets.SetPassword(viewModel.SystemAdminPassword);
				sysAdminUser.Approved = true;
				sysAdminUser.Verified = true;
				await this.UserManager.SaveSystemAdministrator(sysAdminUser);
			}

			// create site admin user
			Abstractions.Models.User siteAdminUser = new()
			{
				UserName = viewModel.SiteAdminUserName
			};
			siteAdminUser.Secrets = new();
			siteAdminUser.Secrets.SetPassword(viewModel.SiteAdminPassword);
			siteAdminUser.Roles = new List<Role>() { viewModel.Site.AdministratorsRole };
			siteAdminUser.Approved = true;
			siteAdminUser.Verified = true;

			await this.UserManager.Save(viewModel.Site, siteAdminUser);
		}

		private async Task<ViewModels.Setup.SiteWizard> BuildViewModel(ViewModels.Setup.SiteWizard viewModel)
		{
			IEnumerable<ModuleDefinition> modulesInTemplate = null;
			IEnumerable<LayoutDefinition> layoutsInTemplate = null;
			IEnumerable<ContainerDefinition> containersInTemplate = null;

			viewModel.CreateSystemAdministratorUser = await this.UserManager.CountSystemAdministrators() == 0;

			viewModel.Templates = new();
			foreach (FileInfo templateFile in TemplatesFolder().EnumerateFiles("*.xml"))
			{
				viewModel.Templates.Add(new ViewModels.Setup.SiteWizard.SiteTemplate(System.IO.Path.GetFileNameWithoutExtension(templateFile.Name).Replace('-', ' '), templateFile.Name));
			}

			if (String.IsNullOrEmpty(viewModel.SelectedTemplate) && viewModel.Templates.Count == 1)
			{
				viewModel.SelectedTemplate = viewModel.Templates[0].FileName;
			}

			if (!String.IsNullOrEmpty(viewModel.SelectedTemplate))
			{
				using (Stream templateFile = System.IO.File.OpenRead(System.IO.Path.Combine(TemplatesFolder().FullName, viewModel.SelectedTemplate)))
				{
					Nucleus.Abstractions.Models.Export.SiteTemplate template = await this.SiteManager.ParseTemplate(templateFile);

					viewModel.Site = template.Site;
					
					modulesInTemplate = template.Pages
						.SelectMany(page => page.Modules)
						.Select(module => module.ModuleDefinition)
						.Distinct();

					layoutsInTemplate = template.Pages
								.Where(page => page.LayoutDefinition != null)
								.Select(page => page.LayoutDefinition)
								.Distinct();

					if (template.Site.DefaultLayoutDefinition != null)
					{
						layoutsInTemplate = layoutsInTemplate.Concat
						(
							new List<LayoutDefinition>() { template.Site.DefaultLayoutDefinition }
						);
					}

					containersInTemplate =
						(
							template.Pages
							.Where(page => page.DefaultContainerDefinition != null)
							.Select(page => page.DefaultContainerDefinition)
							.Distinct()
						).Concat
						(
							template.Pages
								.SelectMany(page => page.Modules)
								.Where(module => module.ContainerDefinition != null)
								.Select(module => module.ContainerDefinition)
								.Distinct()
						);

					// save parsed template (with Guids generated) so that the Guids stay the same when we build the site
					viewModel.TemplateTempFileName = await this.SiteManager.SaveTemplateTempFile(template);
				}
			}


			List<ViewModels.Setup.SiteWizard.InstallableExtension> installableExtensions = new();

			foreach (FileInfo extensionPackageFile in InstallableExtensionsFolder().EnumerateFiles("*.zip"))
			{
				using (Stream extensionStream = extensionPackageFile.OpenRead())
				{
					PackageResult extensionResult = await this.ExtensionManager.ValidatePackage(extensionStream);

					if (extensionResult.IsValid)
					{
						ViewModels.Setup.SiteWizard.InstallableExtension installableExtension = new(extensionPackageFile.Name, extensionResult);

						installableExtension.ModulesInPackage = extensionResult.Package.components
							.SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.moduleDefinition>())
							.Select(moduleDefinition => Guid.Parse(moduleDefinition.id))
							.Distinct();

						installableExtension.LayoutsInPackage = extensionResult.Package.components
							.SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.layoutDefinition>())
							.Select(layoutDefinition => Guid.Parse(layoutDefinition.id))
							.Distinct();

						installableExtension.ContainersInPackage = extensionResult.Package.components
							.SelectMany(component => component.Items.OfType<Nucleus.Abstractions.Models.Extensions.containerDefinition>())
							.Select(containerDefinition => Guid.Parse(containerDefinition.id))
							.Distinct();

						if
							(
								(modulesInTemplate != null && modulesInTemplate.Where(moduleDef => installableExtension.ModulesInPackage.Contains(moduleDef.Id)).Any()) ||
								(layoutsInTemplate != null && layoutsInTemplate.Where(layoutDef => installableExtension.LayoutsInPackage.Contains(layoutDef.Id)).Any()) ||
								(containersInTemplate != null && containersInTemplate.Where(containerDef => installableExtension.ContainersInPackage.Contains(containerDef.Id)).Any())
							)
						{
							// template contains one or more modules/layouts/containers that are in this extension (so the extension is required)
							installableExtension.IsSelected = true;
							installableExtension.IsRequired = true;
						}

						installableExtensions.Add(installableExtension);
					}
				}
			}

			List<string> warnings = new();
			// check for missing modules					
			foreach (ModuleDefinition moduleDefinition in modulesInTemplate)
			{
				if (!installableExtensions.Where(installableExtension => installableExtension.ModulesInPackage.Contains(moduleDefinition.Id)).Any())
				{
					// module is missing
					warnings.Add($"Module '{moduleDefinition.FriendlyName}'.");
				}
			}

			// check for missing layouts					
			foreach (LayoutDefinition layoutDefinition in layoutsInTemplate)
			{
				if (!installableExtensions.Where(installableExtension => installableExtension.LayoutsInPackage.Contains(layoutDefinition.Id)).Any())
				{
					// layout is missing
					warnings.Add($"Layout '{layoutDefinition.FriendlyName}'.");
				}
			}

			// check for missing containers					
			foreach (ContainerDefinition containerDefinition in containersInTemplate)
			{
				if (!installableExtensions.Where(installableExtension => installableExtension.ContainersInPackage.Contains(containerDefinition.Id)).Any())
				{
					// container is missing
					warnings.Add($"Container '{containerDefinition.FriendlyName}'.");
				}
			}

			viewModel.MissingExtensionWarnings = warnings.Distinct();

			viewModel.InstallableExtensions = installableExtensions.OrderBy(ext => ext.Name).ToList();
			viewModel.Layouts = (await this.LayoutManager.List()).InsertDefaultListItem();
			viewModel.Containers = (await this.ContainerManager.List()).InsertDefaultListItem();
						
			return viewModel;
		}

		private async Task<ViewModels.Setup.SiteWizard> BuildViewModel()
		{
			ViewModels.Setup.SiteWizard viewModel = await BuildViewModel(new());

			viewModel.Site.DefaultSiteAlias = new SiteAlias() { Alias = $"{ControllerContext.HttpContext.Request.Host}{ControllerContext.HttpContext.Request.PathBase}" };			
			if (ControllerContext.HttpContext.Request.Host.Port.HasValue)
			{
				viewModel.Site.DefaultSiteAlias.Alias += $":{ControllerContext.HttpContext.Request.Host.Port}";
			}

			return viewModel;
		}

		private DirectoryInfo TemplatesFolder()
		{
			string path = Path.Combine(Path.Combine(System.IO.Path.Combine(this.WebHostEnvironment.ContentRootPath, "Setup"), "Templates"), "Site");
			return new DirectoryInfo(path);
		}

		private DirectoryInfo InstallableExtensionsFolder()
		{
			string path = Path.Combine(System.IO.Path.Combine(this.WebHostEnvironment.ContentRootPath, "Setup"), "Extensions");
			DirectoryInfo result = new(path);
			if (!result.Exists)
			{
				result.Create();
			}
			return result;
		}
	}
}
