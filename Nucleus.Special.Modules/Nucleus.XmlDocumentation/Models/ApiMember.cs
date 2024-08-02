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
			Interface,
			Field,
			Property,
			Method,
			Event,
			Enum,
			Unknown
		}

		public string Name { get; set; }

		public string ControlId()
		{
			return (this.Name + (this.Params?.Any() == true ? $"({this.Parameters.GetSimpleParameterTypes()})" : ""))
				.Replace(" ", "").Replace("<", "").Replace(">", "").Replace(",", "");
		}

		public MemberTypes Type { get; private set; } = MemberTypes.Unknown;
		public string FullName { get; }
		public string IdString { get; }
		public string Namespace { get; }

		public string ClassName { get; }
		public string Parameters { get; set; }

		public MixedContent Summary { get; set; }

		public Returns Returns { get; set; }

		public MixedContent Remarks { get; set; }

		public Event[] Events { get; set; }
		public Serialization.Exception[] Exceptions { get; set; }

		public string[] Examples { get; set; }

		public Value[] Values { get; set; }

		public Param[] Params { get; set; }
		public TypeParam[] TypeParams { get; set; }

		public SeeAlso[] SeeAlso { get; set; }

		public MixedContent Internal { get; set; }

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
			this.Internal = member.Internal;

			if (System.Enum.TryParse<MemberTypes>(member.Type, out MemberTypes type))
			{
				this.Type = type;
			}

			// Parse name value like: P:Nucleus.Abstractions.Models.Paging.PagingSettings.PageControlNumbers
			// ID string format: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/
			_match = System.Text.RegularExpressions.Regex.Match(member.Name, "^([A-Z]{1}):([^\\(\\)]*)");
			if (_match.Success)
			{
				this.IdString = member.Name;

				// Only determine the type if it hasn't been explictly specified in a XML commands <type> element.
				if (this.Type == MemberTypes.Unknown)
				{
					this.Type = ParseMemberType(_match.Groups[1].Value);
					if (this.Type == MemberTypes.Class && _match.Groups[2].Value.Split('.', StringSplitOptions.RemoveEmptyEntries).Last().StartsWith("I"))
					{
						this.Type = MemberTypes.Interface;
					}
				}

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
			if (_match.Success)
			{
				if (this.Type == MemberTypes.Class || this.Type == MemberTypes.Interface || this.Type == MemberTypes.Enum)
				{
					this.Name = this.FullName.Substring(assemblyName.Length + 1);  
					this.ClassName = this.FullName; 
				}
				else
				{
					this.Name = ParseName(_match.Groups[2].Value.Substring(_match.Groups[2].Value.LastIndexOf('.') + 1));
					this.ClassName = ParseName(this.FullName.Substring(0, this.FullName.LastIndexOf('.')));
				}

				if (this.Name.Contains("#ctor"))
				{
					this.Type = MemberTypes.Constructor;
          //this.Name = this.FullName.Substring(0, this.FullName.LastIndexOf('.')).Substring(assemblyName.Length + 1);
          this.Name = this.FullName.Substring(0, this.FullName.LastIndexOf('.')).Substring(assemblyName.Length + 1);
          if (this.Name.LastIndexOf('.') > 0)
          {
            this.Name = this.Name.Substring(this.Name.LastIndexOf('.'));
          }
        }

				this.Namespace = this.ClassName.Substring(0, this.ClassName.LastIndexOf('.'));
			}
			
			// Parse the parameters signature, which contains a comma-separated list of types			
			System.Text.RegularExpressions.Match matchParams = System.Text.RegularExpressions.Regex.Match(member.Name, "\\((.*)\\)");
			if (matchParams.Success)
			{
				this.Parameters = ParseParameters(matchParams.Groups[1].Value.Replace(",", ", "));
			}
			

			// Match parameter types from the member type with documented parameters.  The parameters come from the member's ID string, documentation
			// on encoding is at https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments
			// This code assumes that the order in which parameters are documented in code comments matches the order that they appear in the method
			// signature.
			if (!String.IsNullOrEmpty(this.Parameters) && this.Params != null)
			{
				int count = 0;
					
				foreach(string parameter in this.Parameters.Split(',', StringSplitOptions.RemoveEmptyEntries))
				{
					System.Text.RegularExpressions.Match matchParamList = System.Text.RegularExpressions.Regex.Match(parameter, "(?<type>.*) (?<name>.*)");

					if (matchParamList.Success)
					{
						if (this.Params.Length > count)
						{
							if (matchParamList.Groups[1].Value.EndsWith('@'))
							{
								this.Params[count].IsRef = true;
							}

							this.Params[count].Type = matchParamList.Groups[1].Value
								.Replace("@", "")
								.Replace('{', '<')
								.Replace('}', '>')
								.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

							System.Text.RegularExpressions.Match nullableTypeMatch = System.Text.RegularExpressions.Regex.Match(this.Params[count].Type, @"System.Nullable\((?<type>.*)\)");
							if (nullableTypeMatch.Success)
							{
								this.Params[count].Type = nullableTypeMatch.Groups[1].Value + "?";
							}
						}

						count++;
					}
				}				
			}

			if (!String.IsNullOrEmpty(this.Parameters) && this.Params != null)
			{
				this.Parameters = String.Join(", ", this.Params.Select(param => ParseParameter(param)));
			}
		}
		
		private string ParseParameter(Param param)
		{			
			return $"{GetSimpleParameterType(param.Type)} {param.Name}";
		}

		private static string GetSimpleParameterType(string parameterType)
		{
			if (String.IsNullOrEmpty(parameterType)) return "";

			if (parameterType.Contains('<') && parameterType.Contains('>'))
			{
				System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(parameterType, "(?<type1>[^<]*)<(?<type2>[^>]*)>.*");
				if (match.Success && match.Groups.Count == 3)
				{
					return $"{GetSimpleParameterType(match.Groups[1].Value)}<{GetSimpleParameterType(match.Groups[2].Value)}>";
				}
				else
				{
					return parameterType;
				}
			}
			else
			{
				return parameterType.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
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

					// This is done later in the process, so that we can preserve the "real" parameter types in order to attach Urls, etc
					//parameter = SimplifyParameterType(parameter);

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
