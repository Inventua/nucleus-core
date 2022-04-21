using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.Models
{
	public class ClientApp : ModelBase 
	{
		public Guid Id { get; set; }

		public ApiKey ApiKey { get; set; }

		public string Title { get; set; }

		public string RedirectUri { get; set; }

		public int TokenExpiryMinutes { get; set; } = 5;

		public Page LoginPage { get; set; }
	}
}
