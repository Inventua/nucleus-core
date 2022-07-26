using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.XmlDocumentation.ViewModels
{
	public class Settings
	{
		public Folder DocumentationFolder { get; set; }
		public Boolean DefaultOpen { get; set; }

		public Content WelcomeMessage { get; set; }
	}
}
