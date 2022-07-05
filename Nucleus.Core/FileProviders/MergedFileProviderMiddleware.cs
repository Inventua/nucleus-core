using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.FileProviders
{
	/// <summary>
	/// The MergedFileProvider class serves "merged" js and css files. 
	/// </summary>
	/// <remarks>
	/// The MergedFileProvider responds to requests for <strong>merged.css</strong> and <strong>merged.js</strong>, with a src query 
	/// string containing a base64 encoded comma-separated list of resources.  The script and link tags which the merged file provider responds to are generated 
	/// by the <see cref="T:Nucleus.AspNetCore.Mvc.ViewFeatures.TagHelpersMergedScriptsTagHelper"/>
	/// and <see cref="T:Nucleus.AspNetCore.Mvc.ViewFeatures.MergedStyleSheetsTagHelper"/>.
	/// <br/><br/>
	/// Merging files reduces the number of requests sent to the server, which improves application load performance.  File merging is
	/// executed dynamically, so there is no need to change the way that you separate functionality into different files for code maintainability.
	/// <br/><br/>
	/// The Tag Helpers which generate the merged file tags can be enabled or disabled in appSettings.config.
	/// <br/><br/>
	/// The results are cached, so the processing overhead only occurs once.
	/// </remarks>
	public class MergedFileProviderMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
		public const char SEPARATOR_CHAR = ',';

		private IOptions<MergedFileProviderOptions> Options { get; }
		private ILogger<MergedFileProviderMiddleware> Logger { get; }
		
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }
		private string _cacheFolder;

		public MergedFileProviderMiddleware(IOptions<MergedFileProviderOptions> options, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions, ILogger<MergedFileProviderMiddleware> Logger)
		{
			this.Options = options;
			this.Logger = Logger;
			this.FolderOptions = folderOptions;

			ClearCache();
		}

		/// <summary>
		/// Locates a file at the given path.
		/// </summary>
		/// <param name="subpath">The path that identifies the file. </param>
		/// <returns>
		/// The file information. Caller must check Exists property. This will be the first existing <see cref="IFileInfo"/> returned 
		/// by the provided <see cref="IFileProvider"/> or a not found <see cref="IFileInfo"/> if no existing files is found.
		/// </returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			string extension = "";
			string src;
			string cacheKey = context.Request.Query["src"];

			Boolean isFound = false;
			MemoryStream mergedcontent;
			MergedFileInfo result;
			string subpath = context.Request.Path;
			string contentType;

			if (subpath.Contains("/merged.") && context.Request.Query.ContainsKey("src"))
			{
				src = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.Query["src"]));
				Logger.LogTrace("Received Request for {src}", src);

				contentType = subpath.EndsWith(".css") ? "text/css; charset=utf-8" : "application/javascript";
				result = GetCacheValue(cacheKey);

				if (result != null && result.Exists)
				{
					if (context.Request.Headers.IfNoneMatch == GenerateETag(result.LastModified.UtcDateTime))
					{
						context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
						return;
					}

					if (context.Request.Headers.IfModifiedSince == result.LastModified.UtcDateTime.ToString("r"))
					{
						context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
						return;
					}

					Logger.LogTrace("Served {cacheKey} from cache", cacheKey);
					await WriteFile(result, context.Response, contentType, result.LastModified.UtcDateTime);
					return;
				}
				else
				{
					mergedcontent = new System.IO.MemoryStream();

					foreach (string fullPath in src.Split(SEPARATOR_CHAR))
					{
						string path = fullPath.IndexOf('?') > 0 ? fullPath.Substring(0, fullPath.IndexOf('?')) : fullPath;
						string[] pathParts = path.Split(new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
						string pathBase = context.Request.PathBase.Value;
						string requestedPathRoot = pathParts[0];

						// check to see if the first part of the path is Request.PathBase.  If it is, use the next part of the path as the root 
						// path to validate (this is what happens when an application is run in an IIS virtual directory)
						if (pathBase.StartsWith( System.IO.Path.DirectorySeparatorChar) || pathBase.StartsWith(System.IO.Path.AltDirectorySeparatorChar))
						{
							pathBase = pathBase[1..];
						}

						if (requestedPathRoot.Equals(pathBase, StringComparison.OrdinalIgnoreCase))
						{
							path = String.Join(System.IO.Path.AltDirectorySeparatorChar, pathParts.Skip(1)); //path.Substring(requestedPathRoot.Length);
							requestedPathRoot = pathParts[1];
						}

						if (Nucleus.Abstractions.Models.Configuration.FolderOptions.ALLOWED_STATICFILE_PATHS.Contains(requestedPathRoot))
						{
							PhysicalFileProvider fileProvider = new(System.IO.Path.Combine(this.FolderOptions.Value.GetWebRootFolder(), ""));

							if (TryProvider(fileProvider, path, mergedcontent))
							{
								Logger.LogTrace("Added {path} to result.", path);
																
								// signal that at least one file was found
								isFound = true;

								if (String.IsNullOrEmpty(extension))
								{
									extension = System.IO.Path.GetExtension(path).ToLower();
								}
								else if (extension != System.IO.Path.GetExtension(path).ToLower())
								{
									Logger.LogWarning("File Extension mismatch on path {path}, expected {extension}.", path, extension);
								}
							}
							else
							{
								// one of the files was not found, fail the whole request
								Logger.LogWarning("File not found - {path}.", path);
							}
						}
            else
            {
							Logger.LogWarning("Invalid merged file request for {path}.", path);
            }
					}

					if (isFound)
					{
						result = new MergedFileInfo(cacheKey, DateTime.UtcNow, mergedcontent);
						this.SetCacheValue(cacheKey, mergedcontent);
						await WriteFile(result, context.Response, contentType, GetCacheEntryDate(cacheKey));
						return;
					}
				}
			}

			await next(context);
		}

		private static async Task WriteFile(IFileInfo input, HttpResponse response, string contentType, DateTime lastModified)
		{
			response.ContentType = contentType;
			response.ContentLength = input.Length;
			 
			response.Headers.LastModified = lastModified.ToString("r");
			response.Headers.ETag = GenerateETag(lastModified);
			await response.SendFileAsync(input);
		}
				
		private static string GenerateETag(DateTime lastModified)
		{
			return "\"" + Encode(lastModified.ToString()) + "\"";
		}

		private static Boolean TryProvider(IFileProvider provider, string path, Stream stream)
		{
			StreamReader reader;
			StreamWriter writer = new(stream, new UTF8Encoding(false));

			IFileInfo file = provider.GetFileInfo(path);

			if (file != null && file.Exists)
			{
				string content = $"{Environment.NewLine}/* {path}  */{Environment.NewLine}";
				writer.Write(content);

				reader = new StreamReader(file.CreateReadStream(), new UTF8Encoding(false));

				writer.Write(reader.ReadToEnd());
				writer.Flush();

				return true;
			}

			return false;
		}

		private string CacheFolder()
		{
			if (String.IsNullOrEmpty(_cacheFolder))
			{
				_cacheFolder = this.FolderOptions.Value.GetCacheFolder("MergedFileProvider"); 
			}

			return _cacheFolder;
		}

		private void ClearCache()
		{
			// If the cache folder does not exist (because of a permissions error), it doesn't need to be cleared.
			try
			{
				if (!System.IO.Directory.Exists(CacheFolder()))
				{
					return;
				}
			}
			catch (System.UnauthorizedAccessException)
			{
				// folder permissions error, ignore
				return;
			}

			Logger.LogInformation("Cache folder is {folder}.  Clearing cache after restart.", CacheFolder());
			
			foreach (string cachedFileName in Directory.EnumerateFiles(CacheFolder()))
			{
				try
				{
					File.Delete(cachedFileName);
				}
				catch (Exception e)
				{
					Logger.LogError(e, "An error was encountered clearing the cache");
				}
			}
		}

		private MergedFileInfo GetCacheValue(string subpath)
		{
			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");
			if (File.Exists(cachedFilePath))
			{
				return new MergedFileInfo(subpath, File.GetLastWriteTimeUtc(cachedFilePath), new FileStream(cachedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));
				//return new FileStream(cachedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			else
			{
				return null;
			}
		}

		private DateTime GetCacheEntryDate(string subpath)
		{
			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");

			try
			{
				if (File.Exists(cachedFilePath))
				{
					return File.GetLastWriteTimeUtc(cachedFilePath);
				}
			}
			catch 
			{
				// The try-catch is for the (unlikely) case where the cache item expires and is deleted in between the File.Exists check and the File.GetLastWriteTime
				// call.  We suppress that case and return DateTime.MinValue 
			}

			return DateTime.MinValue;
		}

		private void SetCacheValue(string subpath, Stream stream)
		{
			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");

			try
			{
				if (File.Exists(cachedFilePath))
				{
					File.Delete(cachedFilePath);
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
				Logger.LogError(e, "An error was encountered writing a cache file named {filepath}", cachedFilePath);
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

			using (MD5 md5 = MD5.Create())
			{
				return BitConverter.ToString(md5.ComputeHash(valueBytes)).Replace("-", "");
			}
		}
	}
}

