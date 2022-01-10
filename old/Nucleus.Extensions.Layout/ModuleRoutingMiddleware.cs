//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Nucleus.Abstractions.Models;
//using Nucleus.Core.DataProviders;
//using Nucleus.DataProviders.Abstractions;

//namespace Nucleus.Extensions.Layout
//{
//	/// <summary>
//	/// Provides support for API (/api/modules/[module]/[action]) routes where the request path contains a "mid" querystring value by finding the 
//	/// corresponding module and populating the context object's page and module properties.  The context object is a scoped DI object.
//	/// </summary>
//	public class ModuleRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
//	{
//		public Context Context { get; }
//		public DataProviderFactory DataProviderFactory { get; }

//		public ModuleRoutingMiddleware(ILogger<PageRoutingMiddleware> Logger, Context Context, DataProviderFactory dataProviderFactory)
//		{
//			this.Context = Context;
//			this.DataProviderFactory = dataProviderFactory;
//		}

//		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
//		{
//			Guid moduleId;

//			if (Guid.TryParse(context.Request.Query["mid"], out moduleId))
//			{
//				using (ILayoutDataProvider dataProvider = (ILayoutDataProvider)DataProviderFactory.CreateProvider())
//				{
//					this.Context.Module = dataProvider.GetPageModule(moduleId);
					
//					if (this.Context.Module != null)
//					{
//						this.Context.Page = dataProvider.GetPageModulePage(this.Context.Module.Id);

//						if (this.Context.Page != null)
//						{
//							this.Context.Site = dataProvider.GetSite(this.Context.Page.SiteId);
//						}
//					}
//				}
//				//foreach (PageInfo page in this.DataProvider.Pages)
//				//{
//				//	foreach (ModuleInfo module in page.Modules)
//				//	{
//				//		if (module.Id == moduleId)
//				//		{
//				//			this.Context.Page = page;
//				//			this.Context.Module = module;
//				//		}
//				//	}
//				//}
//			}


//			await next(context);
//		}
//	}
//}
