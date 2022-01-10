using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Html;

namespace Nucleus.Abstractions.Layout
{
	/// <summary>
	/// Represents a module being rendered by <see cref="IModuleContentRenderer"/> and the module output.
	/// </summary>
	/// <remarks>
	/// This class is the Model passed into all Nucleus containers (which are implemented in Razor).
	/// </remarks>
	public class ContainerContext : Context
	{
		//// <summary>
		//// <see cref="PageModule"/> being rendered.
		//// </summary>
		//public PageModule Module { get; set; }

		/// <summary>
		/// The rendered output of the module.
		/// </summary>
		public IHtmlContent Content { get; set; }

	}
}
