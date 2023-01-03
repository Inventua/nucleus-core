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
	public class StoreOptions
	{
		#nullable enable
		/// <summary>
		/// Configuration file section path for claim type options.
		/// </summary>
		public const string Section = "Nucleus:StoreOptions";

		/// <summary>
		/// Claim type options, read from configuration.
		/// </summary>
		public List<Store> Stores { get; private set; } = new();

	}
}
