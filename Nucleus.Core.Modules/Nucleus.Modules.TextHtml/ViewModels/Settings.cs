using System;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.TextHtml.ViewModels;

public class Settings
{
  public Guid ModuleId { get; set; }
  public string Title { get; set; }
  public Content Content { get; set; }

  public string ContentTypeCssClass()
  {
    switch (this.Content?.ContentType)
    {
      case "text/markdown": return "MarkdownEditorControl";
      case "text/plain": return "";
      default: // "text/html"
        return "HtmlEditorControl";
    }
  }
  public string ContentTypeFriendlyName()
  {
    switch (this.Content?.ContentType)
    {
      case "text/markdown": return "Markdown";
      case "text/plain": return "Plain Text";
      default: // "text/html"
        return "Html";
    }
  }
}
