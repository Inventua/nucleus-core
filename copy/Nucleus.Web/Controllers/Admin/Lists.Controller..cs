using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class ListsController : Controller
	{
		private ILogger<ListsController> Logger { get; }
		private ListManager ListManager { get; }		
		private Context Context { get; }

		public ListsController(Context context, ILogger<ListsController> logger, ListManager ListManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.ListManager = ListManager;		
		}

		/// <summary>
		/// Display the list editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.ListEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? ListManager.CreateNew() : ListManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddList()
		{
			return View("Editor", BuildViewModel(new List()));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult AddListItem(ViewModels.Admin.ListEditor viewModel)
		{
			if (viewModel.List.Items == null)
			{
				viewModel.List.Items = new();
			}

			viewModel.List.Items.Add(new ListItem());

			return View("Editor", BuildViewModel(viewModel.List));
		}


		/// <summary>
		/// Remove the list item specified by id from the currently selected list's items.  
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <remarks>
		/// If a list item with the specified id is not present in the currently selected list's items, no action is taken and no exception is 
		/// generated.  The "delete" occurs within the viewModel only - the Save action must be called in order to commit the delete operation
		/// to the database.
		/// </remarks>
		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeleteListItem(ViewModels.Admin.ListEditor viewModel, Guid id)
		{

			foreach (ListItem listItem in viewModel.List.Items)
			{
				if (listItem.Id == id)
				{
					viewModel.List.Items.Remove(listItem);
					break;
				}
			}

			return View("Editor", BuildViewModel(viewModel.List));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.ListEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.ListManager.Save(this.Context.Site, viewModel.List);
			
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteList(ViewModels.Admin.ListEditor viewModel)
		{
			this.ListManager.Delete(viewModel.List);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.ListIndex BuildViewModel()
		{
			ViewModels.Admin.ListIndex viewModel = new();

			viewModel.Lists = this.ListManager.List(this.Context.Site);
			
			return viewModel;
		}

		private ViewModels.Admin.ListEditor BuildViewModel(List List)
		{
			ViewModels.Admin.ListEditor viewModel = new();

			viewModel.List = List;					
			
			return viewModel;
		}
	}
}
