using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Web.ViewModels.Setup
{
	public class SiteWizard
	{
		public string SelectedTemplate { get; set; }
		public List<SiteTemplate> Templates { get; set; }

		public string TemplateTempFileName { get; set; }

		public IEnumerable<string> MissingExtensionWarnings { get; set; }

		public Site Site { get; set; }
		
		public Boolean CreateSystemAdministratorUser { get; set; }

		[Required(ErrorMessage = "You must enter a System Administrator user name")]
		public string SystemAdminUserName { get; set; }

		[Required(ErrorMessage = "You must enter a System Administrator password")]
		public string SystemAdminPassword { get; set; }

		[Required(ErrorMessage = "Confirm Password is required")]
		[Compare(nameof(SystemAdminPassword), ErrorMessage = "The new password and confirm password values must match")]
		public string SystemAdminConfirmPassword { get; set; }

		[Required(ErrorMessage = "You must enter a Site Administrator user name")]
		public string SiteAdminUserName { get; set; }

		[Required(ErrorMessage = "You must enter a Site Administrator password")]
		public string SiteAdminPassword { get; set; }

		[Required(ErrorMessage = "Confirm Password is required")]
		[Compare(nameof(SiteAdminPassword), ErrorMessage = "The new password and confirm password values must match")]
		public string SiteAdminConfirmPassword { get; set; }

		public IList<InstallableExtension> InstallableExtensions { get; set; }
		
		public IEnumerable<LayoutDefinition> Layouts { get; set; }
		public IEnumerable<ContainerDefinition> Containers { get; set; }

		public class SiteTemplate
		{
			public string FileName { get; set; }
			public string Title { get; set; }

			public SiteTemplate(string title, string filename)
			{
				this.Title = title;
				this.FileName = filename;
			}
		}

		public class InstallableExtension
		{
			//public Nucleus.Abstractions.Models.Extensions.PackageResult PackageInfo { get; set; }
			public string Filename { get; set; }

			public string Name { get; set; }
			public string Description { get; set; }

			public string Publisher { get; set; }
			public string PublisherUrl { get; set; }
			public string PublisherEmail { get; set; }


			public Boolean IsSelected { get; set; }
			public Boolean IsRequired { get; set; }

			public IEnumerable<Guid> ModulesInPackage { get; set; }
			public IEnumerable<Guid> LayoutsInPackage { get; set; }
			public IEnumerable<Guid> ContainersInPackage { get; set; }

			public InstallableExtension()
			{

			}

			public InstallableExtension(string filename, Nucleus.Abstractions.Models.Extensions.PackageResult packageInfo)
			{
				this.Filename = filename;

				this.Name = packageInfo.Package.name;
				this.Description = packageInfo.Package.description;
				this.Publisher = packageInfo.Package.publisher.name;
				this.PublisherEmail = packageInfo.Package.publisher.email;
				this.PublisherUrl = packageInfo.Package.publisher.url;
			}
		}
	}
}
