using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Extensions;

namespace Nucleus.Modules.Links.HtmlHelpers
{
	public static class AdminHtmlHelper
	{
		public static IHtmlContent AddEditingControls(this IHtmlHelper htmlHelper, Guid Id)
		{
			Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();
			Boolean isEditing = htmlHelper.ViewContext.HttpContext.User.IsEditing(htmlHelper.ViewContext?.HttpContext, context.Site, context.Page, context.Module);					
			
			if (isEditing)
			{
				IUrlHelper urlHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(htmlHelper.ViewContext);

				TagBuilder editorBuilder = new("div");
				editorBuilder.AddCssClass("nucleus-inline-edit-controls justify-content-start");
				editorBuilder.InnerHtml.AppendHtml(context.Module.BuildEditButton("&#xe3c9;", "Edit", urlHelper.NucleusAction("Editor", "Admin", "Links", new { Id = Id }), null));

				return editorBuilder;
			}

			return null;
		}
	}
}
