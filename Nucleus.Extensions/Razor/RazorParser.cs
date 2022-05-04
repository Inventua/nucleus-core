using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngineCore;

namespace Nucleus.Extensions.Razor
{
	internal class RazorParser
	{
		public static string Parse<TRazorTemplateBase>(string template, Object model)
			where TRazorTemplateBase : MailRazorTemplateBase
		{
			// https://github.com/adoconnection/RazorEngineCore
			IRazorEngine engine = new RazorEngine();
			IRazorEngineCompiledTemplate<MailRazorTemplateBase> compiledTemplate;

			compiledTemplate = engine.Compile<TRazorTemplateBase>(template, BuildRazorOptions);

			return compiledTemplate.Run(template =>
			{
				template.Model = model;
			});

		}
		private static void BuildRazorOptions(IRazorEngineCompilationOptionsBuilder builder)
		{
			builder.AddAssemblyReference(typeof(Nucleus.Extensions.AssemblyExtensions).Assembly);
			builder.AddAssemblyReference(typeof(Nucleus.Abstractions.Models.Page).Assembly);
		}


	}
}
