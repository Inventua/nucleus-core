using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Layout;
using Nucleus.ViewFeatures.HtmlHelpers;

namespace Nucleus.ViewFeatures.TagHelpers;

/// <summary>
/// Apply selected container styles to the class.
/// </summary>
/// <remarks>
/// This tag helper is for Nucleus containers.  It indicates that the container supports styles, and applies selected container styles to the class.  The 
/// container-styles attribute is valid for containers only.  If it is applied to an element in a layout, it has no effect.
/// </remarks>
/// <example>
/// <![CDATA[<div class="container-default" container-styles="true" suppress-empty="true">@Html.ModuleTitle()@Model.Content</div>]]>
/// </example>
[HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[container-styles=true]")]
public class ContainerStylesTagHelper : TagHelper
{
  private static readonly char[] SpaceChars = { '\u0020', '\u0009', '\u000A', '\u000C', '\u000D' };

  /// <summary>
  /// Provides access to view context.
  /// </summary>
  [ViewContext]
  [HtmlAttributeNotBound]
  public ViewContext ViewContext { get; set; }

  /// <summary>
  /// Specifies whether to suppress an empty element.
  /// </summary>
  [HtmlAttributeName("container-styles")]
  public Boolean ContainerStyles { get; set; }

  /// <summary>
  /// Apply container styles to the specified element.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="output"></param>
  /// <returns></returns>
  async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
  {
    if (this.ContainerStyles)
    {
      ContainerContext containerContext = this.ViewContext.HttpContext.RequestServices.GetService<ContainerContext>();

      // if the container-styles attribute is applied to an element in a layout (or anything else that is not a container), containerContext will be
      // null and the attribute will (and should) have no effect.
      if (containerContext != null)
      {
        List<string> cssClasses = [];
        List<string> cssStyles = [];

        if (!String.IsNullOrEmpty(containerContext.Module.AutomaticClasses) || !String.IsNullOrEmpty(containerContext.Module.AutomaticStyles))
        {
          // prepare a list of css classes, including any that were specified in a class attribute for the container element, plus the configured
          // automatic styles.  The .Distinct call ensures that there are no duplicates.
          if (!String.IsNullOrEmpty(containerContext.Module.AutomaticClasses))
          {
            cssClasses.AddRange(containerContext.Module.AutomaticClasses.Split(SpaceChars));
          }

          // prepare a list of styles including any that were specified in a style attribute for the container element, plus the configured
          // automatic styles, which set css variables.  The .Distinct call ensures that there are no duplicates.
          if (!String.IsNullOrEmpty(containerContext.Module.AutomaticStyles))
          {
            cssStyles.AddRange(containerContext.Module.AutomaticStyles.Split(SpaceChars));
          }
        }

        // add .AutomaticClasses to the class attribute.  We don't use output.AddClass here because there will often be several css classes to add,
        // and the .AddClass function does a lot of parsing of the existing class attribute value, so it is not efficient to call it multiple 
        // times - so we do our own .Split() and .Distinct() (in the code above) - so that we parse the existing class attribute value once rather
        // than multiple times. 
        
        // there is always at least one css class to add ("container")
        if (output.Attributes.TryGetAttribute("class", out TagHelperAttribute existingClassAttribute))
        {
          cssClasses.InsertRange(0, existingClassAttribute.Value.ToString().Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries));
        }

        // we need the container-style class and the container styles css file.  We always add these to containers which support container styles
        // (that is, have a container-styles=true attribute) so that when containers define default values for container style variables they
        // are applied by container-styles.css
        AddStyleHtmlHelper.AddStyle(this.ViewContext, AddStyleHtmlHelper.WellKnownScripts.NUCLEUS_CONTAINER_STYLES);
        cssClasses.Insert(0, "nucleus-container");

        cssClasses = cssClasses.Distinct().ToList();
        output.Attributes.SetAttribute("class", String.Join(' ', cssClasses));
        
        // add style attribute to set css variables for custom values
        if (cssStyles.Any())
        {
          if (output.Attributes.TryGetAttribute("style", out TagHelperAttribute existingStyleAttribute))
          {
            cssStyles.InsertRange(0, existingStyleAttribute.Value.ToString().Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries));
          }
          cssStyles = cssStyles.Distinct().ToList();
          output.Attributes.SetAttribute("style", String.Join("; ", cssStyles));
        }
      }
    }

    await Task.CompletedTask;
  }
}
