using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Collection type used to retrieve ClaimType settings from the configuration files.
	/// </summary>
	public class ClaimTypeOptions
	{
		#nullable enable
		/// <summary>
		/// Configuration file section path for claim type options.
		/// </summary>
		public const string Section = "Nucleus:ClaimTypeOptions";

		/// <summary>
		/// Claim type options, read from configuration.
		/// </summary>
		public List<ClaimTypeOption> Types { get; private set; } = new();

		/// <summary>
		/// Return the claim specified by TypeUri, or a default instance if it is not present.
		/// </summary>
		/// <param name="typeUri"></param>
		/// <returns>The requested claim type.</returns>
		/// <remarks>
		/// typeUri is not case-sensitive.
		/// </remarks>
		public ClaimTypeOption Find(string? typeUri)
		{
			if (typeUri != null)
			{
				foreach (ClaimTypeOption option in Types)
				{
					if (option.Uri?.Equals(typeUri, StringComparison.OrdinalIgnoreCase) == true)
					{
						return option;
					}
				}
			}

			// Uri is not in the config file, return a default claim type option
			return new ClaimTypeOption(typeUri, "text", "", "", false);
		}
	}
}
