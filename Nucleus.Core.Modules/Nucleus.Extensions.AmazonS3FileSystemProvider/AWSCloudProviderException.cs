using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	/// <summary>
	/// Represents an exception from a call to an AWSCloudProvider method.
	/// </summary>
	/// <remarks>
	/// AWSCloudProvider methods can also return exceptions of type AmazonS3Exception or general .net exceptions.
	/// </remarks>
	public class AWSCloudProviderException : ApplicationException
	{
		/// <value>
		/// Http status code returned by the call to the S3 API.
		/// </value>
		public System.Net.HttpStatusCode StatusCode { get; }

		/// <value>
		/// Response from the S3 API.
		/// </value>
		public AmazonWebServiceResponse Response { get; }

		/// <summary>
		/// Create an instance
		/// </summary>
		/// <param name="response">AmazonWebServiceResponse object returned by the S3 API.</param>
		/// <remarks>
		/// Status code is set to the value returned in the response parameter.  The AmazonWebServiceResponse may be parsed to provide 
		/// futher information.
		/// </remarks>
		public AWSCloudProviderException (AmazonWebServiceResponse response) : base ($"Error {response.HttpStatusCode}.")
		{
			this.StatusCode = response.HttpStatusCode;
			this.Response = response; 
		}

		/// <summary>
		/// Create an instance
		/// </summary>
		/// <param name="StatusCode">Http status code returned by the call to the S3 API.</param>
		/// <param name="Message">Error message (display text)</param>
		public AWSCloudProviderException(System.Net.HttpStatusCode StatusCode, string Message) : base(Message)
		{
			this.StatusCode = StatusCode;
		}
	}
}
