using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class Extensions
	{
		public string Readme { get; set; }
		public string License { get; set; }
		public Nucleus.Abstractions.Models.Extensions.package Package { get; set; }
		public string FileId { get; set; }

		public List<string> Messages { get; } = new();
		public List<Abstractions.Models.Extensions.package> InstalledExtensions { get; set; }

    public Abstractions.Models.Configuration.StoreOptions StoreOptions { get; set; }

    public Abstractions.Models.Configuration.Store SelectedStore { get; set; }

    public string SelectedStoreUrl { get; set; }

    public ExtensionsStoreSettings StoreSettings { get; set; }
    public Boolean IsOwnerAssigned { get; set; }

    public string SubscriptionName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public string StoreUrl { get; set; }
  }
}
