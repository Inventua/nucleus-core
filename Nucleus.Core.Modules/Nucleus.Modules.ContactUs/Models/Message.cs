using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.ContactUs.Models
{
	public class Message
	{
		public string FirstName { get; set; }	

		public string LastName { get; set; }	

		[Required(ErrorMessage = "Enter your email address.")]
		public string Email { get; set; }	

		public string Company { get; set; }

		public string PhoneNumber { get; set; }

		public ListItem Category { get; set; }

		public string Subject { get; set; }

		[Required(ErrorMessage = "Enter the message body.")]
		public string Body { get; set; }
	}

}
