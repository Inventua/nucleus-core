using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Sitemap.ViewModels
{
	public class SitemapSettings : Sitemap
	{
		public PageMenu Pages { get; set; }
	}
}
