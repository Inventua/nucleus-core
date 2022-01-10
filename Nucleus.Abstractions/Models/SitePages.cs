using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents special pages within a site.
	/// </summary>
	public class SitePages
	{
		/// <summary>
		/// If set, represents the Id of the login page.
		/// </summary>
		public Guid? LoginPageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the user registration page.
		/// </summary>
		public Guid? UserRegisterPageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the user profile page.
		/// </summary>
		public Guid? UserProfilePageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the user change password page.
		/// </summary>
		public Guid? UserChangePasswordPageId { get; set; }			
		
		/// <summary>
		/// If set, represents the Id of the terms page.
		/// </summary>
		public Guid? TermsPageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the privacy page.
		/// </summary>
		public Guid? PrivacyPageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the error page.
		/// </summary>
		public Guid? ErrorPageId { get; set; }

		/// <summary>
		/// If set, represents the Id of the 404-not found page.
		/// </summary>
		public Guid? NotFoundPageId { get; set; }


	}
}
