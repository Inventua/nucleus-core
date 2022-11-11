using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Client.ViewModels
{
	public class Viewer : Settings
	{
		public Models.Configuration.SAMLProviders Options { get; set; }
		public string ReturnUrl { get; set; }
	}
}
