using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.Excel;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class Extensions
	{
		public string Readme { get; set; }
		public string License { get; set; }
		public Nucleus.Abstractions.Models.Extensions.package Package { get; set; }
		public string FileId { get; set; }
    public string Title { get; set; } = "Installation Complete.";

    public List<string> Messages { get; } = new();
		public List<Abstractions.Models.Extensions.package> InstalledExtensions { get; set; }

    public Dictionary<Guid, ExtensionComponents> ExtensionsUsage { get; set; } = new();

    public Abstractions.Models.Configuration.StoreOptions StoreOptions { get; set; }

    public Abstractions.Models.Configuration.Store SelectedStore { get; set; }

    public string SelectedStoreUrl { get; set; }

    public ExtensionsStoreSettings StoreSettings { get; set; }
    public Boolean IsOwnerAssigned { get; set; }

    public string SubscriptionName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public string StoreUrl { get; set; }

    public class ExtensionComponents
    {
      public List<ExtensionComponentUsage> Layouts { get; set; } = new();
      
      public List<ExtensionComponentUsage> Containers { get; set; } = new();
      
      public List<ExtensionComponentUsage> Modules { get; set; } = new();

      public List<ExtensionComponentUsage> ControlPanelExtensions { get; set; } = new();

      public int PageCount { get => this.Layouts.Sum(item=>item.Pages.Count) + this.Containers.Sum(item => item.Pages.Count) + this.Modules.Sum(item => item.Pages.Count) + this.ControlPanelExtensions.Sum(item => item.Pages.Count); }
    }

    public class ExtensionComponentUsage
    {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public List<Page> Pages { get; set; } = new();
      public string Message { get; set; }

      public ExtensionComponentUsage(Guid id, string name)
      {
        this.Id = id;
        this.Name = name;
      }

      public ExtensionComponentUsage(Guid id, string name, List<Page> pages) 
      {
        this.Id = id;
        this.Name = name;
        this.Pages = pages;
      }
    }
  }
}
