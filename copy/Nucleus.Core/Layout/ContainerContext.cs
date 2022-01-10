using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Html;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Represents a module being rendered by <see cref="ModuleContentRenderer"/> and the module output.
	/// </summary>
	/// <remarks>
	/// This class is the Model passed into all Nucleus containers (which are implemented in Razor).
	/// </remarks>
	public class ContainerContext
	{
		/// <summary>
		/// <see cref="PageModule"/> being rendered.
		/// </summary>
		public PageModule Module { get; internal set; }

		/// <summary>
		/// The rendered output of the module.
		/// </summary>
		public IHtmlContent Content { get; internal set; }

		
	}
}
