using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.MultiContent.ViewModels
{
	public class Editor
	{		
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
}
