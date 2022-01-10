//using Microsoft.Extensions.FileProviders;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace Nucleus.Extensions.FileProviders
//{
//	internal class MinifiedFileInfo : IFileInfo
//	{
//		private Stream FileStream { get; }

//		public MinifiedFileInfo (IFileInfo original, Stream newStream)
//		{
//			this.Exists = original.Exists;
//			this.Length = newStream.Length;
//			this.PhysicalPath = original.PhysicalPath;
//			this.Name = original.Name;
//			this.LastModified = original.LastModified;
//			this.IsDirectory = original.IsDirectory;

//			this.FileStream = newStream;
		
//		}

//		public bool Exists { get; }

//		public long Length { get; }

//		public string PhysicalPath { get; }

//		public string Name { get; }

//		public DateTimeOffset LastModified { get; }

//		public bool IsDirectory  { get; }

//		public Stream CreateReadStream()
//		{
//			this.FileStream.Position = 0;
//			return this.FileStream;
//		}
//	}
//}
