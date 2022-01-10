using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.User
{
	public class PageSelector
	{
		public Page Page { get; set; }
		public Page FromPage { get; set; }
		public PageMenu PageMenu { get; set; }
	}
}
