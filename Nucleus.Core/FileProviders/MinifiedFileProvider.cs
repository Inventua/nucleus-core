//using System;
//using Microsoft.Extensions.FileProviders;
//using Microsoft.Extensions.Primitives;
//using System.IO;
//using Microsoft.Extensions.Options;
//using Microsoft.Extensions.Logging;
//using System.Security.Cryptography;
//using System.ComponentModel;
//using Nucleus.Abstractions.Models.Configuration;

//namespace Nucleus.Core.FileProviders
//{
//	/// <summary>
//	/// The MinifiedFileProvider serves dynamically-generated &quot;minified&quot; versions of Javascript and CSS static files.
//	/// </summary>
//	/// <remarks>
//	/// The Minified File Provider class wraps the .Net core static file provider in order to intercept and minify 
//	/// requests for static files.  This reduces the size of resources delivered to the caller, thus improving performance.
//	/// The results are cached, so the processing overhead only occurs once.
//	/// <br/><br/>
//	/// The Minified File Provider service is injected by the Nucleus Core host and is always available in all Nucleus Core applications.  The 
//	/// minified file provider can be enabled or disabled in config/appSettings.json.
//	/// </remarks>
//	public class MinifiedFileProvider : IFileProvider
//	{
//		private IFileProvider[] FileProviders {get;}
//		private MinifiedFileProviderOptions Options { get; }
//		private ILogger<MinifiedFileProvider> Logger { get; }
//		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

//		/// <summary>
//		/// Initializes a new instance of the <see cref="MinifiedFileProvider" /> class using a collection of file provider.
//		/// </summary>
//		/// <param name="fileProviders">The collection of <see cref="IFileProvider" /></param>
//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public MinifiedFileProvider(MinifiedFileProviderOptions options, ILogger<MinifiedFileProvider> Logger, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions, params IFileProvider[] fileProviders)
//		{
//			this.Options = options;
//			this.Logger = Logger;
//			this.FolderOptions = folderOptions;

//			this.FileProviders = fileProviders ?? Array.Empty<IFileProvider>();

//			ClearCache();
//		}

//		/// <summary>
//		/// Locates a file at the given path.
//		/// </summary>
//		/// <param name="subpath">The path that identifies the file. </param>
//		/// <returns>The file information. Caller must check Exists property. This will be the first existing <see cref="IFileInfo"/> returned by the provided <see cref="IFileProvider"/> or a not found <see cref="IFileInfo"/> if no existing files is found.</returns>
//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public IFileInfo GetFileInfo(string subpath)
//		{
//			IFileInfo fileInfo;
//			Stream fileStream;
		
//			foreach (var fileProvider in FileProviders)
//			{
//				StreamReader reader;

//				fileInfo = fileProvider.GetFileInfo(subpath);
//				if (fileInfo != null && fileInfo.Exists)
//				{
//					// check cache
//					fileStream = GetCacheValue(subpath);

//					if (fileStream != null)
//					{
//						fileInfo = new MinifiedFileInfo(fileInfo, fileStream);
//						Logger.LogInformation("Request for {0} served from cache", fileInfo.Name);
//						return fileInfo;
//					}

//					// minify
//					switch (Path.GetExtension(fileInfo.Name).ToLower())
//					{
//						case ".js":
//							if (this.Options.MinifyJs)
//							{ 
//								if (!Path.GetFileName(fileInfo.Name).EndsWith(".min.js"))
//								{
//									reader = new StreamReader(fileInfo.CreateReadStream(), System.Text.Encoding.Default, true, 1024, true);
//									fileInfo = new MinifiedFileInfo(fileInfo, MinifyJs(reader.ReadToEnd()));
//									SetCacheValue(fileInfo.Name, fileInfo.CreateReadStream());
//									Logger.LogInformation("Minified {0}", fileInfo.Name);
//								}
//								else
//								{
//									Logger.LogInformation("Did not minify {0}, filename suffix .min.js indicates that it is already minified", fileInfo.Name);
//								} 
//							}
//							break;

//						case ".css":
//							if (this.Options.MinifyCss)
//							{
//								if (!Path.GetFileName(fileInfo.Name).EndsWith(".min.css"))
//								{
//									reader = new StreamReader(fileInfo.CreateReadStream(), System.Text.Encoding.Default, true, 1024, true);
//									fileInfo = new MinifiedFileInfo(fileInfo, MinifyCss(reader.ReadToEnd()));
//									SetCacheValue(fileInfo.Name, fileInfo.CreateReadStream());
//									Logger.LogInformation("Minified {0}", fileInfo.Name);
//								}
//								else
//								{
//									Logger.LogInformation("Did not minify {0}, filename suffix .min.css indicates that it is already minified", fileInfo.Name);
//								}
//							}
//							break;
//					}
										
//					return fileInfo;
//				}
//			}
//			return new NotFoundFileInfo(subpath);
//		}

//		private static MemoryStream MinifyJs(string code)
//		{
//			MemoryStream result = new();
//			StreamWriter writer = new(result);

//			NUglify.JavaScript.CodeSettings settings = new()
//			{
//				MinifyCode = true
//			};

//			writer.Write(NUglify.Uglify.Js(code, settings).Code);
//			writer.Flush();
			
//			return result;
//		}

//		private static MemoryStream MinifyCss(string code)
//		{
//			MemoryStream result = new();
//			StreamWriter writer = new(result);

//			NUglify.Css.CssSettings settings = new();

//			writer.Write(NUglify.Uglify.Css(code, settings).Code);
//			writer.Flush();

//			return result;
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public IDirectoryContents GetDirectoryContents(string subpath)
//		{
//			return null;
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public IChangeToken Watch(string pattern)
//		{
//			return null;
//		}

//		private string CacheFolder()
//		{
//			return this.FolderOptions.Value.GetDataFolder("Cache\\MinifiedFileProvider");
//		}

//		private void ClearCache()
//		{
//			Logger.LogInformation("Cache folder is {0}.  Clearing cache after restart.", CacheFolder());

//			foreach (string cachedFileName in Directory.EnumerateFiles(CacheFolder()))
//			{
//				try
//				{
//					File.Delete(cachedFileName);
//				}
//				catch (Exception e)
//				{
//					Logger.LogError(e, "An error was encountered clearing the cache");
//				}
//			}
//		}

//		private Stream GetCacheValue(string subpath)
//		{
//			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");
//			if (File.Exists(cachedFilePath))
//			{
//				return new FileStream(cachedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
//			}
//			else
//			{
//				return null;
//			}
//		}

//		private void SetCacheValue(string subpath, Stream stream)
//		{
//			string cachedFilePath = Path.Combine(CacheFolder(), Encode(subpath) + ".cache");

//			try
//			{
//				if (File.Exists(cachedFilePath))
//				{
//					File.Delete(cachedFilePath);
//				}

//				using (Stream cachedFileStream = new FileStream(cachedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
//				{
//					stream.CopyTo(cachedFileStream);
//					stream.Flush();
//				}
//			}
//			catch (IOException e)
//			{
//				// if there is an IO exception here, it generally means that the file is in use, because another process is already 
//				// writing to the cache for the specified subpath.  In this case, we can ignore and return, as the file content already
//				// in the process of being written are the same as what we would be writing.
//				Logger.LogError(e, "An error was encountered writing a cache file named {0}", cachedFilePath);
//			}
//		}

//		/// <summary>
//		/// Return a hex-encoded MD5 hash of the the ath for use as a cache filename.  The MD5 encoding is to remain consitent with te
//		/// scheme used in the MergedFileProvider.  
//		/// </summary>
//		/// <param name="value">Filename (path) to encode.</param>
//		/// <returns>The Base-64 (escaped) MD5 hash of the input value.</returns>
//		private static string Encode(string value)
//		{
//			byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(value);

//			using (MD5 md5 = MD5.Create())
//			{
//				return BitConverter.ToString(md5.ComputeHash(valueBytes)).Replace("-", "");
//				//return Convert.ToBase64String(md5.ComputeHash(valueBytes)).Replace("+", "%2B").Replace("/", "%2F");
//			}
//		}
//	}
//}

