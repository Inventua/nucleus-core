using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Sitemap;
using Nucleus.Extensions;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using System.Collections.Generic;
using Nucleus.Core.Managers;
using System;
using System.Linq;

namespace Nucleus.Web.Controllers;

public class SitemapController : Controller
{
  private Context Context { get; set; }
  private IPageManager PageManager { get; set; }

  private static readonly string[] BLOCKED_ROUTES = { RoutingConstants.ADMIN_ROUTE_PATH_PREFIX, RoutingConstants.EXTENSIONS_ROUTE_PATH_PREFIX, RoutingConstants.API_ROUTE_PATH_PREFIX, "oauth2", RoutingConstants.ERROR_ROUTE_PATH };

  public SitemapController(Context context, IPageManager pageManager)
  {
    this.Context = context;
    this.PageManager = pageManager;
  }

  public async Task<ActionResult> Index()
  {
    Sitemap siteMap = new();
    System.IO.MemoryStream output = new();

    foreach (Page page in await this.PageManager.List(this.Context.Site))
    {
      if (!page.Disabled)
      {
        // only include pages which can be accessed by users who have not logged on
        page.Permissions = await this.PageManager.ListPermissions(page);
        if (this.Context.Site.AllUsersRole?.HasViewPermission(page) == true)
        {
          SiteMapEntry entry = new();
          PageRoute route = (await this.PageManager.Get(page.Id)).DefaultPageRoute();

          if (route != null)
          {
            entry.Url = base.Url.GetAbsoluteUri(route.Path).AbsoluteUri;

            // Removed.  Using page/DateChanged will not be accurate for pages with modules which provide content from
            // the database (forums, articles).
            //entry.LastModified = page.DateChanged == System.DateTime.MaxValue ? page.DateAdded : page.DateChanged;

            siteMap.Items.Add(entry);
          }
        }
      }
    }

    System.Xml.Serialization.XmlSerializer serializer = new(typeof(Sitemap));
    serializer.Serialize(output, siteMap);
    output.Position = 0;

    return File(output, "application/xml");
  }

  public async Task<ActionResult> Robots()
  {
    SitePages sitePages = this.Context.Site.GetSitePages();
    
    List<string> reservedPaths = new();
    foreach (string path in BLOCKED_ROUTES)
    {
      AddPath(reservedPaths, path);
    }

    // always exclude the built-in "emergency" user pages (login, change password, user profile).  These are fallback pages which would only be used when
    // the site does not have the relevant page(s) set up 
    AddPath(reservedPaths, "/user/account/*");
    // robots.txt paths are case-sensitive
    AddPath(reservedPaths, "/User/Account/*");

    // exclude special pages
    await AddPage(reservedPaths, sitePages.LoginPageId);
    await AddPage(reservedPaths, sitePages.UserChangePasswordPageId);
    await AddPage(reservedPaths, sitePages.UserProfilePageId);
    await AddPage(reservedPaths, sitePages.ErrorPageId);
    
    await AddPage(reservedPaths, sitePages.NotFoundPageId);
    await AddPage(reservedPaths, sitePages.UserRegisterPageId);

    // add exclusions for pages with the "Include in search" property set to false
    foreach (Page page in await this.PageManager.List(this.Context.Site))
    {
      if (!page.IncludeInSearch)
      {
        await AddPage(reservedPaths, page);
      }
    }

    string output = $"Sitemap: {this.Context.Site.AbsoluteUri(RoutingConstants.SITEMAP_ROUTE_PATH, Request.IsHttps)}\r\nUser-agent: *\r\n{string.Join("\r\n", reservedPaths)}";
    return File(System.Text.Encoding.UTF8.GetBytes(output), "text/plain");
  }

  public async Task AddPage(List<string> reservedPaths, Guid? pageId)
  {
    if (pageId.HasValue)
    {
      Page page = await this.PageManager.Get(pageId.Value);
      await AddPage(reservedPaths, page);      
    }
  }

  public async Task AddPage(List<string> reservedPaths, Page page)
  {  
    // only include robots.txt entries for pages which can be accessed by users who have not logged on, so that we don't expose Urls for
    // pages which are private
    page.Permissions = await this.PageManager.ListPermissions(page);
    if (this.Context.Site.AllUsersRole?.HasViewPermission(page) == true)
    {
      foreach (PageRoute route in page.Routes.Where(route => route.Type == PageRoute.PageRouteTypes.Active))
      {
        AddPath(reservedPaths, route.Path);
      }
    }    
  }

  public void AddPath(List<string> reservedPaths, string path)
  {    
    reservedPaths.Add($"Disallow: {(!path.StartsWith('/') ? "/" : "")}{path}");    
  }
}
