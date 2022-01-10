using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// Attribute used to specify the extension name for a Nucleus extension.
	/// </summary>
	/// <remarks>
	/// The name that you supply in the constructor name parameter is used when building routes to your extension controller actions.
	/// </remarks>
	/// <seealso href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-5.0">Routing in ASP.NET Core</seealso>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ExtensionAttribute : RouteValueAttribute
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
		public ExtensionAttribute(string name) : base("extension", name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Extension name can not be empty", nameof(name));
			}
		}
	}
}
