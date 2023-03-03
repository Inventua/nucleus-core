using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Mail.Template;

/// <summary>
/// Class used to send user information to the mail client - contains data used by a mail template.
/// </summary>
public class UserMailTemplateData
{
	/// <summary>
	/// Site that the user belongs to.
	/// </summary>
	public Site Site { get; set; }

	/// <summary>
	/// User information.
	/// </summary>
	public User User { get; set; }

	/// <summary>
	/// Login page for the user's site. Can be null.
	/// </summary>
	public Page LoginPage { get;set; }

	/// <summary>
	/// Privacy page for the user's site. Can be null.
	/// </summary>
	public Page PrivacyPage { get; set; }

	/// <summary>
	/// Terms page for the user's site. Can be null.
	/// </summary>
	public Page TermsPage { get; set; }

}
