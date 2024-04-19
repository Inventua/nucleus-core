using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RazorEngineCore;

// https://github.com/adoconnection/RazorEngineCore

namespace Nucleus.Extensions.Razor
{
  /// <summary>
  /// Razor Parser methods.
  /// </summary>
  public class RazorParser
	{
		private static readonly ConcurrentDictionary<string, object> CompiledTemplateCache = new();
		private readonly static string[] UsingNamespaces = 
    [ 
      "System", 
      "System.Collections.Generic", 
      "System.Linq", 
      "System.Text", 
      "System.Security.Claims", 
      "Nucleus.Extensions", 
      "Nucleus.Abstractions", 
      "Nucleus.Abstractions.Models" 
    ];
    private static readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

		/// <summary>
		/// Compile and execute the specified template, using the supplied model as input.
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <param name="template">Razor-language template.</param>
		/// <param name="model"></param>
		/// <returns></returns>
		public static async Task<string> Parse<TModel>(string template, TModel model)
			where TModel : class
		{
			RazorEngine engine = new();
			IRazorEngineCompiledTemplate<RazorEngineTemplate<TModel>> compiledTemplate = null;
			
			string templateKey = typeof(TModel).FullName + Hash(template);

			if (CompiledTemplateCache.TryGetValue(templateKey, out object cachedTemplate))
			{
				compiledTemplate = cachedTemplate as IRazorEngineCompiledTemplate<RazorEngineTemplate<TModel>>;
			}

      if (compiledTemplate == null)
      {
        // when events are raised in quick succession (like when a data import is in progress), the events hare handled
        // asynchronously, so this function can be running multiple times at once.  We have to use a semaphore to prevent
        // attempts to add the compiled template to CompiledTemplateCache more than once.  
                
        await _cacheSemaphore.WaitAsync();
        try
        {
          // check the CompiledTemplateCache again in case another thread has added a compiled template in between the
          // original CompiledTemplateCache.TryGetValue and here.
          if (CompiledTemplateCache.TryGetValue(templateKey, out object newCachedTemplate))
          {
            compiledTemplate = newCachedTemplate as IRazorEngineCompiledTemplate<RazorEngineTemplate<TModel>>;
          }

          // if another thread has not added the compiled template, compile one and add it to the cache
          if (compiledTemplate == null)
          {
            compiledTemplate = await engine.CompileAsync<RazorEngineTemplate<TModel>>(template, BuildRazorOptions);
            CompiledTemplateCache.TryAdd(templateKey,compiledTemplate);
          }
        }
        finally 
        { 
          _cacheSemaphore.Release(); 
        }
      }

			return await compiledTemplate.RunAsync(instance =>
			{
				instance.Model = model;
			});
		}

		/// <summary>
		/// Test-compile the template, throwing an exception on error.
		/// </summary>
    /// <param name="modelType"></param>
		/// <param name="template">Razor-language template.</param>
		/// <returns></returns>
		public static async Task<RazorValidatorResult> TestCompile(System.Type modelType, string template)
		{
			RazorValidator engine = new();
      Type templateType = typeof(RazorEngineTemplate<>).MakeGenericType(modelType);

      return await engine.TestCompileAsync(modelType, template, BuildRazorOptions);
		}
     
		private static void BuildRazorOptions(IRazorEngineCompilationOptionsBuilder builder)
		{
			builder.Options.TemplateFilename = " ";
			builder.Options.DefaultUsings = [];

			builder.AddAssemblyReference(typeof(System.Uri).Assembly);
			builder.AddAssemblyReference(typeof(System.Collections.Generic.CollectionExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Extensions.AssemblyExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Abstractions.Models.Page).Assembly);

			foreach (string ns in UsingNamespaces)
			{
				builder.AddUsing(ns);
			}
		}

		private static string Hash(string value)
    {
      byte[] objData = Encoding.UTF8.GetBytes(value);
      return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(objData.AsSpan(0, objData.Length)));
    }
  }
}
