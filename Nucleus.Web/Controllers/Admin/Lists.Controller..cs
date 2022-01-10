using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class ListsController : Controller
	{
		private ILogger<ListsController> Logger { get; }
		private IListManager ListManager { get; }		
		private Context Context { get; }

		public ListsController(Context context, ILogger<ListsController> logger, IListManager ListManager)
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
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.ListEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await ListManager.CreateNew() : await ListManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddList()
		{
			return View("Editor", await BuildViewModel (new List()));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> AddListItem(ViewModels.Admin.ListEditor viewModel)
		{
			if (viewModel.List.Items == null)
			{
				viewModel.List.Items = new();
			}

			viewModel.List.Items.Add(new ListItem());

			return View("Editor", await BuildViewModel(viewModel.List));
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
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> DeleteListItem(ViewModels.Admin.ListEditor viewModel, Guid id)
		{

			foreach (ListItem listItem in viewModel.List.Items)
			{
				if (listItem.Id == id)
				{
					viewModel.List.Items.Remove(listItem);
					break;
				}
			}

			return View("Editor", await BuildViewModel(viewModel.List));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.ListEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			// Model binding will set .Items to null if there are no items, or they have all been deleted.  We needs to set .Items
			// to an empty list, because the data provider interprets null as meaning "don't update the list items"
			if (viewModel.List.Items == null)
			{
				viewModel.List.Items = new();
			}

			await this.ListManager.Save(this.Context.Site, viewModel.List);
			
			return View("Index", await BuildViewModel ());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteList(ViewModels.Admin.ListEditor viewModel)
		{
			await this.ListManager.Delete(viewModel.List);
			return View("Index", await BuildViewModel ());
		}

		private async Task<ViewModels.Admin.ListIndex> BuildViewModel()
		{
			ViewModels.Admin.ListIndex viewModel = new();

			viewModel.Lists = await this.ListManager.List(this.Context.Site);
			
			return viewModel;
		}

		private Task<ViewModels.Admin.ListEditor> BuildViewModel(List List)
		{
			ViewModels.Admin.ListEditor viewModel = new();

			viewModel.List = List;					
			
			return Task.FromResult(viewModel);
		}
	}
}
