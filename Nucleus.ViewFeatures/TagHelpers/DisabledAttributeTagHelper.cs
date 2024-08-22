using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// Suppresses "disabled" attribute if the value is false.
  /// </summary>	
  [HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[disabled=false]")]
  public class DisabledAttributeTagHelper : TagHelper
	{
		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.SuppressOutput();
      await Task.CompletedTask;
    }
	}
}