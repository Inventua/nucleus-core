//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Nucleus.Abstractions.Models;
//using Nucleus.DataProviders.Abstractions;
//using Nucleus.Core.DataProviders;

//namespace Nucleus.Extensions.Layout
//{
//	/// <summary>
//	/// Provides support for page routing.  Pages can have any but the reserved list of routes (urls), this class checks the
//	/// request Url and matches it to a page, and sets the context.Page property to the matching page.  
//	/// 
//	/// For API and troubleshooting purposes, a route with a "pageid" querystring value can be specified, in this case the 
//	/// page is selected by Id.  A pageId querystring value takes precendence over the route (request path) for the purposes of page selection.
//	/// </summary>
//	public class PageRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
//	{		
//		public Context Context { get; }
//		public DataProviderFactory DataProviderFactory { get; }

//		public PageRoutingMiddleware(ILogger<PageRoutingMiddleware> Logger, Context Context, DataProviderFactory dataProviderFactory)
//		{
//			this.Context = Context;
//			this.DataProviderFactory = dataProviderFactory;
//		}

//		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
//		{
//			Guid pageId;

//			if (Guid.TryParse(context.Request.Query["pageid"], out pageId))
//			{
//				using (ILayoutDataProvider dataProvider = (ILayoutDataProvider)this.DataProviderFactory.CreateProvider())
//				{
//					this.Context.Page = dataProvider.GetPage(pageId);

//					if (this.Context.Page != null)
//					{
//						this.Context.Site = dataProvider.GetSite(this.Context.Page.SiteId);
//					}
//				}
//			}
//			else
//			{
//				using (ILayoutDataProvider dataProvider = (ILayoutDataProvider)DataProviderFactory.CreateProvider())
//				{
//					this.Context.Site = dataProvider.GetSite(context.Request.Host);

//					if (this.Context.Site != null)
//					{
//						this.Context.Page = dataProvider.GetPage(this.Context.Site, context.Request.Path);
//					}
//				}				
//			}

//			await next(context);
//		}
//	}
//}
