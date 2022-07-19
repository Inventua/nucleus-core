using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Authentication
{
	/// <summary>
	/// Constants for the Nucleus authentication implementation.
	/// </summary>
	public class Constants
	{
		/// <summary>
		/// Authentication scheme name
		/// </summary>
		public const string DEFAULT_AUTH_SCHEME = "urn:nucleus/authentication/default-scheme-name";

		/// <summary>
		/// Nucleus session ID claim URI.
		/// </summary>
		/// <remarks>
		/// This claim type is used in the cookie which is used to represent a logged-on user.
		/// </remarks>
		public const string SESSION_ID_CLAIMTYPE = "urn:nucleus/identity/claims/sessionid";

		/// <summary>
		/// Nucleus System Administrator claim Uri.
		/// </summary>
		public const string SYSADMIN_CLAIMTYPE = "urn:nucleus/identity/claims/issystemadministrator";

		/// <summary>
		/// Nucleus unverified user claim Uri.
		/// </summary>
		public const string NOT_VERIFIED_CLAIMTYPE = "urn:nucleus/identity/claims/not-verified";

		/// <summary>
		/// Nucleus not-approved user claim.
		/// </summary>
		public const string NOT_APPROVED_CLAIMTYPE = "urn:nucleus/identity/claims/not-approved";

		/// <summary>
		/// Nucleus user claim Uri used to specify that the user was authenticated using a session cookie.
		/// </summary>
		/// <remarks>
		/// In practise, this claim is used to determine whether a Nucleus user id claim will be present.  When
		/// a request is received using an Api Key, this claim is not present, and no user id is available.
		/// </remarks>
		public const string AUTHENTICATED_BY_COOKIE = "cookie";



	}
}
