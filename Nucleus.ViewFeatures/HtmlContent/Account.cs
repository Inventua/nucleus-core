using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.ViewFeatures.HtmlContent;

/// <summary>
/// Renders an account control.
/// </summary>
/// <remarks>
/// If the user is logged in, the account control displays the user name, linked to a menu button and a "Log Out" button.  If the user is not 
/// logged in, the account control displays a "Log in" button.  
/// This function is used by the <see cref="HtmlHelpers.AccountHtmlHelper"/> and <see cref="TagHelpers.AccountTagHelper"/>.
/// </remarks>
/// <internal />
/// <hidden />
internal static class Account
{
  internal static async Task<TagBuilder> Build(ViewContext context, string buttonClass, object htmlAttributes)
  {
    TagBuilder outputBuilder = new("div");
    IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
    Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
    SitePages sitePages = nucleusContext.Site.GetSitePages();
    IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();

    outputBuilder.AddCssClass("nucleus-account-control");
    outputBuilder.AddCssClass("navbar");

    if (context.HttpContext.User.Identity.IsAuthenticated)
    {
      // user is logged in
      TagBuilder accountMenuButtonBuilder = new("button");
      accountMenuButtonBuilder.AddCssClass("btn dropdown-toggle");
      accountMenuButtonBuilder.AddCssClass(String.IsNullOrEmpty(buttonClass) ? "btn-secondary" : buttonClass);

      if (!context.HttpContext.User.IsApproved() || !context.HttpContext.User.IsVerified() || context.HttpContext.User.IsPasswordExpired())
      {
        TagBuilder warningBuilder = new("span");
        warningBuilder.AddCssClass("nucleus-material-icon pe-2");
        warningBuilder.InnerHtml.AppendHtml("&#xf052;");
        accountMenuButtonBuilder.InnerHtml.AppendHtml(warningBuilder);
      }

      accountMenuButtonBuilder.InnerHtml.AppendHtml(context.HttpContext.User.Identity.Name);
      accountMenuButtonBuilder.Attributes.Add("type", "button");
      accountMenuButtonBuilder.Attributes.Add("data-bs-toggle", "dropdown");
      accountMenuButtonBuilder.Attributes.Add("area-expanded", "false");
      outputBuilder.InnerHtml.AppendHtml(accountMenuButtonBuilder);

      TagBuilder accountMenuBuilder = new("ul");
      accountMenuBuilder.AddCssClass("dropdown-menu");
      accountMenuBuilder.Attributes.Add("data-boundary", "viewport");

      if (!context.HttpContext.User.IsApproved())
      {
        TagBuilder notApprovedItemBuilder = new("li");
        TagBuilder notApprovedSpanBuilder = new("span");

        notApprovedSpanBuilder.AddCssClass("d-inline-block text-center alert alert-warning px-1 small");
        notApprovedSpanBuilder.InnerHtml.Append("Your account has not been approved");
        notApprovedSpanBuilder.Attributes.Add("title", "Your account has not been approved.  You can log in, but you can't access any secured functions.");

        notApprovedItemBuilder.InnerHtml.AppendHtml(notApprovedSpanBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(notApprovedItemBuilder);
      }

      if (!context.HttpContext.User.IsVerified())
      {
        TagBuilder notVerifiedItemBuilder = new("li");
        TagBuilder notVerifiedSpanBuilder = new("span");

        notVerifiedSpanBuilder.AddCssClass("d-inline-block text-center alert alert-warning px-1 small");
        notVerifiedSpanBuilder.InnerHtml.Append("Your account has not been verified");
        notVerifiedSpanBuilder.Attributes.Add("title", "Your account has not been verified.  You can log in, but you can't access any secured functions.  Check your email for a welcome message with instructions on how to verify your account.");

        notVerifiedItemBuilder.InnerHtml.AppendHtml(notVerifiedSpanBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(notVerifiedItemBuilder);
      }

      if (context.HttpContext.User.IsPasswordExpired())
      {
        TagBuilder passwordExpiredItemBuilder = new("li");
        TagBuilder passwordExpiredSpanBuilder = new("span");

        passwordExpiredSpanBuilder.AddCssClass("d-inline-block text-center alert alert-warning px-1 small");
        passwordExpiredSpanBuilder.InnerHtml.Append("Your password has expired");
        passwordExpiredSpanBuilder.Attributes.Add("title", "Your password has expired.  You can log in, but you can't access any secured functions until you reset your password.");

        passwordExpiredItemBuilder.InnerHtml.AppendHtml(passwordExpiredSpanBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(passwordExpiredItemBuilder);
      }

      if (!context.HttpContext.User.IsSystemAdministrator() && context.HttpContext.User.IsApproved() && context.HttpContext.User.IsVerified())
      {
        TagBuilder accountProfileItemBuilder = new("li");
        TagBuilder accountProfileLinkBuilder = new("a");

        accountProfileLinkBuilder.AddCssClass("dropdown-item");
        accountProfileLinkBuilder.InnerHtml.Append("Account Settings");

        PageRoute accountSettingsPageRoute = await GetPageRoute(sitePages.UserProfilePageId, pageManager);
        string path = accountSettingsPageRoute == null ? urlHelper.AreaAction("EditAccountSettings", "Account", "User") : urlHelper.Content("~" + accountSettingsPageRoute.Path);
        accountProfileLinkBuilder.Attributes.Add("href", $"{path}?returnUrl={GetReturnUrl(context.HttpContext, urlHelper, path)}");
        accountProfileLinkBuilder.Attributes.Add("rel", "nofollow");

        accountProfileItemBuilder.InnerHtml.AppendHtml(accountProfileLinkBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(accountProfileItemBuilder);
      }

      if (context.HttpContext.User.IsApproved() && context.HttpContext.User.IsVerified())
      {
        TagBuilder changePasswordItemBuilder = new("li");
        TagBuilder changePasswordLinkBuilder = new("a");

        changePasswordLinkBuilder.AddCssClass("dropdown-item");
        changePasswordLinkBuilder.InnerHtml.Append("Change Password");
        PageRoute changePasswordPageRoute = await GetPageRoute(sitePages.UserChangePasswordPageId, pageManager);
        string path = changePasswordPageRoute == null ? urlHelper.AreaAction("EditPassword", "Account", "User") : urlHelper.Content("~" + changePasswordPageRoute.Path);
        changePasswordLinkBuilder.Attributes.Add("href", $"{path}?returnUrl={GetReturnUrl(context.HttpContext, urlHelper, path)}");
        changePasswordLinkBuilder.Attributes.Add("rel", "nofollow");

        changePasswordItemBuilder.InnerHtml.AppendHtml(changePasswordLinkBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(changePasswordItemBuilder);
      }

      {
        TagBuilder logoutItemBuilder = new("li");
        TagBuilder logoutLinkBuilder = new("a");
        logoutLinkBuilder.AddCssClass("dropdown-item");
        logoutLinkBuilder.InnerHtml.Append("Log Out");

        string path = urlHelper.AreaAction("Logout", "Account", "User");
        logoutLinkBuilder.Attributes.Add("href", $"{path}?returnUrl={GetReturnUrl(context.HttpContext, urlHelper, path)}");
        logoutLinkBuilder.Attributes.Add("rel", "nofollow");

        logoutItemBuilder.InnerHtml.AppendHtml(logoutLinkBuilder);
        accountMenuBuilder.InnerHtml.AppendHtml(logoutItemBuilder);
      }
      outputBuilder.InnerHtml.AppendHtml(accountMenuBuilder);
    }
    else
    {
      // user is not logged in
      TagBuilder buttonRowBuilder = new("div");
      buttonRowBuilder.AddCssClass("d-flex flex-row gap-1");

      if (nucleusContext.Site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.SignupAllowed))
      {
        PageRoute registerPageRoute = await GetPageRoute(sitePages.UserRegisterPageId, pageManager);

        if (registerPageRoute != null)
        {
          TagBuilder registerLinkBuilder = new("a");

          registerLinkBuilder.AddCssClass("btn");
          registerLinkBuilder.AddCssClass(String.IsNullOrEmpty(buttonClass) ? "btn-secondary" : buttonClass);
          registerLinkBuilder.InnerHtml.Append("Register");
          string path = urlHelper.Content("~" + registerPageRoute.Path);
          registerLinkBuilder.Attributes.Add("href", $"{path}?returnUrl={GetReturnUrl(context.HttpContext, urlHelper, path)}");
          registerLinkBuilder.Attributes.Add("rel", "nofollow");

          buttonRowBuilder.InnerHtml.AppendHtml(registerLinkBuilder);
        }
      }

      {
        PageRoute loginPageRoute = await GetPageRoute(sitePages.LoginPageId, pageManager);

        TagBuilder loginLinkBuilder = new("a");
        loginLinkBuilder.AddCssClass("btn");
        loginLinkBuilder.AddCssClass(String.IsNullOrEmpty(buttonClass) ? "btn-secondary" : buttonClass);

        loginLinkBuilder.InnerHtml.Append("Log In");

        string path = loginPageRoute == null ? urlHelper.AreaAction("", "Account", "User") : urlHelper.Content("~" + loginPageRoute.Path);
        loginLinkBuilder.Attributes.Add("href", $"{path}?returnUrl={GetReturnUrl(context.HttpContext, urlHelper, path)}");
        loginLinkBuilder.Attributes.Add("rel", "nofollow");

        buttonRowBuilder.InnerHtml.AppendHtml(loginLinkBuilder);
      }

      outputBuilder.InnerHtml.AppendHtml(buttonRowBuilder);
    }

    outputBuilder.MergeAttributes(htmlAttributes);

    return outputBuilder;
  }

  private static string GetReturnUrl(HttpContext context, IUrlHelper urlHelper, string targetUrl)
  {
    string returnUrl = urlHelper.Content("~" + context.Request.Path);
    if (returnUrl.Equals(targetUrl, StringComparison.OrdinalIgnoreCase))
    {
      returnUrl = urlHelper.Content("~/");
    }

    return System.Uri.EscapeDataString(returnUrl);
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
