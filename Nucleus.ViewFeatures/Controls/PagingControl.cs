using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.ViewFeatures.Controls
{
	/// <summary>
	/// Paging control
	/// </summary>
	[ViewComponent(Name = "PagingControl")]
	public class PagingControl : ViewComponent
	{
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="fileSystemManager"></param>
		public PagingControl(Context Context, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Invoke (render) the control.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="propertyName"></param>
		/// <param name="renderMode"></param>
		/// <returns></returns>
		public IViewComponentResult Invoke(PagingSettings model, string propertyName, ViewModels.PagingControl.RenderModes renderMode = ViewModels.PagingControl.RenderModes.Standard)
		{
			ViewModels.PagingControl viewModel = new()
			{
				Results = model,
				PropertyName = propertyName ?? "PagingResult",
				RenderMode = renderMode
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
