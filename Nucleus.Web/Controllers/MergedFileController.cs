using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Nucleus.Web.Controllers
{
	public class MergedFileController : Controller
	{
		private IOptions<MergedFileProviderOptions> Options { get; }
		private ILogger<MergedFileController> Logger { get; }
		private IOptions<FolderOptions> FolderOptions { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MinifiedFileProvider" /> class using a collection of file provider.
		/// </summary>
		/// <param name="fileProviders">The collection of <see cref="IFileProvider" /></param>
		public MergedFileController(IOptions<FolderOptions> folderOptions, IOptions<MergedFileProviderOptions> options, ILogger<MergedFileController> logger)
		{
			this.FolderOptions = folderOptions;
			this.Options = options;
			this.Logger = logger;
		}

		[HttpGet]
		public ActionResult Index([FromQuery] string src)
		{
			string extension = "";
			string cacheKey = src;

			string mimeType = ControllerContext.HttpContext.Request.Path.Value.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? "text/css" : "text/javascript"; 
			Boolean isFound = false;
			MemoryStream mergedcontent;
			Stream fileStream;
			string sourceFiles = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(src));

			Logger?.LogTrace("Received Request for {sourceFiles}", sourceFiles);

			fileStream = GetCacheValue(cacheKey);

			if (fileStream != null)
			{
				Logger?.LogInformation("Served {cacheKey} from cache", cacheKey);
				return File(fileStream, mimeType);
			}
			else
			{
				mergedcontent = new MemoryStream();

				foreach (string fullPath in sourceFiles.Split(MergedFileProviderOptions.SEPARATOR_CHAR))
				{
					string path = fullPath.IndexOf('?') > 0 ? fullPath.Substring(0, fullPath.IndexOf('?')) : fullPath;
					string[] pathParts = path.Split(new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
					string root = pathParts[0];

					if (!Nucleus.Abstractions.Models.Configuration.FolderOptions.ALLOWED_STATICFILE_PATHS.Contains(root))
					{
						return NotFound();
					}

					PhysicalFileProvider fileProvider = new PhysicalFileProvider(System.IO.Path.Combine(
						this.FolderOptions.Value.GetWebRootFolder(), 
						""));

					if (TryProvider(fileProvider, path, mergedcontent))
					{
						Logger?.LogTrace("Add {path} to result", path);

						// signal that at least one file was found
						isFound = true;

						if (String.IsNullOrEmpty(extension))
						{
							extension = System.IO.Path.GetExtension(path).ToLower();
						}
						else if (extension != System.IO.Path.GetExtension(path).ToLower())
						{
							Logger?.LogWarning("File Extension mismatch on path {path}, expected {extension}", path, extension);
							return NotFound();
						}
					}
					else
					{
						// one of the files was not found, fail the whole request
						Logger?.LogWarning("File not found - {path}", path);
						return NotFound();
					}
				}

				if (isFound)
				{
					this.SetCacheValue(cacheKey, mergedcontent);
					return File(mergedcontent, mimeType);
				}
			}

			return NotFound();
		}


		private static Boolean TryProvider(IFileProvider provider, string path, Stream stream)
		{
			StreamReader reader;
			StreamWriter writer = new(stream, new System.Text.UTF8Encoding(false));					

			IFileInfo file = provider.GetFileInfo(path);

			if (file != null && file.Exists)
			{
				string content = $"{Environment.NewLine}/* {path}  */{Environment.NewLine}";
				writer.Write(content);

				reader = new StreamReader(file.CreateReadStream(), new System.Text.UTF8Encoding(false));

				writer.Write(reader.ReadToEnd());
				writer.Flush();

				return true;
			}

			return false;
		}

		
		private string CacheFolder()
		{
			return this.FolderOptions.Value.GetDataFolder("Cache\\MergedFileProvider");
		}

		//private void ClearCache()
		//{
		//	Logger.LogInformation("Cache folder is {0}.  Clearing cache after restart.", CacheFolder());

		//	foreach (string cachedFileName in Directory.EnumerateFiles(CacheFolder()))
		//	{
		//		try
		//		{
		//			File.Delete(cachedFileName);
		//		}
		//		catch (Exception e)
		//		{
		//			Logger.LogError(e, "An error was encountered clearing the cache");
		//		}
		//	}
		//}

		private Stream GetCacheValue(string subpath)
		{
			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");
			if (System.IO.File.Exists(cachedFilePath))
			{
				return new FileStream(cachedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			else
			{
				return null;
			}
		}

		private void SetCacheValue(string subpath, Stream stream)
		{
			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");

			try
			{
				if (System.IO.File.Exists(cachedFilePath))
				{
					System.IO.File.Delete(cachedFilePath);
				}

				using (Stream cachedFileStream = new FileStream(cachedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					stream.Position = 0;
					stream.CopyTo(cachedFileStream);
					stream.Flush();
				}
			}
			catch (IOException e)
			{
				// if there is an IO exception here, it generally means that the file is in use, because another process is already 
				// writing to the cache for the specified subpath.  In this case, we can ignore and return, as the file content already
				// in the process of being written are the same as what we would be writing.
				Logger.LogError(e, "An error was encountered writing a cache file named {cachedFilePath}", cachedFilePath);
			}
		}

		/// <summary>
		/// Return a hex-encoded MD5 hash of the the "merged" subpath for use as a cache filename.  The MD5 encoding is to
		/// reduce the length of the resulting filename and because the path includes the query string.  
		/// </summary>
		/// <param name="value">Filename (path) to encode.</param>
		/// <returns>The Base-64 (escaped) MD5 hash of the input value.</returns>
		private static string Encode(string value)
		{
			byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(value);

			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				return BitConverter.ToString(md5.ComputeHash(valueBytes)).Replace("-", "");
			}
		}
	}
}

