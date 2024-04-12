using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.ViewFeatures.ViewModels
{
	/// <summary>
	/// View model for Nucleus layouts.
	/// </summary>
	public class Layout
	{
		/// <summary>
		/// Encapsulates information about the current request.
		/// </summary>
		/// <remarks>
		/// Use the Context object to access Nucleus Site and Page information for the current request.
		/// </remarks>
		public Context Context { get; }

		/// <summary>
		/// Specifies whether the user has enabled inline edit mode.
		/// </summary>
		public Boolean IsEditing { get; set; }

		/// <summary>
		/// Specifies whether the current user has edit rights for the current page.
		/// </summary>
		public Boolean CanEdit { get; set; }

    /// <summary>
    /// Css class used to control control panel docking location.
    /// </summary>
    public string ControlPanelDockingCssClass { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string DefaultPageUri { get; set; }

		/// <summary>
		/// Gets the current request's site icon.  This value can be empty, which means that no icon has been selected for the site.
		/// </summary>
		public string SiteIconPath { get; set; }

		/// <summary>
		/// Gets the current request's site custom CSS file.  This value can be empty, which means that no custom CSS file has been 
		/// selected for the site.
		/// </summary>
		public string SiteCssFilePath { get; set; }
		
		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		/// <param name="context"></param>
		public Layout(Context context)
		{			
			this.Context = context;

		}
	}
}
