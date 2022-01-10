using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an account control.
	/// </summary>
	/// <remarks>
	/// If the user is logged in, the account control displays the user name, linked to a menu button and a "Log Out" button.  If the user is not 
	/// logged in, the account control displays a "Log in" button.  
	/// This function is used by the <see cref="HtmlHelpers.AccountHtmlHelper"/> and <see cref="TagHelpers.AccountTagHelper"/>.
	/// </remarks>
	internal static class Account
	{
		internal static TagBuilder Build(ViewContext context, object htmlAttributes)
		{
			TagBuilder outputBuilder = new TagBuilder("div");			
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);

			if (context.HttpContext.User.Identity.IsAuthenticated)
			{
				// user is logged in
				TagBuilder accountLinkBuilder = new TagBuilder("a");
				TagBuilder accountIconBuilder = new TagBuilder("span");
				TagBuilder accountMenuBuilder = new TagBuilder("div");

				accountMenuBuilder.AddCssClass("AccountMenu PopupMenu");

				accountIconBuilder.AddCssClass("MaterialIcon");
				accountIconBuilder.InnerHtml.AppendHtml("&#xe853;");

				accountLinkBuilder.Attributes.Add("data-target", ".AccountMenu");

				accountLinkBuilder.Attributes.Add("href", $"{urlHelper.AreaAction("Menu", "Account", "User")}");

				accountLinkBuilder.InnerHtml.AppendHtml(accountIconBuilder);
				accountLinkBuilder.InnerHtml.Append(context.HttpContext.User.Identity.Name);
				outputBuilder.InnerHtml.AppendHtml(accountLinkBuilder);

				TagBuilder logoutLinkBuilder = new TagBuilder("a");
				logoutLinkBuilder.InnerHtml.Append("Log Out");
				logoutLinkBuilder.Attributes.Add("href", $"{urlHelper.AreaAction("Logout", "Account", "User")}?returnUrl={System.Uri.EscapeUriString(context.HttpContext.Request.Path)}");
				outputBuilder.InnerHtml.AppendHtml(logoutLinkBuilder);

				outputBuilder.InnerHtml.AppendHtml(accountMenuBuilder);
			}
			else
			{
				// user is not logged in
				SitePages sitePage = context.HttpContext.RequestServices.GetService<Context>().Site.GetSitePages();
				PageRoute loginPageRoute = null;
				if (sitePage.LoginPageId.HasValue)
				{
					Page loginPage = context.HttpContext.RequestServices.GetService < PageManager>().Get(sitePage.LoginPageId.Value);
					if (loginPage != null)
					{
						loginPageRoute = loginPage.DefaultPageRoute();
					}
				}

				PageRoute registerPageRoute = null;
				if (sitePage.UserRegisterPageId.HasValue)
				{
					Page registerPage = context.HttpContext.RequestServices.GetService<PageManager>().Get(sitePage.UserRegisterPageId.Value);
					if (registerPage  != null)
					{
						registerPageRoute = registerPage.DefaultPageRoute();
					}
				}

				if (registerPageRoute != null)
				{
					TagBuilder registerLinkBuilder = new TagBuilder("a");

					registerLinkBuilder.InnerHtml.Append("Register");
					registerLinkBuilder.Attributes.Add("href", urlHelper.Content("~" + registerPageRoute.Path + $"?returnUrl={System.Uri.EscapeUriString(context.HttpContext.Request.Path)}"));
					outputBuilder.InnerHtml.AppendHtml(registerLinkBuilder);
				}

				TagBuilder loginLinkBuilder = new TagBuilder("a");
				loginLinkBuilder.InnerHtml.Append("Log In");

				if (loginPageRoute == null)
				{					
					loginLinkBuilder.Attributes.Add("href", $"{urlHelper.AreaAction("", "Account", "User")}?returnUrl={System.Uri.EscapeUriString(context.HttpContext.Request.Path)}");
				}
				else
				{
					loginLinkBuilder.Attributes.Add("href", urlHelper.Content("~" + loginPageRoute.Path + $"?returnUrl={System.Uri.EscapeUriString(context.HttpContext.Request.Path)}"));
				}

				outputBuilder.InnerHtml.AppendHtml(loginLinkBuilder);
			}

			outputBuilder.MergeAttributes(htmlAttributes);

			return outputBuilder;
		}		
	}
}
