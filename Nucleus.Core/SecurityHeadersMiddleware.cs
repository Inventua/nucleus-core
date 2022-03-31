using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders;
using Nucleus.Data.Common;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Adds security headers to the response.
	/// </summary>
	public class SecurityHeadersMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
		
		public SecurityHeadersMiddleware()
		{
		}

		/// <summary>
		/// Add security headers to the response.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="next"></param>
		/// <returns></returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			AddHeader(context, "X-Frame-Options", "SAMEORIGIN");
			AddHeader(context, "X-Content-Type-Options", "nosniff");
			AddHeader(context, "X-XSS-Protection", "1; mode=block");
			AddHeader(context, "Referrer-Policy", "same-origin");
			AddHeader(context, "X-Permitted-Cross-Domain-Policies", "none");

			await next(context);
		}

		private void AddHeader(HttpContext context, string name, string value)
		{
			if (!context.Response.Headers.ContainsKey(name))
			{
				context.Response.Headers.Add(name, value);
			}
		}
	}
}
