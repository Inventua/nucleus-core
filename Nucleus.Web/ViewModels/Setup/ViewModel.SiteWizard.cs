using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Setup
{
  public class SiteWizard
  {
    public const string REFRESH_DATABASES = "refresh the database list ...";
    public string Url { get; set; }

    // database settings
    public Boolean IsDatabaseConfigured { get; set; }
    public IEnumerable<string> DatabaseProviders { get; set; } = [];
    public string DatabaseProvider { get; set; } = "SqlServer";
    public string DatabaseServer { get; set; }

    public IEnumerable<string> Databases { get; set; } = [REFRESH_DATABASES];
    public string ListDatabasesError { get; set; }

    public string DatabaseName { get; set; }
    public Boolean UseEncryption { get; set; }
    public Boolean TrustServerCertificate { get; set; }
    public Boolean ShowDatabasePicker { get; set; }

    public string DatabaseConnectionString { get; set; }

    public Boolean DatabaseUseIntegratedSecurity { get; set; } = true;
    public string DatabaseUserName { get; set; }
    public string DatabasePassword { get; set; }

    // configuration checks
    public Nucleus.Abstractions.IPreflight.ValidationResults Preflight { get; set; }

    // file system
    public List<FileSystemType> AvailableFileSystemTypes { get; set; } = [];
    public List<SelectedFileSystem> SelectedFileSystems { get; set; } = [];

    public FileSystemType AddFileSystemType { get; set; }
    public string ScrollTo { get; set; }

    // site settings
    public string SelectedTemplate { get; set; }
    public List<SiteTemplate> Templates { get; set; }

    public string TemplateTempFileName { get; set; }

    public IEnumerable<string> OtherWarnings { get; set; }
    public IEnumerable<string> MissingExtensionWarnings { get; set; }

    public Site Site { get; set; }

    public Boolean CreateSystemAdministratorUser { get; set; }

    [Required(ErrorMessage = "You must enter a System Administrator user name")]
    public string SystemAdminUserName { get; set; }

    [Required(ErrorMessage = "You must enter a System Administrator password")]
    public string SystemAdminPassword { get; set; }

    [Required(ErrorMessage = "You must enter a System Administrator password confirmation")]
    [Compare(nameof(SystemAdminPassword), ErrorMessage = "The System Administrator password and confirm password values must match")]
    public string SystemAdminConfirmPassword { get; set; }

    [Required(ErrorMessage = "You must enter a Site Administrator user name")]
    public string SiteAdminUserName { get; set; }

    [Required(ErrorMessage = "You must enter a Site Administrator password")]
    public string SiteAdminPassword { get; set; }

    [Required(ErrorMessage = "You must enter a Site Administrator password confirmation")]
    [Compare(nameof(SiteAdminPassword), ErrorMessage = "The Site Administrator password and confirm password values must match")]
    public string SiteAdminConfirmPassword { get; set; }

    public IList<InstallableExtension> InstallableExtensions { get; set; }

    public IEnumerable<LayoutDefinition> Layouts { get; set; }
    public IEnumerable<ContainerDefinition> Containers { get; set; }

    public class FileSystemType
    {
      public Guid PackageId { get; set; }

      public string FriendlyName { get; set; }
      public string ProviderType { get; set; }
      public string DefaultKey { get; set; }
      public string DefaultName { get; set; }

      public List<FileSystemProperty> Properties { get; set; } = [];
    }

    public class FileSystemProperty
    {
      public string FriendlyName { get; set; }
      public string Key { get; set; }
      public string Value { get; set; }


      public FileSystemProperty()
      {

      }

      public FileSystemProperty(string friendlyName, string key, string value) 
      {
        this.FriendlyName = friendlyName;
        this.Key = key;
        this.Value = value;
      }
    }

    public class SelectedFileSystem
    {
      public string Key { get; set; }
      public string Name { get; set; }
      public FileSystemType FileSystemType { get; set; }
      public Boolean IsRemoved { get; set; }
      public List<FileSystemProperty> Values { get; set; } = [];
    }

    public class SiteTemplate
    {
      public string FileName { get; set; }
      public string Title { get; set; }
      public string Description { get; set; }

			public SiteTemplate(string title, string description, string filename)
			{
				this.Title = title;
        this.Description = description;
				this.FileName = filename;
			}
		}

		public class InstallableExtension
		{			
			public Guid PackageId { get; set; }
			public System.Version PackageVersion { get; set; }
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
				this.PackageId = Guid.Parse(packageInfo.Package.id);
				this.PackageVersion = System.Version.Parse(packageInfo.Package.version);

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
