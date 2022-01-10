using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to read extended data for claim types from the configuration file.  Extended data is used when rendering controls
	/// for a UserProfileValue which represents a per-user value for the claim type.
	/// </summary>
	public class ClaimTypeOption
	{
		#nullable enable

		/// <summary>
		/// Claim type Uri.  Claims should use one of the values from the http://schemas.microsoft.com/ws/2008/06/identity/claims namespace, or
		/// from the OpenId Connect specification where available, but can use a custom uri if no pre-existing Uri exists for the required type.
		/// </summary>
		public string? Uri { get; private set; }

		/// <summary>
		/// A default friendly name for the claim, used on-screen as a label.
		/// </summary>
		public string DefaultName { get; private set; }

		/// <summary>
		/// Regular expression used to validate input.
		/// </summary>
		public string Pattern { get; private set; }

		/// <summary>
		/// An type used for a HTML element used to collect input for this claim type.
		/// </summary>
		public string InputType { get; private set; }

		/// <summary>
		/// Specifies whether new sites have this claim added to the site's User Profile Properties automatically.
		/// </summary>
		public Boolean IsSiteDefault { get; private set; }

		#nullable disable

		/// <summary>
		/// Constructor used by configuration file deserialization.
		/// </summary> 
		public ClaimTypeOption() { }

		/// <summary>
		/// Constructor used to create a default claim type for a Uri which is not found in the configuration file. 
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="defaultName"></param>
		/// <param name="inputType"></param>
		/// <param name="pattern"></param>
		/// <param name="isSiteDefault"></param>
		internal ClaimTypeOption(string uri, string defaultName, string inputType, string pattern, Boolean isSiteDefault) 
		{
			this.Uri = uri;
			this.DefaultName = defaultName;
			this.InputType = inputType;
			this.Pattern = pattern;
			this.IsSiteDefault = isSiteDefault;
		}
	}
}
