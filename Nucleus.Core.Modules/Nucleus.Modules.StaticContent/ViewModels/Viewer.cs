using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Nucleus.Modules.StaticContent.ViewModels
{
	public class Viewer : Settings
	{
		public HtmlString Content { get; set; }
	}
}
