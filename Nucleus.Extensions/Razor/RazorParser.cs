using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngineCore;
using Nucleus.Abstractions.Mail;
using System.Threading;
using System.Collections.Concurrent;

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
    { 
      nameof(System), 
      nameof(System.Collections.Generic), 
      nameof(System.Linq), 
      nameof(System.Text), 
      nameof(System.Security.Claims), 
      nameof(Nucleus.Extensions), 
      nameof(Nucleus.Abstractions), 
      nameof(Nucleus.Abstractions.Models) 
    };
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
			IRazorEngine engine = new RazorEngine();
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
		/// <param name="template">Razor-language template.</param>
		/// <returns></returns>
		public static async Task<TestCompileResult> TestCompile(string template)
		{
			IRazorEngine engine = new RazorEngine();
			IRazorEngineCompiledTemplate compiledTemplate = null;

			try
			{
				compiledTemplate = await engine.CompileAsync(template, BuildRazorOptions);
			}
			catch (RazorEngineCompilationException ex)
			{
				return new TestCompileResult(false, ex.Errors.Select(err => $"{err.GetMessage()}, position {err.Location.GetLineSpan().StartLinePosition.Character} in source: <code>{GetSource(ex.GeneratedCode, err)}</code>"));				
			}

			return new TestCompileResult(true);
		}

		private static string GetSource(string code, Microsoft.CodeAnalysis.Diagnostic error)
		{
			string[] codeLines = code.Split(Environment.NewLine);

			Microsoft.CodeAnalysis.FileLinePositionSpan location = error.Location.GetLineSpan();

			return String.Join(" ", codeLines
				.Skip(location.StartLinePosition.Line)
				.Take(location.EndLinePosition.Line - location.StartLinePosition.Line+1));
		}

		/// <summary>
		/// Return value for the TestCompile function.
		/// </summary>
		public class TestCompileResult
		{
			/// <summary>
			/// Indicates that the template was compiled successfully (true) or had errors (false)
			/// </summary>
			public Boolean Success { get; }

			/// <summary>
			/// List of error messages when Success=false.
			/// </summary>
			public IEnumerable<string> Errors { get; }

			internal TestCompileResult(Boolean success)
			{
				this.Success = success;
			}

			internal TestCompileResult(Boolean success, IEnumerable<string> errors)
			{
				this.Success = success;
				this.Errors = errors;
			}
		}

		private static void BuildRazorOptions(IRazorEngineCompilationOptionsBuilder builder)
		{
			builder.Options.TemplateFilename = " ";
			builder.Options.DefaultUsings = new();

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
			var objData = Encoding.UTF8.GetBytes(value);

			using (System.Security.Cryptography.HashAlgorithm provider = System.Security.Cryptography.SHA256.Create())
			{
				return Convert.ToBase64String(provider.ComputeHash(objData, 0, objData.Length));
			}
		}
	}
}
