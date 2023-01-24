using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.ViewFeatures;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// TagHelper which handles the Nucleus ~! (path of the currently executing view) and ~# (path of the currently 
  /// executing Nucleus extension) prefixes in elements which can have an attribute which represents an Url.
  /// </summary>
  /// <remarks>
  /// Some code copied from https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Razor/src/TagHelpers/UrlResolutionTagHelper.cs
  /// </remarks>
  [HtmlTargetElement("*", Attributes = "[itemid^='~!/']")]
  [HtmlTargetElement("a", Attributes = "[href^='~!/']")]
  [HtmlTargetElement("applet", Attributes = "[archive^='~!/']")]
  [HtmlTargetElement("area", Attributes = "[href^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("audio", Attributes = "[src^='~!/']")]
  [HtmlTargetElement("base", Attributes = "[href^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("blockquote", Attributes = "[cite^='~!/']")]
  [HtmlTargetElement("button", Attributes = "[formaction^='~!/']")]
  [HtmlTargetElement("del", Attributes = "[cite^='~!/']")]
  [HtmlTargetElement("embed", Attributes = "[src^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("form", Attributes = "[action^='~!/']")]
  [HtmlTargetElement("html", Attributes = "[manifest^='~!/']")]
  [HtmlTargetElement("iframe", Attributes = "[src^='~!/']")]
  [HtmlTargetElement("img", Attributes = "[src^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("img", Attributes = "[srcset^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("input", Attributes = "[src^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("input", Attributes = "[formaction^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("ins", Attributes = "[cite^='~!/']")]
  [HtmlTargetElement("link", Attributes = "[href^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("menuitem", Attributes = "[icon^='~!/']")]
  [HtmlTargetElement("object", Attributes = "[archive^='~!/']")]
  [HtmlTargetElement("object", Attributes = "[data^='~!/']")]
  [HtmlTargetElement("q", Attributes = "[cite^='~!/']")]
  [HtmlTargetElement("script", Attributes = "[src^='~!/']")]
  [HtmlTargetElement("source", Attributes = "[src^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("source", Attributes = "[srcset^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("track", Attributes = "[src^='~!/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("video", Attributes = "[src^='~!/']")]
  [HtmlTargetElement("video", Attributes = "[poster^='~!/']")]

  [HtmlTargetElement("*", Attributes = "[itemid^='~#/']")]
  [HtmlTargetElement("a", Attributes = "[href^='~#/']")]
  [HtmlTargetElement("applet", Attributes = "[archive^='~#/']")]
  [HtmlTargetElement("area", Attributes = "[href^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("audio", Attributes = "[src^='~#/']")]
  [HtmlTargetElement("base", Attributes = "[href^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("blockquote", Attributes = "[cite^='~#/']")]
  [HtmlTargetElement("button", Attributes = "[formaction^='~#/']")]
  [HtmlTargetElement("del", Attributes = "[cite^='~#/']")]
  [HtmlTargetElement("embed", Attributes = "[src^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("form", Attributes = "[action^='~#/']")]
  [HtmlTargetElement("html", Attributes = "[manifest^='~#/']")]
  [HtmlTargetElement("iframe", Attributes = "[src^='~#/']")]
  [HtmlTargetElement("img", Attributes = "[src^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("img", Attributes = "[srcset^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("input", Attributes = "[src^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("input", Attributes = "[formaction^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("ins", Attributes = "[cite^='~#/']")]
  [HtmlTargetElement("link", Attributes = "[href^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("menuitem", Attributes = "[icon^='~#/']")]
  [HtmlTargetElement("object", Attributes = "[archive^='~#/']")]
  [HtmlTargetElement("object", Attributes = "[data^='~#/']")]
  [HtmlTargetElement("q", Attributes = "[cite^='~#/']")]
  [HtmlTargetElement("script", Attributes = "[src^='~#/']")]
  [HtmlTargetElement("source", Attributes = "[src^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("source", Attributes = "[srcset^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("track", Attributes = "[src^='~#/']", TagStructure = TagStructure.WithoutEndTag)]
  [HtmlTargetElement("video", Attributes = "[src^='~#/']")]
  [HtmlTargetElement("video", Attributes = "[poster^='~#/']")]

  public class UrlTagHelper : TagHelper
  {
    /// <summary>
    /// Provides access to view context.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }
        
    private static readonly Dictionary<string, string[]> ElementAttributeLookups =
      new(StringComparer.OrdinalIgnoreCase)
      {
        { "a", new[] { "href" } },
        { "applet", new[] { "archive" } },
        { "area", new[] { "href" } },
        { "audio", new[] { "src" } },
        { "base", new[] { "href" } },
        { "blockquote", new[] { "cite" } },
        { "button", new[] { "formaction" } },
        { "del", new[] { "cite" } },
        { "embed", new[] { "src" } },
        { "form", new[] { "action" } },
        { "html", new[] { "manifest" } },
        { "iframe", new[] { "src" } },
        { "img", new[] { "src", "srcset" } },
        { "input", new[] { "src", "formaction" } },
        { "ins", new[] { "cite" } },
        { "link", new[] { "href" } },
        { "menuitem", new[] { "icon" } },
        { "object", new[] { "archive", "data" } },
        { "q", new[] { "cite" } },
        { "script", new[] { "src" } },
        { "source", new[] { "src", "srcset" } },
        { "track", new[] { "src" } },
        { "video", new[] { "poster", "src" } },
      };


    /// <summary>
    /// Handle ~! and ~# prefixes for element attributes which can contain Urls.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
      if (output.TagName == null)
      {
        return;
      }

      if (ElementAttributeLookups.TryGetValue(output.TagName, out string[] attributeNames))
      {
        foreach (string name in attributeNames)
        {
          ProcessUrlAttribute(name, output);
        }
      }

      // itemid can be present on any HTML element.
      ProcessUrlAttribute("itemid", output);

      await Task.CompletedTask;
    }

    /// <summary>
    /// Handle ~! and ~# prefix for the attribute name specified by <paramref name="attributeName"/>.
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="output"></param>
    protected void ProcessUrlAttribute(string attributeName, TagHelperOutput output)
    {
      if (output.Attributes.TryGetAttribute(attributeName, out TagHelperAttribute attr))
      {
        if (attr.Value != null)
        {
          string value = attr.Value.ToString();

          if (value.StartsWith(Nucleus.ViewFeatures.HtmlHelperExtensions.EXTENSIONPATH_TOKEN) || value.StartsWith(Nucleus.ViewFeatures.HtmlHelperExtensions.VIEWPATH_TOKEN))
          {
            // parse ~! and ~# prefixes and update the attribute value
            output.Attributes.SetAttribute(attr.Name, this.ViewContext.ResolveExtensionUrl(value));
          }
        }
      }
    }
  }
}
