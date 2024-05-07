// Stylesheet and script merging has been removed.  See \Nucleus.Core\FileProviders\MergedFileProviderMiddleware for more information.
//using Microsoft.Extensions.FileProviders;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace Nucleus.Core.FileProviders
//{
//	internal class MergedFileInfo : IFileInfo
//	{
//		private Stream FileStream { get; }

//		public MergedFileInfo(string name, DateTime lastModified, Stream newStream)
//		{
//			this.Exists = true;
//			this.Length = newStream.Length;
//			this.PhysicalPath = null;
//			this.Name = name;
//			this.LastModified = lastModified;
//			this.IsDirectory = false;

//			this.FileStream = newStream;
//		}

//		public bool Exists { get; }

//		public long Length { get; }

//		public string PhysicalPath { get; }

//		public string Name { get; }

//		public DateTimeOffset LastModified { get; }

//		public bool IsDirectory { get; }

//		public Stream CreateReadStream()
//		{
//			MemoryStream result = new MemoryStream();

//			this.FileStream.Position = 0;
//			this.FileStream.CopyTo(result);
//			result.Position = 0;
//			return result;
//		}
//	}
//}
