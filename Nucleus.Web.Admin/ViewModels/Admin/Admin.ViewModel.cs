using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{
	public class Index
	{
		public Page CurrentPage { get; set; }
		public Boolean IsSiteAdmin { get; set; }
		public Boolean IsSystemAdministrator { get; set; }
		public Boolean CanEditPage { get; set; }
		public Boolean CanEditContent { get; set; }
		public Boolean IsEditMode { get; set; }
    public string ControlPanelDockingCssClass { get; set; }
  }
}
