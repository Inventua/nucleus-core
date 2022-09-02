using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="Nucleus.Abstractions.Models.Configuration.AllowedFileType" />.
	/// </summary>
	public static class AllowedFileTypeExtensions
	{
		/// <summary>
		/// Return whether the bytes in the stream match one or more file signatures.
		/// </summary>
		/// <param name="allowedFileType"></param>
		/// <param name="stream"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function does not close the stream, so the caller must close the stream.
		/// </remarks>
		public static Boolean IsValid(this Nucleus.Abstractions.Models.Configuration.AllowedFileType allowedFileType, System.IO.Stream stream)
		{
			byte[] sample = GetSample(stream);
			foreach (string signature in allowedFileType.Signatures)
			{
				if (IsValid(signature, sample))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Retrieve a 64 byte sample from the start of a stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static byte[] GetSample(System.IO.Stream stream)
		{
			byte[] sample = new byte[63];

			stream.Read(sample, 0, sample.Length);

			return sample;
		}
				
		private static Boolean IsValid(string signature, byte[] sample)
		{
			List<string> signatureBytes = new();

			for (int count = 0; count < (int)Math.Floor(signature.Length / (double)2); count += 2)
			{
				signatureBytes.Add(signature.Substring(count, 2));
			}

			if (sample.Length < signatureBytes.Count) return false;

			for (int count = 0; count < signatureBytes.Count; count++)
			{
				if (signatureBytes[count] != "??")
				{
					if (!signatureBytes[count].Equals(sample[count].ToString("X"), StringComparison.OrdinalIgnoreCase)) return false;
				}
			}

			return true;
		}
	}
}
