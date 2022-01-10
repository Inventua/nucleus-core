using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.Core
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ExtensionAttribute : RouteValueAttribute
	{
		public ExtensionAttribute(string name) : base("extension", name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Extension name can not be empty", nameof(name));
			}
		}
	}
}
