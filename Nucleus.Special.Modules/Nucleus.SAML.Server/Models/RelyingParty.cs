using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Nucleus.SAML.Server.Models
{
	public class RelyingParty
	{
		public string Metadata { get; set; }

		public string Issuer { get; set; }

		public Uri AssertionConsumerServiceUri { get; set; }

		public string ProtocolBinding { get; set; } = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";

		public Uri SingleLogoutDestination { get; set; }

		public X509Certificate2 SignatureValidationCertificate { get; set; }

		public X509Certificate2 EncryptionCertificate { get; set; }
	}
}

