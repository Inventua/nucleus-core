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
	/// Adds a cache-control no-cache, no-store header to the response.
	/// </summary>
	/// <remarks>
	/// Sets a default "no cache" header, which can (and should) be overridden by controllers and middleware that needs to set a different
	/// cache-control header.
	/// </remarks>
	public class DefaultNoCacheMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
    static readonly Microsoft.Net.Http.Headers.CacheControlHeaderValue DEFAULT_CACHE_HEADERVALUES = new ()
			{
				NoCache = true,
				NoStore = true
			};
    
    public DefaultNoCacheMiddleware()
		{

		}

		/// <summary>
		/// Add default cache-control header to the response.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="next"></param>
		/// <returns></returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
      context.Response.GetTypedHeaders().CacheControl = DEFAULT_CACHE_HEADERVALUES;
      await next(context);
		}
	}
}
