using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to read settings for security headers.
	/// </summary>
	public class SecurityHeaderOption
	{
		/// <summary>
		/// Header name.
		/// </summary>
		public string HeaderName { get; private set; }

		/// <summary>
		/// Header value.
		/// </summary> 
		public string HeaderValue { get; private set; }

	}
}
