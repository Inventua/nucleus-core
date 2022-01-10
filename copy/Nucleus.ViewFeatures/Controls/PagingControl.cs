using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.ViewFeatures.Controls
{
	[ViewComponent(Name = "PagingControl")]
	public class PagingControl : ViewComponent
	{
		Context Context { get; }
		FileSystemManager FileSystemManager { get; }

		public PagingControl(Context Context, FileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		public IViewComponentResult Invoke(PagingSettings model, string propertyName)
		{
			ViewModels.PagingControl viewModel = new()
			{
				Results = model,
				PropertyName = propertyName ?? "PagingResult"
			};

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Remove(viewModel.PropertyName + nameof(PagedResult<object>.CurrentPageIndex));
			ModelState.Remove(viewModel.PropertyName + nameof(PagedResult<object>.PageSize));


			return View("~/Shared/Controls/Views/PagingControl.cshtml", viewModel);
		}
	}
}
