using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// Parse a template string using both the Razor parser and the Simple parser.
	/// </summary>
	public static class TemplateParser
	{
		/// <summary>
		/// Parse a template string.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="model"></param>
		/// <typeparam name="TModel">Type of the model object used as input data.</typeparam>
		/// <returns></returns>
		public static async Task<string> ParseTemplate<TModel>(this string template, TModel model)
			where TModel : class
		{
			string result;

			// The RazorParser (RazorEngineCore) does not support anonymous types.
			if (IsAnonymousType(typeof(TModel)))
			{
				// nameof(TModel) does not work because nameof(C#) doesn't work with generic types.
				throw new ArgumentException("TModel may not be an anonymous type.", "TModel");
			}

			// Parse the template as Razor 
			result = await RazorParser.Parse<TModel>(template, model);

			// Parse the template as "simple"
			result = SimpleParser.Parse(result, model);

			return result;
		}

		/// <summary>
		/// Return whether the specified type is an anonymous type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <remarks>
		/// This code is from https://web.archive.org/web/20130817025324/http://www.liensberger.it/web/blog/?p=191.
		/// </remarks>
		private static bool IsAnonymousType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
					&& type.IsGenericType && type.Name.Contains("AnonymousType")
					&& (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
					&& type.Attributes.HasFlag(TypeAttributes.NotPublic);
		}

	}
}
