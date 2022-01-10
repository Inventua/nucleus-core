using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers
{
	[Area(AREA_NAME)]
	public class FileSelectorController : Controller
	{
		public const string AREA_NAME = "User";

		[HttpGet]
		[HttpPost]
		public ActionResult Index(ViewModels.User.FileSelector viewModel, string pattern)
		{
			viewModel.Pattern = pattern;
			return View("Index", viewModel);
		}

	}
}

