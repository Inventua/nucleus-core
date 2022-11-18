using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server.Models
{
	public class ClientAppToken
	{
		public Guid Id { get; set; }
				
		public ClientApp ClientApp { get; set; }

		public string ProtocolBinding { get; set; }

		public string RequestId { get; set; }

		public string RelayState { get; set; }

		public string AssertionConsumerServiceUrl { get; set; }

		public string Code { get; set; }

		public Guid? UserId { get; set; }

		public DateTime ExpiryDate { get; set; }
		
		public DateTime DateAdded { get; set; }

	}
}
