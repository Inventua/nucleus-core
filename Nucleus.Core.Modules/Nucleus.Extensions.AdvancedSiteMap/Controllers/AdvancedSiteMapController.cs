using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.AdvancedSiteMap.Controllers
{
	[Extension("AdvancedSiteMap")]
	public class AdvancedSiteMapController : Controller
	{
		private Nucleus.Abstractions.Models.Configuration.FolderOptions Options { get; }

		public AdvancedSiteMapController(IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> options)
		{
			this.Options = options.Value;
		}

		[HttpGet]
		[Route("sitemap.xml")]
		public ActionResult Index()
		{
			string filename = System.IO.Path.Join(this.Options.GetTempFolder(), "SiteMap", "Sitemap.xml"); 

			if (System.IO.File.Exists(filename))
			{
				return File(System.IO.File.OpenRead(filename), "application/xml");				
			}
			else
			{
				return NotFound();
			}
		}
	}
}