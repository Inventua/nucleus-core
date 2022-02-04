//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Nucleus.Extensions
//{
//	/// <summary>
//	/// Extensions for Razor pages
//	/// </summary>
//	public static class RazorPageExtensions
//	{
//		/// <summary>
//		/// Return a relative path, based the razor page path, to the file specified by <paramref name="relatedFile"/>
//		/// </summary>
//		/// <param name="razorPage"></param>
//		/// <param name="relatedFile"></param>
//		/// <returns></returns>
//		public static string GetRelativeFilePath(this Microsoft.AspNetCore.Mvc.Razor.RazorPageBase razorPage, string relatedFile)
//		{
//			return $"{System.IO.Path.GetDirectoryName(razorPage.Path).Replace("\\", "/")}/{relatedFile}";
//		}
//	}
//}
