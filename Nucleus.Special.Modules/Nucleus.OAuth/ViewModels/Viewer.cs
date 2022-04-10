using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.ViewModels
{
	public class Viewer : Settings
	{
		public OAuth.Models.Configuration.OAuthProviders Options { get; set; }
	}
}
