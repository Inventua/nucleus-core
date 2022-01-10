//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.ComponentModel;
//using Nucleus.Abstractions;

//namespace Nucleus.Extensions.Logging
//{
//	public class TextFileLoggerOptions
//	{
//		internal const string Section = "TextFileLoggerOptions";

//		private string _path;

//		public string Path 
//		{ 
//			get
//			{
//				return _path;
//			}
//			set
//			{
//				_path = value;

//				if (!System.IO.Directory.Exists(_path))
//				{
//					System.IO.Directory.CreateDirectory(_path);
//				}
//			}
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public TextFileLoggerOptions()
//		{
//			this.Path = Folders.GetDataFolder("Logs");
//		}

		
//	}
//}
