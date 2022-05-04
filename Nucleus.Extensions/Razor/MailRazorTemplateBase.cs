using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngineCore;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// 
	/// </summary>
	public class MailRazorTemplateBase : RazorEngineTemplateBase 
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public static PageRoute DefaultPageRoute(Page page)
		{
			return page.DefaultPageRoute();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string FriendlyEncode(string value)
		{
			return value.FriendlyEncode();
		}
	}
}
