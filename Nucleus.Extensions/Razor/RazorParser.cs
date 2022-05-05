using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngineCore;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions.Razor
{
	internal class RazorParser
	{
		private static Dictionary<string, object> CompiledTemplateCache = new();

		public static string Parse<T>(string template, T model)
			where T : MailTemplateModelBase<T>
		{
			// https://github.com/adoconnection/RazorEngineCore
			IRazorEngine engine = new RazorEngine();
			IRazorEngineCompiledTemplate<T> compiledTemplate;
			string templateKey = typeof(T).FullName + Hash(template);

			//if (CompiledTemplateCache.TryGetValue(templateKey, out IRazorEngineCompiledTemplate<T> cachedTemplate))
			//{
			//	compiledTemplate = (IRazorEngineCompiledTemplate<T>)cachedTemplate;
			//}
			//else
			//{
				compiledTemplate = engine.Compile<T>(template, BuildRazorOptions);
				//CompiledTemplateCache.Add(templateKey, (IRazorEngineCompiledTemplate<T>)compiledTemplate);
			//}
			
			return compiledTemplate.Run(instance =>
			{
				instance.Model = model;
			});

		}
		private static void BuildRazorOptions(IRazorEngineCompilationOptionsBuilder builder)
		{
			builder.AddAssemblyReference(typeof(System.Collections.Generic.CollectionExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Extensions.AssemblyExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Abstractions.Models.Page).Assembly);

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
