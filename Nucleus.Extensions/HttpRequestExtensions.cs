using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Api Key signature extensions.
	/// </summary>
	public static class HttpRequestExtensions
	{
		// Specifies the allowed variation between signature dates and the server date
		private static readonly TimeSpan DateThreshold = TimeSpan.FromMinutes(10);

		/// <summary>
		/// Scheme name for the authorization header used to send the request signature.
		/// </summary>
		public const string AUTHORIZATION_SCHEME = "Nucleus-HMAC256";

		/// <summary>
		/// Add a signature to the specified HttpRequestMessage
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="secret"></param>
		public static void Sign(this System.Net.Http.HttpRequestMessage request, Guid accessKey, string secret)
		{
			if (!request.Headers.Date.HasValue)
			{
				request.Headers.Date = DateTimeOffset.UtcNow;
			}
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(AUTHORIZATION_SCHEME, $"{accessKey}:{GenerateSignature(request, secret)}");
		}

		/// <summary>
		/// Return whether an incoming request has a API key signature
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <returns></returns>
		public static Boolean IsSigned(this Microsoft.AspNetCore.Http.HttpRequest request, out Guid accessKey)
		{
			return IsSigned(request, out accessKey, out string signature, out string reason);
		}

		/// <summary>
		/// Validate request headers from an incoming request..
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="signature"></param>
		/// <param name="reason"></param>
		/// <exception cref="InvalidOperationException"></exception>
		private static Boolean IsSigned(HttpRequest request, out Guid accessKey, out string signature, out string reason)
		{
      accessKey = Guid.Empty;
      signature = "";
      reason = "";
      
      if (!request.Headers.Authorization.Any())
      {
        reason = $"Request not signed";
        return false;
      }

      if (request.Headers.Authorization.Count > 1)
			{
				reason = $"Invalid request signature [invalid format]: {request.Headers.Authorization}";
				return false;
			}

			string[] headerParts = request.Headers.Authorization[0].Split(' ');			
			if (headerParts.Length != 2)
			{
				reason = $"Invalid request signature [invalid header format]: {request.Headers.Authorization}";
				return false;
			}

			string headerAuthorizationScheme = headerParts[0];
			string[] signatureParts = headerParts[1].Split(':');

			if (signatureParts.Length != 2)
			{
				reason = $"Invalid request signature [invalid signature format]: {request.Headers.Authorization}";
				return false;
			}

			string accessKeyValue = signatureParts[0];
			signature = signatureParts[1];

			if (headerAuthorizationScheme != AUTHORIZATION_SCHEME)
			{
				reason = $"Invalid authorization scheme.";
				return false;
			}			

			if (String.IsNullOrEmpty(accessKeyValue))
			{
				reason = $"Invalid request signature [access key blank]: {request.Headers.Authorization}";
				return false;
			}

			if (String.IsNullOrEmpty(signature))
			{
				reason = $"Invalid request signature [signature blank]:  {request.Headers.Authorization}";
				return false;
			}

			if (!Guid.TryParse(accessKeyValue, out accessKey))
			{
				reason = $"Invalid request signature [access key invalid format]: {request.Headers.Authorization}";
				return false;
			}

			if (request.Headers.Date.Count == 1)
			{
				if (DateTime.TryParse(request.Headers.Date[0], out DateTime requestDate))
				{
					// aspnet core converts the date header to server-local date/time so we need to compare with DateTime.Now, not DateTime.UtcNow
					if (requestDate < DateTime.Now.Add(-DateThreshold) || requestDate > DateTime.Now.Add(DateThreshold))
					{
						reason = $"Invalid request signature [expired]: {request.Headers.Authorization}";
						return false;
					}
				}
				else
				{
					reason = $"Invalid request signature [invalid date header]";
					return false;
				}
			}
			else
			{
				reason = $"Invalid request signature [no date header]";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validate request headers and check that the request signature matches a signature generated using the same data and stored secret on the server.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="secret"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		public static Boolean IsValid(this Microsoft.AspNetCore.Http.HttpRequest request, string secret, out string reason)
		{
			if (IsSigned(request, out Guid accessKey, out string signature, out reason))
			{
				return GenerateSignature(request, accessKey, secret) == signature;
			}
			else
			{
				return false;
			}	
		}

		/// <summary>
		/// Generate a HMAC signature string
		/// </summary>
		/// <param name="request"></param>
		/// <param name="secret"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static string GenerateSignature(System.Net.Http.HttpRequestMessage request, string secret)
		{
			string signatureSource;
			var parameterKeys = new SortedDictionary<string, string>();
			string canonicalizedParameters = "";
			string localPath;
			bool hostHeaderProcessed = false;

			if (String.IsNullOrEmpty(secret))
			{
				throw new ArgumentException("Invalid secret");
			}

			foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
			{
				if (header.Key.ToLower() == "host")
				{
					hostHeaderProcessed = true;
				}

				if (header.Value.Any())
				{
					parameterKeys.Add(header.Key, ConcatenateHeaderValues(header.Value));
				}
			}

			foreach (string parameter in parameterKeys.Keys)
			{
				string name = parameter;
				string value = parameterKeys[parameter];

				switch (name.ToLower() ?? "")
				{
					case "date":
						{
							canonicalizedParameters += PercentEncode(value);
							if (!hostHeaderProcessed)
							{
								value = request.RequestUri.Host;
								if (value.Contains(':'))
								{
									value = value.Substring(0, value.IndexOf(':'));
								}

								canonicalizedParameters += PercentEncode(value);
								hostHeaderProcessed = true;
							}

							break;
						}

					case "user-agent":
						{
							canonicalizedParameters += PercentEncode(value);
							break;
						}

					case "host":
						{
							if (value.Contains(':'))
							{
								value = value.Substring(0, value.IndexOf(':'));
							}

							canonicalizedParameters += PercentEncode(value);
							break;
						}

					default:
						{
							break;
						}
				}
			}

			localPath = request.RequestUri.LocalPath;
			signatureSource = request.Method.ToString().ToUpper() + "\n" + request.RequestUri.Host.ToLower() + "\n" + localPath + "\n" + canonicalizedParameters;
			return SHA256Hash(signatureSource, secret);
		}

		/// <summary>
		/// Generate a signature for an incoming request in order to compare it.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="secret"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static string GenerateSignature(Microsoft.AspNetCore.Http.HttpRequest request, Guid accessKey, string secret)
		{
			string signatureSource;
			var parameterKeys = new SortedDictionary<string, string>();
			string canonicalizedParameters = "";
			string localPath;
			bool hostHeaderProcessed = false;

			if (String.IsNullOrEmpty(secret))
			{
				throw new ArgumentException("Invalid secret");
			}

			foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers)
			{
				if (header.Key.ToLower() == "host")
				{
					hostHeaderProcessed = true;
				}

				if (header.Value.Count > 0)
				{
					parameterKeys.Add(header.Key, ConcatenateHeaderValues(header.Value));
				}
			}

			foreach (string parameter in parameterKeys.Keys)
			{
				string name = parameter;
				string value = parameterKeys[parameter];

				switch (name.ToLower() ?? "")
				{
					case "date":
						{
							canonicalizedParameters += PercentEncode(value);
							if (!hostHeaderProcessed)
							{
								value = request.Host.Host;
								canonicalizedParameters += PercentEncode(value);
								hostHeaderProcessed = true;
							}

							break;
						}

					case "soapaction":
					case "user-agent":
						{
							canonicalizedParameters += PercentEncode(value);
							break;
						}

					case "host":
						{
							if (value.Contains(':'))
							{
								value = value.Substring(0, value.IndexOf(':'));
							}

							canonicalizedParameters += PercentEncode(value);
							break;
						}

					default:
						{
							break;
						}
						// ignore

				}
			}

			localPath = request.Path;
			signatureSource = request.Method.ToString().ToUpper() + "\n" + request.Host.Host.ToLower() + "\n" + localPath + "\n" + canonicalizedParameters;
			return SHA256Hash(signatureSource, secret);
		}

		private static string ConcatenateHeaderValues(IEnumerable<string> values)
		{
			string strResult = "";
			foreach (string strValue in values)
			{
				if (!string.IsNullOrEmpty(strResult))
				{
					strResult += " ";
				}

				strResult += strValue;
			}

			return strResult;
		}

		/// <summary>
		/// Percent-encode the value using "Amazon rules": alphanumeric and -_.~ characters are written as-is, and all other values are percent-encoded.
		/// </summary>
		/// <param name="parameter"></param>
		/// <returns></returns>
		private static string PercentEncode(string parameter)
		{
			string result = "";

			foreach (char character in parameter.ToCharArray())
			{
				if (char.IsLetterOrDigit(character) || new char[] { '-', '_', '.', '~' }.Contains(character))
				{
					result += character;
				}
				else
				{
					result = result + "%" + Convert.ToByte(character).ToString("X2");
				}
			}

			return result;
		}

		private static string SHA256Hash(string Value, string Secret)
		{
			var objData = Encoding.UTF8.GetBytes(Value);
			{
				using (System.Security.Cryptography.HashAlgorithm provider = new System.Security.Cryptography.HMACSHA256(Encoding.ASCII.GetBytes(Secret)))
				{
					return Convert.ToBase64String(provider.ComputeHash(objData, 0, objData.Length));
				}				
			}
		}
	}
}
