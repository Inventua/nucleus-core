using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.ViewModels
{
	public class HeadlinesCategorySelection
	{
		public Boolean IsSelected { get; set; }
		public Nucleus.Abstractions.Models.ListItem Category { get; set; }
	}
}
