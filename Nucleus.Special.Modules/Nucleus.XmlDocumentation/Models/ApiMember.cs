using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models.Serialization;

namespace Nucleus.XmlDocumentation.Models
{
	public class ApiMember
	{
		public enum MemberTypes
		{
			Namespace,
			Constructor,
			Class,
			Field,
			Property,
			Method,
			Event,
			Unknown
		}

		public string Name { get; set; }
		public MemberTypes Type { get; private set; }
		public string FullName { get; }
		public string IdString { get; }
		public string Namespace { get; }

		public string ClassName { get; }
		public string Parameters { get; set; }

		public MixedContent Summary { get; set; }

		public MixedContent Returns { get; set; }

		public MixedContent Remarks { get; set; }

		public Event[] Events { get; set; }
		public Serialization.Exception[] Exceptions { get; set; }

		public string[] Examples { get; set; }

		public Value[] Values { get; set; }

		public Param[] Params { get; set; }
		public TypeParam[] TypeParams { get; set; }

		public SeeAlso[] SeeAlso { get; set; }

		private System.Text.RegularExpressions.Match _match;

		/// <summary>
		/// Constructor which only parses the id, type and fullname
		/// </summary>
		/// <param name="member"></param>
		public ApiMember(Models.Serialization.Member member)
		{
			this.Summary = member.Summary;
			this.Returns = member.Returns;
			this.Remarks = member.Remarks;
			this.Examples = member.Examples;
			this.Params = member.Params;
			this.Values = member.Values;
			this.SeeAlso = member.SeeAlso;
			this.TypeParams = member.TypeParams;

			// Parse name value like: P:Nucleus.Abstractions.Models.Paging.PagingSettings.PageControlNumbers
			// ID string format: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/
			_match = System.Text.RegularExpressions.Regex.Match(member.Name, "^([A-Z]{1}):([^\\(\\)]*)");
			if (_match.Success)
			{
				this.IdString = member.Name;// match.Groups[2].Value;

				this.Type = ParseMemberType(_match.Groups[1].Value);
				this.FullName = ParseName(_match.Groups[2].Value);
			}
		}

		/// <summary>
		/// Constructor which fully parses the member
		/// </summary>
		/// <param name="member"></param>
		/// <param name="assemblyName"></param>
		public ApiMember(Models.Serialization.Member member, string assemblyName) : this(member)
		{
			{
				if (_match.Success)
				{
					if (this.Type == MemberTypes.Class)
					{
						this.Name = this.FullName.Substring(assemblyName.Length + 1);  //ParseName(match.Groups[2].Value.Substring(match.Groups[2].Value.LastIndexOf('.') + 1));
						this.ClassName = this.FullName; // ParseName(this.FullName.Substring(0, this.FullName.LastIndexOf('.')));
					}
					else
					{
						this.Name = ParseName(_match.Groups[2].Value.Substring(_match.Groups[2].Value.LastIndexOf('.') + 1));
						this.ClassName = ParseName(this.FullName.Substring(0, this.FullName.LastIndexOf('.')));
					}

					if (this.Name.Contains("#ctor"))
					{
						this.Type = MemberTypes.Constructor;
						this.Name = this.ClassName;
					}

					this.Namespace = this.ClassName.Substring(0, this.ClassName.LastIndexOf('.'));
				}
			}

			// Parse the parameters signature, which contains a comma-separated list of types
			{
				System.Text.RegularExpressions.Match matchParams = System.Text.RegularExpressions.Regex.Match(member.Name, "\\((.*)\\)");
				if (matchParams.Success)
				{
					this.Parameters = ParseParameters(matchParams.Groups[1].Value.Replace(",", ", "));
				}
			}

			// Match parameter types from the member type with documented parameters.  The parameters come from the member's ID string, documentation
			// on encoding is at https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments
			// This code assumes that the order in which parameters are documented in code comments matches the order that they appear in the method
			// signature.
			if (!String.IsNullOrEmpty(this.Parameters) && this.Params != null)
			{
				System.Text.RegularExpressions.Match matchParamList = System.Text.RegularExpressions.Regex.Match(this.Parameters, "([^{}]+?|.*{(.*)})(?:,|$| ,)");

				int count = 0;
				while (matchParamList.Success)
				{
					if (this.Params.Length > count)
					{
						if (matchParamList.Groups[1].Value.EndsWith('@'))
						{
							this.Params[count].IsRef = true;
						}
						this.Params[count].Type = matchParamList.Groups[1].Value.Replace('{', '(').Replace('}', ')').Replace("@", String.Empty);
					}

					matchParamList = matchParamList.NextMatch();
					count++;
				}
			}

			if (!String.IsNullOrEmpty(this.Parameters))
			{
				this.Parameters = this.Parameters.Replace("@", String.Empty);
			}
		}

		/// <summary>
		/// Parse a member name, which may include a ``N suffix, where N is an integer which represents the number of generic type parameters.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private string ParseName(string value)
		{
			return System.Text.RegularExpressions.Regex.Replace(value, "([`]{1,2}[0-9]{1,3})", new System.Text.RegularExpressions.MatchEvaluator(ReplaceGenericTypeParameterCount));
		}

		string ReplaceGenericTypeParameterCount(System.Text.RegularExpressions.Match match)
		{
			string value = match.Value.Replace("`", string.Empty);
			if (int.TryParse(value, out int parsedValue))
			{
				string result = "";

				for (int count = 0; count < parsedValue; count++)
				{
					if (!String.IsNullOrEmpty(result))
					{
						result += ", ";
					}

					result += $"T{count}";
				}

				return $"<{result}>";
			}
			else
			{
				return "<?>";
			}
		}

		/// <summary>
		/// Parse a parameter name, which may include {`N} after each parameter, where N is an integer (starting at zero) which represents the index of the 
		/// generic type parameter within the class type parameters.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private string ParseParameters(string value)
		{
			int matchIndex = 0;
			string result = "";

			value = System.Text.RegularExpressions.Regex.Replace(value, "[\\{]{0,1}[`]{1,2}([0-9]{1,3})[\\}]{0,1}", new System.Text.RegularExpressions.MatchEvaluator(ReplaceGenericParameter));

			// get each parameter from the comma-separated list of parameters
			System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(value, "(.+?)(?:,|$)");

			foreach (System.Text.RegularExpressions.Match match in matches)
			{
				if (match.Success)
				{
					// The first element of the GroupCollection object (the element at index 0) returned by the Groups property contains a string
					// that matches the entire regular expression pattern.  We want the captured group, which starts at position 1
					string parameter = match.Groups[1].Value;

					parameter = SimplifyParameterType(parameter);

					if (!String.IsNullOrEmpty(result))
					{
						result += ", ";
					}

					if (this.Params?.Length > matchIndex)
					{
						// parameter found, add parameter name to the parameters string
						result += $"{parameter} {this.Params[matchIndex].Name}";
					}
					else
					{
						// missing parameter, leave as-is
						result += $"{parameter}";
					}

				}

				matchIndex++;
			}

			return result;
			//return value;
		}

		/// <summary>
		/// Simplify simple types in the System namespace (Change System.Int32 to just "Int32") 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		string SimplifyParameterType(string value)
		{
			System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(value, "System.([^.]*)$");

			if (match.Success)
			{
				return match.Groups[1].Value;
			}
			else
			{
				return value;
			}
		}

		string ReplaceGenericParameter(System.Text.RegularExpressions.Match match)
		{
			//string value = match.Value.Replace("`", string.Empty);
			if (int.TryParse(match.Groups[1].Value, out int parsedValue))
			{
				return $"<T{parsedValue}>";
			}
			else
			{
				return "<?>";
			}
		}

		private MemberTypes ParseMemberType(string value)
		{
			switch (value)
			{
				case "N":
					return MemberTypes.Namespace;
				case "T":
					return MemberTypes.Class;
				case "F":
					return MemberTypes.Field;
				case "P":
					return MemberTypes.Property;
				case "M":
					return MemberTypes.Method;
				case "E":
					return MemberTypes.Event;
				default:
					return MemberTypes.Unknown;

			}
		}
	}
}
