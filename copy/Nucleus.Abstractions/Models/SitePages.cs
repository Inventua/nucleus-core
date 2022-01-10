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
		public Guid? LoginPageId { get; set; }
		public Guid? UserRegisterPageId { get; set; }
		public Guid? UserProfilePageId { get; set; }
		public Guid? TermsPageId { get; set; }
		public Guid? PrivacyPageId { get; set; }
		public Guid? ErrorPageId { get; set; }
		public Guid? NotFoundPageId { get; set; }


	}
}
