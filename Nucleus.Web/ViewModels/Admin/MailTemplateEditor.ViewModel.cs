using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class MailTemplateEditor
	{
		public MailTemplate MailTemplate { get; set; }

    public string DataModel { get; set; }

    public class DataModelElement
    {
      // these need to match the values at https://microsoft.github.io/monaco-editor/typedoc/enums/languages.CompletionItemKind.html
      public enum Kinds
      {
        Function = 1,
        Method = 0,
        Field = 3,
        Property = 9,
        Enum = 15,
        Constant=14,
        
      }

      [Newtonsoft.Json.JsonIgnore]
      public System.Type Type { get; set; }

      [Newtonsoft.Json.JsonProperty("label")]
      public string Label { get; set; }

      [Newtonsoft.Json.JsonProperty("kind")]
      public Kinds Kind { get; set; }

      [Newtonsoft.Json.JsonProperty("code")]
      public string Code { get; set; }

      [Newtonsoft.Json.JsonProperty("documentation")]
      public string Documentation { get; set; }

      [Newtonsoft.Json.JsonProperty("properties")]
      public Dictionary<string, DataModelElement> Properties { get; set; }

      [Newtonsoft.Json.JsonProperty("functions")]
      public Dictionary<string, DataModelElement> Functions { get; set; }

    }
  }
}
