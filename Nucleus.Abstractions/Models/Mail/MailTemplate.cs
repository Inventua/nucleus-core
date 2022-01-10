using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models.Mail
{
	/// <summary>
	/// Represents an e-mail template.
	/// </summary>
	public class MailTemplate : ModelBase
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }
		
		/// <summary>
		/// Name of the email template
		/// </summary>
		[Required]
		public string Name { get; set; }
		
		/// <summary>
		/// Subject of the email.  
		/// </summary>
		/// <remarks>
		/// This value can contain tokens, in the form {key.property}.  Tokens are replaced during email generation with values passed to 
		/// the MailClient using the MailArgs class.
		/// </remarks>
		public string Subject { get; set; }

		/// <summary>
		/// Emai bodyl.  
		/// </summary>
		/// <remarks>
		/// This value can contain tokens, in the form {key.property}.  Tokens are replaced during email generation with values passed to 
		/// the MailClient using the MailArgs class.
		/// </remarks>
		public string Body { get; set; }
	}
}
