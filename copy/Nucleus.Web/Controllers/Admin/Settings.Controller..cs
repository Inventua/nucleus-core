using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class SettingsController : Controller
	{
		/// <summary>
		/// Display the "settings" admin page
		/// </summary>
		[HttpGet]
		public ActionResult Index()
		{
			return View("Index");
		}
	}
}
