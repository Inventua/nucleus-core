using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Client
{
	public class SimpleCertificateValidator : System.IdentityModel.Selectors.X509CertificateValidator
	{
		public override void Validate(X509Certificate2 certificate)
		{
			// no implementation, this class validates all certificates as valid
		}
	}
}
