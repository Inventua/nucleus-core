using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Extensions
{
	/// <summary>
	/// File extensions
	/// </summary>
	public static class FileExtensions
	{
		/// <summary>
		/// Return an encoded file id for the specified file.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		/// <remarks>
		/// Inline and un-encoded file links are not supported for file links specified by file id only.
		/// </remarks>
		public static string EncodeFileId(this File file)
		{
			return EncodeFileId(file.Id);
		}

		/// <summary>
		/// Return an encoded file id for the specified file id.
		/// </summary>
		/// <param name="fileId"></param>
		/// <returns></returns>
		public static string EncodeFileId(Guid fileId)
		{
			return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fileId.ToString()));
		}

		/// <summary>
		/// Return a decoded file path for the specified encoded path.
		/// </summary>
		/// <param name="encodedPath"></param>
		/// <returns></returns>
		public static Guid DecodeFileId(string encodedPath)
		{
			string idString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPath));

			if (!Guid.TryParse(idString, out Guid id))
			{
				throw new InvalidOperationException($"Encoded path {encodedPath} is not valid.");
			}
			else
			{
				return id;
			}
		}
		
	}
}
