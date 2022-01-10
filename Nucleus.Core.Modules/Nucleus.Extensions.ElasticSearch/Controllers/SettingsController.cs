using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Extensions.ElasticSearch.Controllers
{
	[Extension("ElasticSearch")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class SettingsController : Controller
	{
		private Context Context { get; }

		private ISiteManager SiteManager { get; }

		public SettingsController(Context context, ISiteManager siteManager)
		{
			this.Context = context;
			this.SiteManager = siteManager;
		}			

		[HttpGet]
		[HttpPost]
		public ActionResult Settings()
		{
			return View("Settings", BuildSettingsViewModel());
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_URL, viewModel.ServerUrl);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_INDEX_NAME, viewModel.IndexName);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_ATTACHMENT_MAXSIZE, viewModel.AttachmentMaxSize);

			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_TITLE, viewModel.Boost.Title);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_SUMMARY, viewModel.Boost.Summary);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_CATEGORIES, viewModel.Boost.Categories);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_KEYWORDS, viewModel.Boost.Keywords);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_CONTENT, viewModel.Boost.Content);

			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_AUTHOR, viewModel.Boost.AttachmentAuthor);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_KEYWORDS, viewModel.Boost.AttachmentKeywords);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_NAME, viewModel.Boost.AttachmentName);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_TITLE, viewModel.Boost.AttachmentTitle);

			this.SiteManager.Save(this.Context.Site);
			
			return Ok();
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult GetIndexCount(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName);

			long indexCount = request.CountIndex();

			return Json(new { Title = "Index Count", Message = $"There are {indexCount} entries in the index." });			
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult ClearIndex(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName);

			if (request.DeleteIndex())
			{
				return Json(new { Title = "Clear Index", Message = $"Index {viewModel.IndexName} has been removed and will be re-created the next time the search index feeder runs." });
			}
			else
			{
				return Json(new { Title = "Clear Index", Message = $"Index {viewModel.IndexName} was not removed." });
			}
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult GetIndexSettings(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName);

			Nest.GetIndexSettingsResponse response = request.GetIndexSettings();

			if (response.IsValid)
			{
				return Json(new { Title = "Get Index Settings", Message = $"<pre>{response.DebugInformation}</pre>" });
			}
			else
			{
				return Json(new { Title = "Get Index Settings", Message = $"<pre>{response.DebugInformation}</pre>" });
			}
		}


		private ViewModels.Settings BuildSettingsViewModel()
		{
			return new(this.Context.Site);
		}

	}
}