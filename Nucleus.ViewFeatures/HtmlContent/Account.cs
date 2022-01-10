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
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
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
		internal static async Task<TagBuilder> Build(ViewContext context, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");			
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			SitePages sitePages = context.HttpContext.RequestServices.GetService<Context>().Site.GetSitePages();
			IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();

			if (context.HttpContext.User.Identity.IsAuthenticated)
			{
				// user is logged in
				//outputBuilder.AddCssClass("btn-group");

				TagBuilder accountMenuButtonBuilder = new("button");
				accountMenuButtonBuilder.AddCssClass("btn btn-secondary dropdown-toggle");
				accountMenuButtonBuilder.InnerHtml.AppendHtml(context.HttpContext.User.Identity.Name);
				accountMenuButtonBuilder.Attributes.Add("type", "button");
				accountMenuButtonBuilder.Attributes.Add("data-bs-toggle", "dropdown");
				accountMenuButtonBuilder.Attributes.Add("area-expanded", "false");
				outputBuilder.InnerHtml.AppendHtml(accountMenuButtonBuilder);

				TagBuilder accountMenuBuilder = new("ul");
				accountMenuBuilder.AddCssClass("dropdown-menu");
				accountMenuBuilder.Attributes.Add("data-boundary", "viewport");

				{
					TagBuilder accountProfileItemBuilder = new("li");
					TagBuilder accountProfileLinkBuilder = new("a");

					accountProfileLinkBuilder.AddCssClass("dropdown-item");
					accountProfileLinkBuilder.InnerHtml.Append("Account Settings");

					PageRoute accountSettingsPageRoute = await GetPageRoute(sitePages.UserProfilePageId, pageManager);
					//accountProfileLinkBuilder.Attributes.Add("href", $"{urlHelper.AreaAction("EditAccountSettings", "Account", "User")}");
					accountProfileLinkBuilder.Attributes.Add("href",
						accountSettingsPageRoute == null ? urlHelper.AreaAction("EditAccountSettings", "Account", "User") : urlHelper.Content("~" + accountSettingsPageRoute.Path) + $"?returnUrl={System.Uri.EscapeDataString(context.HttpContext.Request.Path)}");

					accountProfileItemBuilder.InnerHtml.AppendHtml(accountProfileLinkBuilder);
					accountMenuBuilder.InnerHtml.AppendHtml(accountProfileItemBuilder);
				}

				{
					TagBuilder changePasswordItemBuilder = new("li");
					TagBuilder changePasswordLinkBuilder = new("a");

					changePasswordLinkBuilder.AddCssClass("dropdown-item");
					changePasswordLinkBuilder.InnerHtml.Append("Change Password");
					PageRoute changePasswordPageRoute = await GetPageRoute(sitePages.UserChangePasswordPageId, pageManager);

					changePasswordLinkBuilder.Attributes.Add("href", 
						changePasswordPageRoute == null ? urlHelper.AreaAction("EditPassword", "Account", "User") : urlHelper.Content("~" + changePasswordPageRoute.Path) + $"?returnUrl={System.Uri.EscapeDataString(context.HttpContext.Request.Path)}");

					changePasswordItemBuilder.InnerHtml.AppendHtml(changePasswordLinkBuilder);
					accountMenuBuilder.InnerHtml.AppendHtml(changePasswordItemBuilder);
				}

				TagBuilder logoutItemBuilder = new("li");
				TagBuilder logoutLinkBuilder = new("a");
				logoutLinkBuilder.AddCssClass("dropdown-item");
				logoutLinkBuilder.InnerHtml.Append("Log Out");
				logoutLinkBuilder.Attributes.Add("href", $"{urlHelper.AreaAction("Logout", "Account", "User")}?returnUrl={System.Uri.EscapeDataString(context.HttpContext.Request.Path)}");

				logoutItemBuilder.InnerHtml.AppendHtml(logoutLinkBuilder);
				accountMenuBuilder.InnerHtml.AppendHtml(logoutItemBuilder);

				outputBuilder.InnerHtml.AppendHtml(accountMenuBuilder);
			}
			else
			{
				// user is not logged in
				
				PageRoute registerPageRoute = await GetPageRoute(sitePages.UserRegisterPageId, pageManager); 
			
				if (registerPageRoute != null)
				{
					TagBuilder registerLinkBuilder = new("a");

					registerLinkBuilder.AddCssClass("btn btn-secondary");
					registerLinkBuilder.InnerHtml.Append("Register");
					registerLinkBuilder.Attributes.Add("href", urlHelper.Content("~" + registerPageRoute.Path + $"?returnUrl={System.Uri.EscapeDataString(context.HttpContext.Request.Path)}"));

					outputBuilder.InnerHtml.AppendHtml(registerLinkBuilder);
				}

				PageRoute loginPageRoute = await GetPageRoute(sitePages.LoginPageId, pageManager);

				
				TagBuilder loginLinkBuilder = new("a");
				loginLinkBuilder.AddCssClass("btn btn-secondary");
				loginLinkBuilder.InnerHtml.Append("Log In");
				
				//outputBuilder.InnerHtml.AppendHtml(Modal.Build(context, "Login", true, null, new { id="LoginDialog" }));
				//loginLinkBuilder.Attributes.Add("data-target", "#LoginDialog");
								
				loginLinkBuilder.Attributes.Add("href",
					loginPageRoute == null ? urlHelper.AreaAction("", "Account", "User") : urlHelper.Content("~" + loginPageRoute.Path) + $"?returnUrl={System.Uri.EscapeDataString(context.HttpContext.Request.Path)}");

				outputBuilder.InnerHtml.AppendHtml(loginLinkBuilder);
			}

			outputBuilder.MergeAttributes(htmlAttributes);

			return outputBuilder;
		}		

		private async static Task<PageRoute> GetPageRoute(Guid? pageId, IPageManager pageManager)
		{
			if (pageId.HasValue)
			{
				Page loginPage = await pageManager.Get(pageId.Value);
				if (loginPage != null)
				{
					return loginPage.DefaultPageRoute();
				}
			}

			return null;
		}
	}
}
