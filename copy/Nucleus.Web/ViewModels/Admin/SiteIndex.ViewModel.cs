using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class SiteIndex
	{
		public IEnumerable<Site> Sites { get; set; }

	}
}
