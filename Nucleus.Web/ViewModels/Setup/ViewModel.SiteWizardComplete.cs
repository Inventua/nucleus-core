using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Web.ViewModels.Setup
{
	public class SiteWizardComplete
	{
		public string SiteUrl { get; set; }
	}
}
