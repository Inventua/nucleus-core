using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents security header options from configuration files.
	/// </summary>
	/// <remarks>
	/// Allows users to configure additional headers and values  to be returned by SecurityHeadersMiddleware. 
	/// </remarks>
	public class SecurityHeaderOptions : List<SecurityHeaderOption>
	{
		/// <summary>
		/// Configuration file section path for security header options.
		/// </summary>
		public const string Section = "Nucleus:SecurityHeaderOptions";
	}
}
