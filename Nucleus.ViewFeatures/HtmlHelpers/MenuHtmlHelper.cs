using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to render a menu.
	/// </summary>
	public static class MenuHtmlHelper
	{
		/// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, int maxLevels, object htmlAttributes)
		{
			return Menu(htmlHelper, HtmlContent.Menu.MenuStyles.DropDown, HtmlContent.Menu.RootPageTypes.SiteRoot, Guid.Empty, maxLevels, htmlAttributes);
		}

		/// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, int maxLevels, object htmlAttributes)
		{
			return Menu(htmlHelper, menuStyle, HtmlContent.Menu.RootPageTypes.SiteRoot, Guid.Empty, maxLevels, htmlAttributes);
		}

    /// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
    /// <param name="menuClass"></param>
		/// <param name="maxLevels"></param>
    /// <param name="useName"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, string menuClass, int maxLevels, Boolean useName, object htmlAttributes)
    {
      return Menu(htmlHelper, menuStyle, menuClass, HtmlContent.Menu.RootPageTypes.SiteRoot, Guid.Empty, true, maxLevels, useName, htmlAttributes); 
    }

    /// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
    /// <param name="rootPageType"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, HtmlContent.Menu.RootPageTypes rootPageType, int maxLevels, object htmlAttributes)
    {
      return Menu(htmlHelper, menuStyle, rootPageType, Guid.Empty, maxLevels, htmlAttributes);
    }

    /// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
    /// <param name="rootPageType"></param>
    /// <param name="rootPageId"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, HtmlContent.Menu.RootPageTypes rootPageType, Guid rootPageId, int maxLevels, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, menuStyle, "", rootPageType, rootPageId, true, maxLevels, false, htmlAttributes).Result;
    }

    /// <summary>
		/// Displays the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
    /// <param name="rootPageType"></param>
    /// <param name="rootPageId"></param>
    /// <param name="hideEmptyMenu"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, HtmlContent.Menu.RootPageTypes rootPageType, Guid rootPageId, Boolean hideEmptyMenu, int maxLevels, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, menuStyle, "", rootPageType, rootPageId, hideEmptyMenu, maxLevels, false, htmlAttributes).Result;
    }

    /// <summary>
    /// Displays the site's menu structure.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="menuStyle"></param>
    /// <param name="menuClass">
    /// If a non-blank value is specified, the value is added to the css class for the top-level div rendered for the
    /// menu, and the default "navbar-light bg-light" classes are not applied.
    /// </param>
    /// <param name="rootPageType"></param>
    /// <param name="rootPageId"></param>
    /// <param name="hideEmptyMenu"></param>
    /// <param name="maxLevels"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, string menuClass, HtmlContent.Menu.RootPageTypes rootPageType, Guid rootPageId, Boolean hideEmptyMenu, int maxLevels, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, menuStyle, menuClass, rootPageType, rootPageId, hideEmptyMenu, maxLevels, false, htmlAttributes).Result;
    }

    /// <summary>
    /// Displays the site's menu structure.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="menuStyle"></param>
    /// <param name="menuClass">
    /// If a non-blank value is specified, the value is added to the css class for the top-level div rendered for the
    /// menu, and the default "navbar-light bg-light" classes are not applied.
    /// </param>
    /// <param name="rootPageType"></param>
    /// <param name="rootPageId"></param>
    /// <param name="hideEmptyMenu"></param>
    /// <param name="maxLevels"></param>
    /// <param name="useName"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, string menuClass, HtmlContent.Menu.RootPageTypes rootPageType, Guid rootPageId, Boolean hideEmptyMenu, int maxLevels, Boolean useName, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, menuStyle, menuClass, rootPageType, rootPageId, hideEmptyMenu, maxLevels, useName, htmlAttributes).Result;
    }
  }
}
