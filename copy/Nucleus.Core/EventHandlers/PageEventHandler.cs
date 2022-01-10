using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.EventHandlers.Abstractions;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;

namespace Nucleus.Core.EventHandlers
{
	public class PageEventHandler : ISystemEventHandler<Page, Create>
	{
		/// <summary>
		/// Perform operations after a page is created.
		/// </summary>
		/// <param name="page"></param>
		/// <remarks>
		/// This class has no implementation and is currently used for testing.
		/// </remarks>
		public void Invoke(Page page)
		{
			
		}
	}
}
