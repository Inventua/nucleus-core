﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class ApiKeyEditor
	{
		public ApiKey ApiKey { get; set; }
		public Boolean IsNew { get; set; }
	}
}
