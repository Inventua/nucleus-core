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
		public Nucleus.Abstractions.Models.Manifest.package Package { get; set; }
		public string FileId { get; set; }

		public List<string> Messages { get; } = new();
		public List<Abstractions.Models.Manifest.package> InstalledExtensions { get; set; }

	}
}
