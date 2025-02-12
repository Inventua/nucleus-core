﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.XmlDocumentation.ViewModels
{
	public class Section
	{
		public Page Page { get; set; }
		public string Caption { get; set; }
		public string CssClass { get; set; }
		public IList<Nucleus.XmlDocumentation.Models.ApiMember> Members { get; set; }
	}
}
