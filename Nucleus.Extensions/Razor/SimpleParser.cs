using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// Simple template parser.
	/// </summary>
	public class SimpleParser
	{
		private Object Model { get; }

		private Object LoopItemValue { get; set; }
		private string LoopItemKey { get; set; }

		private const string COLLECTION_REGEX = "\\[~(?<key>[A-Za-z]*)[^(]*\\((?<expression>[^]]*?)\\)\\]";
		private const string ITEM_REGEX = "\\(~(.*?)\\)";

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="model"></param>
		private SimpleParser(Object model)
		{
			this.Model = model;
		}

		/// <summary>
		/// Parse a template string, replacing template values in the form (~key.property) with values from the specified model.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public static string Parse(string template, object model)
		{
			SimpleParser parser = new(model);
			string result = template;
			result = System.Text.RegularExpressions.Regex.Replace(result, COLLECTION_REGEX, parser.CollectionMatchEvaluator, System.Text.RegularExpressions.RegexOptions.Singleline);
			result = System.Text.RegularExpressions.Regex.Replace(result, ITEM_REGEX, parser.MatchEvaluator);
			return result;
		}

		private TModel GetValue<TModel>(Object model, string propertyName)
		{
			object result = null;

			if (propertyName == this.LoopItemKey)
			{
				result = this.LoopItemValue;
			}
			else
			{
				System.Reflection.PropertyInfo propInfo = model.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);

				if (propInfo != null && propInfo.CanRead)
				{
					result = propInfo.GetValue(model);
				}
			}

			if (result != null)
			{
				if (typeof(TModel) is object)
				{
					return (TModel)result;
				}
				else
				{
					return (TModel)Convert.ChangeType(result, typeof(TModel));
				}
			}

			return default;
		}

		private string CollectionMatchEvaluator(System.Text.RegularExpressions.Match match)
		{
			string key = match.Groups["key"].Value;
			string expression = match.Groups["expression"].Value;
			StringBuilder result = new();
			System.Collections.IEnumerable value = GetValue<object>(this.Model, key) as System.Collections.IEnumerable;

			if (value != null)
			{
				foreach (object item in value)
				{
					this.LoopItemKey = item.GetType().Name;
					this.LoopItemValue = item;

					result.Append(System.Text.RegularExpressions.Regex.Replace(expression, ITEM_REGEX, MatchEvaluator));
				}
			}

			return result.ToString();
		}

		private string MatchEvaluator(System.Text.RegularExpressions.Match match)
		{
			string key;
			string prop;

			if (match.Groups.Count > 1 && match.Groups[1].Value.Contains('.'))
			{
				key = match.Groups[1].Value.Split('.')[0];
				prop = match.Groups[1].Value[(key.Length + 1)..];
			}
			else
			{
				key = "";
				prop = match.Groups[1].Value;
			}

			switch (key)
			{
				case "":
					// handle standalone values
					return MatchStandaloneValue(prop);
				default:
					return MatchObjectValue(key, prop);
			}
		}

		private string MatchStandaloneValue(string prop)
		{
			switch (prop)
			{
				case "time":
					return DateTime.Now.TimeOfDay.ToString();

				case "date":
					return DateTime.Now.ToString();

				default:
					return (GetValue<string>(this.Model, prop) ?? "");
			}
		}

		private string MatchObjectValue(string key, string prop)
		{
			object value = GetValue<object>(this.Model, key);
			if (value == null)
			{
				return "";
			}

			// loop through '.' separated property name - to handle properties of objects (like User.Secrets.PasswordResetToken).  A single 
			// property (like User.UserName) will only loop through once.
			foreach (string part in prop.Split('.', StringSplitOptions.RemoveEmptyEntries))
			{
				if (value is IDictionary<string, object>)
				{
					if ((value as IDictionary<string, object>).ContainsKey(part))
					{
						value = (value as IDictionary<string, object>)[part];
					}
				}
				else
				{
					return (GetValue<string>(value, part) ?? "").ToString();
				}
			}

			return value.ToString();
		}
	}
}
