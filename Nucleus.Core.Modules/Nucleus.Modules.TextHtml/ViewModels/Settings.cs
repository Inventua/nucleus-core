using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.TextHtml.ViewModels
{
	public class Settings
	{
		public Guid ModuleId { get; set; }
    public string Title { get; set; }
		public Content Content { get; set; } 

	}
}
