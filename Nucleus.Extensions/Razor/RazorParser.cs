using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngineCore;
using Nucleus.Abstractions.Mail;

// https://github.com/adoconnection/RazorEngineCore

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// Razor Parser methods.
	/// </summary>
	public class RazorParser
	{
		private static readonly Dictionary<string, object> CompiledTemplateCache = new();
		private readonly static string[] UsingNamespaces = { "System" ,"System.Collections.Generic", "System.Linq", "System.Text", "Nucleus.Extensions", "Nucleus.Abstractions", "Nucleus.Abstractions.Models" };

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
				compiledTemplate = await engine.CompileAsync<RazorEngineTemplate<TModel>>(template, BuildRazorOptions);
				CompiledTemplateCache.Add(templateKey, compiledTemplate);
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

			builder.AddAssemblyReference(typeof(System.Collections.Generic.CollectionExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Extensions.AssemblyExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Abstractions.Models.Page).Assembly);

			foreach (string ns in UsingNamespaces)
			{
				builder.AddUsing(ns);
			}
			builder.AddUsing("System");
			builder.AddUsing("System.Collections.Generic");
			builder.AddUsing("System.Linq");
			builder.AddUsing("System.Text");

			builder.AddUsing("Nucleus.Extensions");
			builder.AddUsing("Nucleus.Abstractions");
			builder.AddUsing("Nucleus.Abstractions.Models");
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
