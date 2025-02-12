﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.MultiContent.ViewModels
{
	public class Settings
	{			
		public List<Content> Contents { get; set; }
		public string Layout { get; set; }
		public List<string> Layouts { get; set; }

		public LayoutSettings LayoutSettings { get; set; }
	}
}
