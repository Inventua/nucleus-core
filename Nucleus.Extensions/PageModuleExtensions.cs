using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extensions for the PageModule class.
	/// </summary>
	public static class PageModuleExtensions
	{
		/// <summary>
		/// Return an edit button for the specified module. 
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="text"></param>
		/// <param name="title"></param>
		/// <param name="formaction"></param>
		/// <param name="attributes"></param>
		/// <returns></returns>
		public static HtmlString BuildEditButton(this PageModule moduleInfo, string text, string title, string formaction, IDictionary<string, string> attributes)
		{
			TagBuilder editControlBuilder = new("button");
			editControlBuilder.InnerHtml.SetContent(text);
			editControlBuilder.Attributes.Add("class", "nucleus-material-icon btn btn-primary");
			editControlBuilder.Attributes.Add("title", title);
			editControlBuilder.Attributes.Add("type", "button");
			editControlBuilder.Attributes.Add("data-frametarget", ".nucleus-modulesettings-frame");
			editControlBuilder.Attributes.Add("formaction", $"{formaction}?mid={moduleInfo.Id}&mode=Standalone");

			if (attributes != null)
			{
				foreach (KeyValuePair<string, string> item in attributes)
				{
					editControlBuilder.Attributes.Add(item.Key, item.Value);
				}
			}

			StringWriter writer = new();
			editControlBuilder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);

			return new HtmlString(System.Web.HttpUtility.HtmlDecode(writer.ToString()));
		}

		/// <summary>
		/// Return a delete button for the specified module.
		/// </summary>
		/// <param name="moduleInfo"></param>
		/// <param name="text"></param>
		/// <param name="title"></param>
		/// <param name="formaction"></param>
		/// <param name="attributes"></param>
		/// <returns></returns>
		public static HtmlString BuildDeleteButton(this PageModule moduleInfo, string text, string title, string formaction, IDictionary<string, string> attributes)
		{
			TagBuilder deleteControlBuilder = new("button");
			deleteControlBuilder.InnerHtml.SetContent(text);
			deleteControlBuilder.Attributes.Add("class", "nucleus-material-icon btn btn-danger");
			deleteControlBuilder.Attributes.Add("title", title);
			deleteControlBuilder.Attributes.Add("type", "submit");
			deleteControlBuilder.Attributes.Add("formaction", $"{formaction}?mid={moduleInfo.Id}");
			deleteControlBuilder.Attributes.Add("data-confirm", "Delete this module?");

			if (attributes != null)
			{
				foreach (KeyValuePair<string, string> item in attributes)
				{
					deleteControlBuilder.Attributes.Add(item.Key, item.Value);
				}
			}

			StringWriter writer = new();
			deleteControlBuilder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);

			return new HtmlString(System.Web.HttpUtility.HtmlDecode(writer.ToString()));
		}
	}
}
