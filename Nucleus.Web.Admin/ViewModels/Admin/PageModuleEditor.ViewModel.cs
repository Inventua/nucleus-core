using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class PageModuleEditor 
	{
		public string UseLayout { get; set; }

    public PageModule Module { get; set; } = new PageModule();
  }
}
