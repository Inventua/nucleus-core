﻿using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.Models
{
	public class ClientAppToken
	{
		public Guid Id { get; set; }
				
		public ClientApp ClientApp { get; set; }

		public string Type { get; set; }

		public string Scope { get; set; }
		public string State { get; set; }

		public string RedirectUri { get; set; }

		public string Code { get; set; }

		public Guid? UserId { get; set; }

		public string AccessToken { get; set; }
		
		public string RefreshToken { get; set; }

		public DateTime ExpiryDate { get; set; }
		
		public DateTime DateAdded { get; set; }

	}
}
