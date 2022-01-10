using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Web.ViewModels
{
	public class Default
	{
		public Context Context { get; }
		public Boolean IsEditing { get; internal set; }
		public Boolean CanEdit { get; internal set; }
		public string DefaultPageUri { get; internal set; }
		public string SiteIconPath { get; internal set; }
		public Default(Context context)
		{			
			this.Context = context;
		}
	}
}
