using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server.Models
{
	public class ClientApp : ModelBase 
	{
		public Guid Id { get; set; }

		public string AllowedIssuer { get; set; }

		public string Title { get; set; }

		public int TokenExpiryMinutes { get; set; } = 5;

		public Page LoginPage { get; set; }

		//public RelyingParty RelyingParty { get; set; }
		public string ServiceProviderMetadataUrl { get; set; }

		public string SigningCertificateFile { get; set; }
		public string SigningCertificatePassword { get; set; }

		public string SignatureValidationCertificateFile { get; set; }
	}
}
