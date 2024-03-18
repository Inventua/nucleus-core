using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Layout;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a module title.
	/// </summary>
	/// <remarks>
	/// Renders the module title.  If the user has module edit rights and is editing page content, render attributes
  /// to enable inline editing of the module title.
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class ModuleTitle
	{
		internal static TagBuilder Build(ViewContext context, string tag, object htmlAttributes)
		{
      IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);

      // the ModuleTitle Html/Tag helper will generally be used by containers
      Context nucleusContext = context.HttpContext.RequestServices.GetService<ContainerContext>();
      if (nucleusContext == null)
      {
        // but if we are not in a container (no ContainerContext), use the Nucleus context intead.
        nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
      }

      if (String.IsNullOrEmpty(nucleusContext.Module.Title))
      {
        return null;
      }
      else
      {
        TagBuilder outputBuilder = new(String.IsNullOrEmpty(tag) ? "h2" : tag);

        if (context.HttpContext.User.IsEditing(context.HttpContext, nucleusContext.Site, nucleusContext.Page, nucleusContext.Module))
        {
          outputBuilder.Attributes.Add("data-inline-edit-route", urlHelper.AreaAction("UpdateModuleTitle", "Pages", "Admin", new { mid = nucleusContext.Module.Id }));
          //outputBuilder.Attributes.Add("data-inline-edit-route", $"/admin/pages/updatemoduletitle?mid={nucleusContext.Module.Id}");
        }

        outputBuilder.AddCssClass("title");
        outputBuilder.InnerHtml.AppendHtml(nucleusContext.Module.Title);
        outputBuilder.MergeAttributes(htmlAttributes);

        return outputBuilder;
      }
		}		
	}
}
