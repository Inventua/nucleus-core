using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models;
using Nucleus.XmlDocumentation.Models.Serialization;

namespace Nucleus.XmlDocumentation
{
	public class DocumentationParser
	{
		private Models.Serialization.Documentation Source { get; }
		public string SourceFileName { get; }
		public Boolean IsValid { get; }

		public DocumentationParser(System.IO.Stream input, string sourceFileName)
		{
			this.SourceFileName = sourceFileName;
			this.Source = DeserializeDocumentationFile(input);
			this.IsValid = true;
			input.Close();
		}

		/// <summary>
		/// Parse deserialized values from xml and return an ApiDocument.
		/// </summary>
		public Models.ApiDocument Document
		{
			get
			{
				Models.ApiDocument result = new()
				{
					AssemblyName = this.Source.Assembly.Name,
					Namespace = new ApiNamespace(FindNamespace(this.Source.Members)),
					SourceFileName = this.SourceFileName
				};

				Dictionary<string, ApiClass> classes = new();
				ApiClass apiClass = null;

				foreach (Models.Serialization.Member member in this.Source.Members.Where(member => member.Hidden == null))
				{
					ApiMember apiMember = new(member, this.Source.Assembly.Name);

					switch (apiMember.Type)
					{
						case ApiMember.MemberTypes.Namespace:
							break;

						case ApiMember.MemberTypes.Class:
						case ApiMember.MemberTypes.Interface:
							// Special handling for namespace documentation
							if (apiMember.Name == "NamespaceDoc")
							{
								result.Namespace.Summary = apiMember.Summary;
								result.Namespace.Remarks = apiMember.Remarks;
								result.Namespace.Examples = apiMember.Examples;
							}
							else
							{
								apiClass = new()
								{
									IdString = apiMember.IdString,
									Name = apiMember.Name,
									Type = apiMember.Type,
									FullName = apiMember.FullName,
									Namespace = apiMember.Namespace,
									AssemblyName = this.Source.Assembly.Name,
									Summary = apiMember.Summary,
									Examples = apiMember.Examples,
									Remarks = apiMember.Remarks,
									SeeAlso = apiMember.SeeAlso,
									TypeParams = apiMember.TypeParams,
									Internal = apiMember.Internal
								};

								ParseTypeParams(apiClass);

								classes.Add(apiClass.FullName, apiClass);
							}

							break;

						case ApiMember.MemberTypes.Constructor:
							apiClass = Find(classes, apiMember.ClassName);
							if (apiClass != null)
							{
								ParseGenericTypeParams(apiClass, apiMember);
								apiClass.Constructors.Add(apiMember);
							}
							break;

						case ApiMember.MemberTypes.Field:
							apiClass = Find(classes, apiMember.ClassName);
							if (apiClass != null)
							{
								ParseGenericTypeParams(apiClass, apiMember);
								apiClass.Fields.Add(apiMember);
							}
							break;

						case ApiMember.MemberTypes.Property:
							apiClass = Find(classes, apiMember.ClassName);
							if (apiClass != null)
							{
								ParseGenericTypeParams(apiClass, apiMember);
								apiClass.Properties.Add(apiMember);
							}
							break;

						case ApiMember.MemberTypes.Method:
							apiClass = Find(classes, apiMember.ClassName);
							if (apiClass != null)
							{
								ParseGenericTypeParams(apiClass, apiMember);
								apiClass.Methods.Add(apiMember);
							}
							break;

						case ApiMember.MemberTypes.Event:
							apiClass = Find(classes, apiMember.ClassName);
							if (apiClass != null)
							{
								ParseGenericTypeParams(apiClass, apiMember);
								apiClass.Events.Add(apiMember);
							}
							break;

						case ApiMember.MemberTypes.Unknown:
							break;
					}
										
					apiMember.UniqueId = $"{apiMember.Name}" + (apiMember.Params?.Any() == true ? $"({apiMember.Parameters.GetSimpleParameterTypes().Replace(" ", "")})" : "");
				}

				result.Classes = classes.Values.ToList();

				return result;
			}
		}


		private static void SetTypeUrl(Param param, List<Models.ApiDocument> results)
		{
			if (String.IsNullOrEmpty(param.Type)) return;

			string rootNamespace = param.Type.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

			if (string.IsNullOrEmpty(rootNamespace)) return;

			// dont make a doc page link for generic types like List<string> as there is no such documentation page
			if (param.Type.Contains("<"))  
			{
				return;
			}
			
			switch (rootNamespace)
			{
				case "Microsoft":
				case "System":
					param.Url = $"https://docs.microsoft.com/en-us/dotnet/api/{param.Type}";
					break;
				case "Nucleus":
					string namespaceName = param.Type.Substring(0, param.Type.LastIndexOf('.'));

					while (!results.Where(apiDoc => apiDoc.Namespace.Name == namespaceName).Any())
					{
						int position = namespaceName.LastIndexOf('.');
						if (position<=0) break;
						namespaceName = namespaceName.Substring(0, position);
					}

					if (String.IsNullOrEmpty(namespaceName)) return;
					param.Url = $"https://www.nucleus-cms.com/api-documentation/{namespaceName}.xml/{param.Type}/#{param.Type}";
					break;
			}
		}


		/// <summary>
		/// Simplify simple types in the System namespace (Change System.Int32 to just "Int32") 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string SimplifyParameterType(string value)
		{
			System.Text.RegularExpressions.Match matchNullable = System.Text.RegularExpressions.Regex.Match(value.Trim(), "^[ ]*System.Nullable{(.*)}$");
			if (matchNullable.Success)
			{
				return SimplifyParameterType(matchNullable.Groups[1].Value) + "?";
			}

			System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(value.Trim(), "^[ ]*System.([^.]*)$");

			if (match.Success)
			{
				return match.Groups[1].Value.Replace("@", String.Empty);
			}
			else
			{
				return value.Replace("@", String.Empty);
			}
		}

		/// <summary>
		/// Replace generic type parameters in the api class name with the correct names.
		/// </summary>
		/// <param name="apiClass"></param>
		/// <returns></returns>
		/// <remarks>
		/// In the xml document file, classes with type parameters hava a "member" name followed by `N, where N is the number of generic
		/// type parameters.  The apiMember class parses this value and substitites numbered type name, so the member name ends up like 
		/// MemberName&lt;T0,T1&gt;.  This function matches the T0, T1 ... type parameters and substitutes the number for the actual 
		/// typeparam name, if typeparam elements have been included in XML comments for the class.
		/// </remarks>
		private static void ParseTypeParams(ApiClass apiClass)
		{
			apiClass.Name = ParseGenericTypeParams(apiClass, null, apiClass.Name);			
		}

		/// <remarks>
		/// In the xml document file, methods, properties and other members with type parameters hava a "member" name followed by `N, where N is the 
		/// number of generic type parameters.  The apiMember class parses this value and substitites numbered type name, so the member name ends 
		/// up like MemberName&lt;T0,T1&gt;.  This function matches the T0, T1 ... type parameters and substitutes the number for the actual 
		/// typeparam name, if typeparam elements have been included in XML comments for the class.
		private static void ParseGenericTypeParams(ApiClass apiClass, ApiMember apiMember)
		{
			if (apiMember.Params != null)
			{
				foreach (Param param in apiMember.Params)
				{
					param.Type = ParseGenericTypeParams(apiClass, apiMember, param.Type);
				}

				apiMember.Parameters = ParseGenericTypeParams(apiClass, apiMember, apiMember.Parameters);
			}

			apiMember.Name = ParseGenericTypeParams(apiClass, apiMember, apiMember.Name);
		}

		/// <remarks>
		/// In the xml document file, classes with type parameters hava a "member" name followed by `N, where N is the number of generic type 
		/// parameters.  The apiMember class parses this value and substitites numbered type name, so the member name ends up like MemberName&lt;T0,T1&gt;.  
		/// This function matches the T0, T1 ... type parameters in the supplied value and substitutes the number for the actual typeparam name, 
		/// if typeparam  elements have been included in XML comments for the class.
		private static string ParseGenericTypeParams(ApiClass apiClass, ApiMember apiMember, string value)
		{
			const string GENERIC_PARMS_REGEX = "<(?<genericTypes>.*)>";
			//const string GENERIC_TYPE_REGEX = "(?<genericType>T[0-9]{1,3})";
			if (value == null) return null;

			return System.Text.RegularExpressions.Regex.Replace(value, GENERIC_PARMS_REGEX, (System.Text.RegularExpressions.Match match) => ReplaceTypeParams(apiClass, apiMember, match));

			//System.Text.RegularExpressions.Match typeParams = System.Text.RegularExpressions.Regex.Match(value, GENERIC_PARMS_REGEX);

			//if (typeParams.Success)
			//{
			//	return System.Text.RegularExpressions.Regex.Replace(value, GENERIC_TYPE_REGEX, ReplaceTypeParams(apiClass, apiMember, typeParams));
			//}
			//else
			//{
			//	return value.Replace("{", "<").Replace("}", ">");
			//}						
		}

		/// <summary>
		/// Replace a matched value representing a generic type (in the form TN, where N is an integer) with the param name in the ordinal 
		/// position indicated by N.
		/// </summary>
		/// <param name="apiClass"></param>
		/// <param name="apiMember"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		public static string ReplaceTypeParams(ApiClass apiClass, ApiMember apiMember, System.Text.RegularExpressions.Match match)
		{
			string result = "";

			System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(match.Value, "(T[0-9]{1,3})");

			if (matches.Any())
			{
				foreach (System.Text.RegularExpressions.Match typeParamMatch in matches)
				{
					// Type params are represented in the member name (by ApiMember.ReplaceGenericParameterCount) as T1, T2, ...
					if (typeParamMatch.Value.StartsWith("T") && typeParamMatch.Value.Length > 1)
					{
						if (int.TryParse(typeParamMatch.Value[1..], out int typeParamIndex))
						{
							if (!String.IsNullOrEmpty(result))
							{
								result += ", ";
							}

							if (apiMember != null && apiMember.TypeParams != null && apiMember.TypeParams.Length > typeParamIndex)
							{
								result += apiMember.TypeParams[typeParamIndex].Name;
							}
							else if (apiClass.TypeParams != null && apiClass.TypeParams.Length > typeParamIndex)
							{
								result += apiClass.TypeParams[typeParamIndex].Name;
							}
							else
							{
								// The typeparam comment is missing for the specified API class		
								System.Diagnostics.Debug.WriteLine($"{apiClass.FullName}{(apiMember == null ? "" : ", " + apiMember.FullName)} missing typeParam.");
								result += $"T{typeParamIndex}";
							}
						}
					}
				}

				return $"<{result}>";
			}
			else
			{
				return match.Value;
			}
		}

		public static void ParseParams(List<Models.ApiDocument> results)
		{
			foreach (ApiDocument apiDocument in results)
			{
				foreach (ApiClass apiClass in apiDocument.Classes.ToList())
				{
					foreach (ApiMember member in apiClass.AllMembers)
					{
						if (member.Params != null)
						{
							if (member.Params != null)
							{
								foreach (Param param in member.Params)
								{
									SetTypeUrl(param, results);
									param.Type = SimplifyParameterType(param.Type);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Perform additional parsing of mixed content properties.
		/// </summary>
		/// <param name="results"></param>
		/// <remarks>
		/// Parse cref and href values for see/seealso, remove extra spaces and return/line feed characters from string values.
		/// </remarks>
		public static void ParseMixedContent(List<Models.ApiDocument> results)
		{
			foreach (ApiDocument apiDocument in results)
			{
				ParseCrefs(results, apiDocument.Namespace.Summary);
				ParseCrefs(results, apiDocument.Namespace.Remarks);
				
				TrimStrings(apiDocument.Namespace.Summary);
				TrimStrings(apiDocument.Namespace.Remarks);

				foreach (ApiClass apiClass in apiDocument.Classes.ToList())
				{
					ParseCrefs(results, apiClass);

					ParseCrefs(results, apiClass.Remarks);
					ParseCrefs(results, apiClass.Summary);
					ParseCrefs(results, apiClass.Internal);

					TrimStrings(apiClass.Remarks);
					TrimStrings(apiClass.Summary);
					TrimStrings(apiClass.Internal);

					foreach (ApiMember member in apiClass.AllMembers)
					{
						ParseCrefs(results, member);

						ParseCrefs(results, member.Remarks);
						ParseCrefs(results, member.Returns);
						ParseCrefs(results, member.Summary);
						ParseCrefs(results, member.Internal);

						if (member.Params != null)
						{
							foreach (Param param in member.Params)
							{
								ParseCrefs(results, param);
							}
						}

						TrimStrings(member.Remarks);
						TrimStrings(member.Returns);
						TrimStrings(member.Summary);
						TrimStrings(member.Internal);
					}
				}
			}
		}

		/// <summary>
		/// String values parsed from XML into the MixedContent class can contain additional spaces and CR/LF characters - remove them
		/// </summary>
		/// <param name="content"></param>
		private static void TrimStrings(MixedContent content)
		{
			if (content?.Items == null) return;

			for (int count=0; count < content.Items.Length; count++)
			{
				if (content.Items[count] is System.String)
				{
					content.Items[count] = (content.Items[count] as System.String).Replace("\r", "").Replace("\n", "");
				}
			}
		}

		/// <summary>
		/// Parse see/seealso values for an apiClass
		/// </summary>
		/// <param name="results"></param>
		/// <param name="apiClass"></param>
		private static void ParseCrefs(List<Models.ApiDocument> results, ApiClass apiClass)
		{
			ParseSeeAlso(apiClass.SeeAlso, results);
		}

		/// Parse see/seealso values for an apiMember
		private static void ParseCrefs(List<Models.ApiDocument> results, ApiMember apiMember)
		{
			if (apiMember.Exceptions != null && apiMember.Exceptions.Any())
			{
				foreach (Models.Serialization.Exception exceptionItem in apiMember.Exceptions)
				{
					if (!String.IsNullOrEmpty(exceptionItem.CodeReference))
					{
						// find the referenced item in results
						exceptionItem.Uri = BuildCrefUrl(results, exceptionItem.CodeReference, out string foundReferenceName);
						if (String.IsNullOrEmpty(exceptionItem.Description) && foundReferenceName != null)
						{
							exceptionItem.Description = foundReferenceName;
						}
					}
				}
			}

			if (apiMember.Events != null && apiMember.Events.Any())
			{
				foreach (Event eventItem in apiMember.Events)
				{
					if (!String.IsNullOrEmpty(eventItem.CodeReference))
					{
						// find the referenced item in results
						eventItem.Uri = BuildCrefUrl(results, eventItem.CodeReference, out string foundReferenceName);
						if (String.IsNullOrEmpty(eventItem.Description) && foundReferenceName != null)
						{
							eventItem.Description = foundReferenceName;
						}
					}
				}
			}

			ParseSeeAlso(apiMember.SeeAlso, results);						
		}

		/// <summary>
		/// Parse see/seelso values in the specified MixedContent instance.
		/// </summary>
		/// <param name="results"></param>
		/// <param name="content"></param>
		private static void ParseCrefs(List<Models.ApiDocument> results, MixedContent content)
		{
			if (content != null && content.Items?.Any() == true)
			{
				foreach (var item in content.Items)
				{
					if (item is See)
					{
						ParseSee(item as See, results);
					}
					else if (item is SeeAlso)
					{
						ParseSeeAlso(item as SeeAlso, results);
					}
				}
			}
		}


		private static void ParseSeeAlso(SeeAlso[] seeAlso, List<Models.ApiDocument> results)
		{
			if (seeAlso != null)
			{
				foreach (SeeAlso seeAlsoItem in seeAlso)
				{
					ParseSeeAlso(seeAlsoItem, results);
				}
			}
		}

		/// <summary>
		/// Parse the href or cref property and set the Uri property for a seeAlso item, and if the link text is empty, set it to 
		/// the Uri text (for hrefs) or to the matched member name (for crefs)
		/// </summary>
		/// <param name="seeAlsoItem"></param>
		/// <param name="results"></param>
		private static void ParseSeeAlso(SeeAlso seeAlsoItem, List<Models.ApiDocument> results)
		{
			if (!String.IsNullOrEmpty(seeAlsoItem.Href))
			{
				if (Uri.TryCreate(seeAlsoItem.Href, UriKind.Absolute, out Uri uri))
				{
					seeAlsoItem.Uri = uri;
					if (String.IsNullOrEmpty(seeAlsoItem.LinkText))
					{
						seeAlsoItem.LinkText = seeAlsoItem.Uri.ToString();
					}
				}
			}
			else if (!String.IsNullOrEmpty(seeAlsoItem.CodeReference))
			{
				// find the referenced item in results
				seeAlsoItem.Uri = BuildCrefUrl(results, seeAlsoItem.CodeReference, out string foundReferenceName);
				if (String.IsNullOrEmpty(seeAlsoItem.LinkText) && foundReferenceName != null)
				{
					seeAlsoItem.LinkText = foundReferenceName;
				}
			}
		}

		/// Parse the href or cref property and set the Uri property for a see item, and if the link text is empty, set it to 
		/// the Uri text (for hrefs) or to the matched member name (for crefs)
		private static void ParseSee(See seeItem, List<Models.ApiDocument> results)
		{
			if (!String.IsNullOrEmpty(seeItem.Href))
			{
				if (Uri.TryCreate(seeItem.Href, UriKind.Absolute, out Uri uri))
				{
					seeItem.Uri = uri;
					if (String.IsNullOrEmpty(seeItem.LinkText))
					{
						seeItem.LinkText = seeItem.Uri.ToString();
					}
				}
			}
			else if (!String.IsNullOrEmpty(seeItem.CodeReference))
			{
				// find the referenced item in results
				seeItem.Uri = BuildCrefUrl(results, seeItem.CodeReference, out string foundReferenceName);
				if (String.IsNullOrEmpty(seeItem.LinkText) && foundReferenceName != null)
				//if (foundReferenceName != null)
				{
					seeItem.LinkText = foundReferenceName;
				}
			}
		}

		/// <summary>
		/// Find the referenced item in results & generate a link Uri
		/// </summary>
		/// <param name="results"></param>
		/// <param name="memberIdString"></param>
		/// <param name="foundReferenceName"></param>
		/// <returns></returns>
		private static System.Uri BuildCrefUrl(List<Models.ApiDocument> results, string memberIdString, out string foundReferenceName)
		{
			foreach (ApiDocument document in results)
			{
				if (document.Namespace.Name == $"N:{memberIdString}") 
				{
					foundReferenceName = document.Namespace.Name;
					return new System.Uri($"{document.SourceFileName}/{document.Namespace.Name}/", UriKind.Relative);
				}

				foreach (ApiClass apiClass in document.Classes.Where(apiClass => apiClass.IdString == memberIdString || apiClass.AllMembers.Where(member => member.IdString == memberIdString).Any()))   // //document.Classes)
				{
					if (apiClass.IdString == memberIdString)
					{
						foundReferenceName = apiClass.Name;
						return new System.Uri($"{document.SourceFileName}/{apiClass.FullName}/", UriKind.Relative);
					}

					ApiMember apiMember = apiClass.AllMembers.Where(member => member.IdString == memberIdString).SingleOrDefault();
					if (apiMember != null)
					{
						foundReferenceName = apiMember.Name;
						return new System.Uri($"{document.SourceFileName}/{apiMember.ClassName}/#{apiMember.Name}", UriKind.Relative);
					}
				}
			}

			if (memberIdString.StartsWith("T:Microsoft"))
			{
				foundReferenceName = memberIdString.Substring(2);
				return new System.Uri($"https://docs.microsoft.com/en-us/dotnet/api/{foundReferenceName}");
			}

			foundReferenceName = null;
			return null;
		}

		/// <summary>
		/// Find the class in results and return the apiClass representing it.
		/// </summary>
		/// <param name="classes"></param>
		/// <param name="fullClassName"></param>
		/// <returns></returns>
		private ApiClass Find(Dictionary<string, ApiClass> classes, string fullClassName)
		{
			KeyValuePair<string, ApiClass> entry = classes.Where(cls => cls.Key == fullClassName).FirstOrDefault();
			return entry.Value;
		}

		/// <summary>
		/// Deserialize the XML from the input stream.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static Models.Serialization.Documentation DeserializeDocumentationFile(System.IO.Stream input)
		{
			System.Xml.Serialization.XmlSerializer xmlSerializer = new(typeof(Models.Serialization.Documentation));
			Models.Serialization.Documentation result = xmlSerializer.Deserialize(input) as Models.Serialization.Documentation;
			return result;
		}

		/// <summary>
		/// Loop through members and return the longest common prefix among all members (the base namespace).
		/// </summary>
		/// <param name="members"></param>
		/// <returns></returns>
		private string FindNamespace(Models.Serialization.Member[] members)
		{
			string ns = "";
			string prefix;

			if (members == null || members.Length <= 1)
			{
				return "";  // no resources, or only one resource, so we can't find the namespace that they have in common
			}
			else
			{
				foreach (Member member in members)
				{
					ApiMember apimember = new(member);

					if (apimember.FullName[ns.Length..].IndexOf(".") > 0)
					{
						prefix = apimember.FullName.Substring(0, ns.Length + apimember.FullName[ns.Length..].IndexOf(".") + 1);
					}
					else
					{
						break;
					}

					if (AllMethodsStartsWith(members, prefix))
					{
						ns = prefix;
					}
					else
					{
						break;
					}
				}
			}

			if (ns.EndsWith('.'))
			{
				ns = ns[0..^1];
			}

			return ns;
		}

		private static Boolean AllMethodsStartsWith(Models.Serialization.Member[] members, string prefix)
		{
			foreach (Member value in members)
			{
				ApiMember member = new(value);
				
				if (!member.FullName.StartsWith(prefix))
					return false;
			}
			return true;
		}

	}
	
}
