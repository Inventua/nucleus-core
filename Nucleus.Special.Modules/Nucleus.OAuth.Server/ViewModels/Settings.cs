using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.ViewModels
{
	public class Settings
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<ClientApp> ClientApps { get; set; } = new () { PageSize = 20 };
	
		public ClientApp ClientApp { get; set; }
	}
}
