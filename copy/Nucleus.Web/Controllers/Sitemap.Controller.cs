using Microsoft.AspNetCore.Mvc;
using Nucleus.Core;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Sitemap;
using Nucleus.Abstractions;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers
{
	public class SitemapController : Controller
	{
		private Context Context { get; set; }
		private PageManager PageManager { get; set; }

		public SitemapController(Context context, PageManager pageManager)
		{
			this.Context = context;
			this.PageManager = pageManager;
		}

		public ActionResult Index()
		{
			Sitemap siteMap = new();
			System.IO.MemoryStream output = new();

			foreach (Page page in this.PageManager.List(this.Context.Site))
			{
				SiteMapEntry entry = new();
				PageRoute route = this.PageManager.Get(page.Id).DefaultPageRoute();

				if (route != null)
				{ 
					entry.Url = base.Url.GetAbsoluteUri(route.Path).AbsoluteUri; 
					entry.LastModified = page.DateChanged == System.DateTime.MaxValue ? page.DateAdded : page.DateChanged;

					siteMap.Items.Add(entry);
				}
			}

			System.Xml.Serialization.XmlSerializer serializer = new(typeof(Sitemap));
			serializer.Serialize(output, siteMap);
			output.Position = 0;

			return File(output, "application/xml");
		}
	}
}
