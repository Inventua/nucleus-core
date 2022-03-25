using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Modules.StaticContent
{
	public static class FileExtensions
	{
		public static Boolean IsMarkdown(this File file)
		{
			return System.IO.Path.GetExtension(file.Name).ToLower() == ".md";
		}

		public static Boolean IsText(this File file)
		{
			string[] fileExtensions = new string[] { ".txt" };

			return fileExtensions.Contains(System.IO.Path.GetExtension(file.Name).ToLower());
		}

		public static Boolean IsContent(this File file)
		{
			string[] fileExtensions = new string[] { ".htm", ".html"};

			return fileExtensions.Contains(System.IO.Path.GetExtension(file.Name).ToLower());
		}

		


	}
}
