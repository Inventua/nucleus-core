using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Sitemap;
using Nucleus.Extensions;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using System.Threading.Tasks;

namespace Nucleus.Web.Controllers
{
	public class SitemapController : Controller
	{
		private Context Context { get; set; }
		private IPageManager PageManager { get; set; }

		public SitemapController(Context context, IPageManager pageManager)
		{
			this.Context = context;
			this.PageManager = pageManager;
		}

		public async Task<ActionResult> Index()
		{
			Sitemap siteMap = new();
			System.IO.MemoryStream output = new();

			foreach (Page page in await this.PageManager.List(this.Context.Site))
			{
				if (!page.Disabled)
				{ 
					// only include pages which can be accessed by users who have not logged on
					page.Permissions = await this.PageManager.ListPermissions(page);
					if (this.Context.Site.AllUsersRole?.HasViewPermission(page) == true)
					{
						SiteMapEntry entry = new();
						PageRoute route = (await this.PageManager.Get(page.Id)).DefaultPageRoute();

						if (route != null)
						{
							entry.Url = base.Url.GetAbsoluteUri(route.Path).AbsoluteUri;

							// Removed.  Using page/DateChanged will not be accurate for pages with modules which provide content from
							// the database (forums, articles).
							//entry.LastModified = page.DateChanged == System.DateTime.MaxValue ? page.DateAdded : page.DateChanged;

							siteMap.Items.Add(entry);
						}
					}
				}
			}

			System.Xml.Serialization.XmlSerializer serializer = new(typeof(Sitemap));
			serializer.Serialize(output, siteMap);
			output.Position = 0;

			return File(output, "application/xml");
		}
	}
}
