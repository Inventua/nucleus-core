using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System.Threading.Tasks;

namespace Nucleus.Extensions.ElasticSearch.Controllers
{
	[Extension("ElasticSearch")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
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
			if (!viewModel.ServerUrl.StartsWith("http"))
			{
				viewModel.ServerUrl = "http://" + viewModel.ServerUrl;
			}
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_URL, viewModel.ServerUrl);
			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_INDEX_NAME, viewModel.IndexName);

			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_USERNAME, viewModel.Username);

			if (viewModel.Password != ViewModels.Settings.DUMMY_PASSWORD)
			{
				this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_PASSWORD, ConfigSettings.EncryptPassword(this.Context.Site, viewModel.Password));
			}

			this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_CERTIFICATE_THUMBPRINT, viewModel.CertificateThumbprint);

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
		public async Task<ActionResult> GetIndexCount(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName, viewModel.Username, GetPassword(viewModel), viewModel.CertificateThumbprint);

			long indexCount = await request.CountIndex();

			return Json(new { Title = "Index Count", Message = $"There are {indexCount} entries in the index." });			
		}

		private string GetPassword(ViewModels.Settings viewModel)
		{
			if (viewModel.Password == ViewModels.Settings.DUMMY_PASSWORD)
			{
				ConfigSettings settings = new(this.Context.Site);
				return ConfigSettings.DecryptPassword(this.Context.Site, settings.EncryptedPassword);
			}
			else
			{
				return viewModel.Password;
			}
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> ClearIndex(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName, viewModel.Username, GetPassword(viewModel), viewModel.CertificateThumbprint);

			if (await request.DeleteIndex())
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
		public async Task<ActionResult> GetIndexSettings(ViewModels.Settings viewModel)
		{
			ElasticSearchRequest request = new(new System.Uri(viewModel.ServerUrl), viewModel.IndexName, viewModel.Username, GetPassword(viewModel), viewModel.CertificateThumbprint);

			Nest.GetIndexSettingsResponse response = await request.GetIndexSettings();

			if (response.IsValid)
			{
				return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode(response.DebugInformation) });
			}
			else
			{
				return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode( response.DebugInformation) });
			}
		}


		private ViewModels.Settings BuildSettingsViewModel()
		{
			return new(this.Context.Site);
		}

	}
}