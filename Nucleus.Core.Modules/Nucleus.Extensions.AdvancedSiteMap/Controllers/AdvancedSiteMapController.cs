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
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> Options { get; }
		private Nucleus.Abstractions.Models.Context Context { get; }

		public AdvancedSiteMapController(IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> options, Context context)
		{
			this.Options = options;
			this.Context = context;
		}

		[HttpGet]
		[Route("sitemap.xml")]
		public ActionResult Index()
		{
			string filename = SearchIndexManager.GetFilename(this.Options.Value, this.Context.Site); 

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