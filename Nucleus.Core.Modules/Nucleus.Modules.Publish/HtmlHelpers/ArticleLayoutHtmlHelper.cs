////using Microsoft.AspNetCore.Html;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.AspNetCore.Mvc.Rendering;
////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Text;
////using System.Threading.Tasks;
////using Nucleus.ViewFeatures;
////using Nucleus.Extensions;

////namespace Nucleus.Modules.Publish.HtmlHelpers
////{
////  public static class ArticleLayoutHtmlHelper
////  {
////    public static async Task<IHtmlContent> ArticleLayoutPartialAsync(this IHtmlHelper helper, string path, ViewModels.ViewArticle model)
////    {
////      IUrlHelper urlHelper = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(helper.ViewContext);

////      string viewPath = System.IO.Path.GetDirectoryName(((ViewContext)urlHelper.ActionContext).ExecutingFilePath).Replace("\\", "/");
////      string[] viewPathParts = viewPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

////      // viewPathParts[0] will be "extensions", viewPathParts[1] will be the extension folder name, and viewPathParts[2] is "Views"
////      if (viewPathParts.Length > 3)
////      {
////        return await helper.PartialAsync($"~/{viewPathParts[0]}/{viewPathParts[1]}/{viewPathParts[2]}/{path}", model);
////      }

////      return default;
////    }

////  }
////}
