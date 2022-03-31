using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Adds security headers to the response.
	/// </summary>
	public class SecurityHeadersMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
		private IOptions<SecurityHeaderOptions> Options { get; }

		public SecurityHeadersMiddleware(IOptions<SecurityHeaderOptions> options)
		{
			this.Options = options;
		}

		/// <summary>
		/// Add security headers to the response.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="next"></param>
		/// <returns></returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{			
			foreach (var option in this.Options.Value)
			{
				AddHeader(context, option.HeaderName, option.HeaderValue);
			}

			// Add defaults.  The AddHeader function checks whether the header is already present,
			// so if the user specified a header in config, the default will be ignored.
			AddHeader(context, "X-Frame-Options", "SAMEORIGIN");
			AddHeader(context, "X-Content-Type-Options", "nosniff");
			AddHeader(context, "X-XSS-Protection", "1; mode=block");
			AddHeader(context, "Referrer-Policy", "same-origin");
			AddHeader(context, "X-Permitted-Cross-Domain-Policies", "none");
			
			await next(context);
		}

		private void AddHeader(HttpContext context, string name, string value)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (!context.Response.Headers.ContainsKey(name))
				{
					if (!(context.Request.Headers.Accept.Contains("text/html") && IsHtmlOnlyHeader(name)))
					{
						context.Response.Headers.Add(name, value);
					}
				}
			}
		}

		private static HashSet<string> HtmlOnlyHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
		{
			"Content-Security-Policy", 
			"X-Content-Security-Policy",
			"X-UA-Compatible",
			"X-WebKit-CSP",
			"X-XSS-Protection"
		};

		private Boolean IsHtmlOnlyHeader(string name)
		{
			return HtmlOnlyHeaderNames.Contains(name);
		}
	}
}
