using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Api Key signature extensions.
	/// </summary>
	public static class HttpRequestExtensions
	{
		// Specifies the allowed variation between signature dates and the server date
		private static TimeSpan DateThreshold = TimeSpan.FromMinutes(10);

		/// <summary>
		/// Add a signature to the specified HttpRequestMessage
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="secret"></param>
		public static void Sign(this System.Net.Http.HttpRequestMessage request, Guid accessKey, string secret)
		{
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $"{accessKey}:{GenerateSignature(request, accessKey, secret)}");
		}

		/// <summary>
		/// Retrieve the access key from an incoming request.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static string AccessKey(this System.Net.Http.HttpRequestMessage request)
		{
			string headerAccessKey = "";
			string headerSignature = "";

			Validate(request, ref headerAccessKey, ref headerSignature);

			return headerAccessKey;
		}

		/// <summary>
		/// Validate request headers.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="signature"></param>
		/// <exception cref="InvalidOperationException"></exception>
		private static void Validate(System.Net.Http.HttpRequestMessage request, ref string accessKey, ref string signature)
		{
			if (request.Headers.Authorization.Scheme != "Bearer")
			{
				throw new InvalidOperationException($"Invalid authorization scheme.");
			}

			string[] authorizationValues = request.Headers.Authorization.Parameter.Split(':');

			if (authorizationValues.Length != 2)
			{
				throw new InvalidOperationException($"Invalid request signature [invalid format]: {request.Headers.Authorization.Parameter}");
			}

			string headerAccessKey = authorizationValues[0];
			string headerSignature = authorizationValues[1];

			if (String.IsNullOrEmpty(headerAccessKey))
			{
				throw new InvalidOperationException($"Invalid request signature [access key blank]: {request.Headers.Authorization.Parameter}");
			}

			if (String.IsNullOrEmpty(headerSignature))
			{
				throw new InvalidOperationException($"Invalid request signature [signature blank]:  {request.Headers.Authorization.Parameter}");
			}

			if (!Guid.TryParse(headerAccessKey, out Guid result))
			{
				throw new InvalidOperationException($"Invalid request signature [access key invalid format]: {request.Headers.Authorization.Parameter}");
			}

			if (request.Headers.Date.HasValue)
			{
				if (request.Headers.Date.Value < DateTime.UtcNow.Add(-DateThreshold) || request.Headers.Date.Value > DateTime.UtcNow.Add(DateThreshold))
				{
					throw new InvalidOperationException($"Invalid request signature [expired]: {request.Headers.Authorization.Parameter}");
				}
			}
			else
			{
				throw new InvalidOperationException($"Invalid request signature [no date header]");
			}
		}

		/// <summary>
		/// Validate request headers and check that the request signature matches a signature generated using the same data and stored secret on the server.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="secret"></param>
		/// <returns></returns>
		public static Boolean IsValid(this System.Net.Http.HttpRequestMessage request, string secret)
		{
			string headerAccessKey = "";
			string headerSignature = "";

			Validate(request, ref headerAccessKey, ref headerSignature);

			return GenerateSignature(request, Guid.Parse(headerAccessKey), secret) == headerSignature;			
		}

		/// <summary>
		/// Generate a HMAC signature string
		/// </summary>
		/// <param name="request"></param>
		/// <param name="accessKey"></param>
		/// <param name="secret"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static string GenerateSignature(System.Net.Http.HttpRequestMessage request, Guid accessKey, string secret)
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

				if (header.Value.Count() > 0)
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
							canonicalizedParameters = canonicalizedParameters + PercentEncode(value);
							if (!hostHeaderProcessed)
							{
								value = request.RequestUri.Host;
								if (value.IndexOf(':') >= 0)
								{
									value = value.Substring(0, value.IndexOf(':'));
								}

								canonicalizedParameters = canonicalizedParameters + PercentEncode(value);
								hostHeaderProcessed = true;
							}

							break;
						}

					case "soapaction":
					case "user-agent":
						{
							canonicalizedParameters = canonicalizedParameters + PercentEncode(value);
							break;
						}

					case "host":
						{
							if (value.IndexOf(':') >= 0)
							{
								value = value.Substring(0, value.IndexOf(':'));
							}

							canonicalizedParameters = canonicalizedParameters + PercentEncode(value);
							break;
						}

					default:
						{
							break;
						}
						// ignore

				}
			}

			localPath = request.RequestUri.LocalPath;
			signatureSource = request.Method.ToString().ToUpper() + "\n" + request.RequestUri.Host.ToLower() + "\n" + localPath + "\n" + canonicalizedParameters;
			return SHA256Hash(signatureSource, secret);
		}

		private static string ConcatenateHeaderValues(IEnumerable<string> values)
		{
			string strResult = "";
			foreach (string strValue in values)
			{
				if (!string.IsNullOrEmpty(strResult))
				{
					strResult = strResult + " ";
				}

				strResult = strResult + strValue;
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
					result = result + character;
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
