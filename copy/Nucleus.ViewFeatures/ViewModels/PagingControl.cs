using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.ViewFeatures.ViewModels
{
	public class PagingControl
	{
		public string PropertyName { get; set; }
		public PagingSettings Results { get; set; }
	}
}
