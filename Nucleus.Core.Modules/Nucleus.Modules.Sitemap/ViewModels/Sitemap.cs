using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Sitemap.ViewModels
{
	public class Sitemap
	{
		public int MaxLevels { get; set; }
		public Guid RootPageId { get; set; }
		public RootPageTypes RootPageType { get; set; }
		public Boolean ShowDescription { get; set; }
		
	}
}
