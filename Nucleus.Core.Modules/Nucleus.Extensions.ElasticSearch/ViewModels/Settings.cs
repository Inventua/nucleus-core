using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.ElasticSearch.ViewModels
{
	public class Settings : ConfigSettings
	{
		// This constructor is used by model binding
		public Settings() { }

		public Settings(Site site) : base(site) {}

	}
}
