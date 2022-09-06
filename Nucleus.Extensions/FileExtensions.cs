using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
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

		/// <summary>
		/// Return the content type for the file, or application/octet-stream if the file extension was not matched to a MIME type.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="addCharset">Specifies whether to append charset=utf-8 to the MIME type for text and javascript files.</param>
		/// <returns></returns>
		public static string GetMIMEType(this File file, Boolean addCharset)
		{
			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();

			if (!extensionProvider.TryGetContentType(file.Path, out string mimeType))
			{
				return "application/octet-stream";
			}
			else
			{
				if (mimeType.StartsWith("text/") && !mimeType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
				{
					mimeType += "; charset=utf-8";
				}

				return mimeType;
			}
		}

		/// <summary>
		/// Retrieve the height and width of the file (if its file extension indicates that it is an image) and set the height/width 
		/// properties of the specified <paramref name="file"/>.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="site"></param>
		/// <param name="fileSystemManager"></param>
		/// <returns>
		/// Returns true on success, or false if the file extension does not indicate that the file is an image, or there 
		/// is an error rendering the image.
		/// </returns>
		public static async Task<Boolean> GetImageDimensions(this File file, Site site, Nucleus.Abstractions.Managers.IFileSystemManager fileSystemManager)
		{
			if (GetMIMEType(file, false).StartsWith("image/"))
			{
				using System.IO.Stream imageStream = await fileSystemManager.GetFileContents(site, file);

				try
				{
					// The SkiaSharp is not directly referenced as a Nuget package, but is a dependency of DocumentPartner.ClosedXML,
					// so it is available to us.
					// Note: SkiaSharp does not support the TIFF format, so this never works for TIF files.
					SkiaSharp.SKImage image = SkiaSharp.SKImage.FromEncodedData(imageStream);
					if (image != null)
					{
						file.Width = image.Width;
						file.Height = image.Height;
						return true;
					}
				}
				catch(Exception)
				{
					// suppress exception thrown when decoding image, getting image dimensions is not a critical function.
				}
			}

			return false;
		}
	}
}
