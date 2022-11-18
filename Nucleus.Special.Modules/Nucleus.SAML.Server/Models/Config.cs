using ITfoxtec.Identity.Saml2;
using System;
using System.Security.Cryptography.X509Certificates;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.SAML.Server;

namespace Nucleus.SAML.Server.Models
{
	public class Config
	{
		public Config(Microsoft.AspNetCore.Http.HttpRequest request, Context context, Models.ClientApp clientApp)
		{
			this.Issuer = request.Issuer();

			this.AllowedIssuer = clientApp.AllowedIssuer;
			
			if (!String.IsNullOrEmpty(clientApp.SigningCertificatePassword))
			{
				try
				{
					this.SigningCertificate = new X509Certificate2(clientApp.SigningCertificateFile, clientApp.SigningCertificatePassword);
				}
				catch(System.Security.Cryptography.CryptographicException e)
				{
					throw new InvalidOperationException(e.Message + " (signing certificate)" ,e);
				}				
			}
			else
			{
				this.SigningCertificate = new X509Certificate2(clientApp.SigningCertificateFile);
			}
			
			this.SignatureValidationCertificate = new X509Certificate2(clientApp.SignatureValidationCertificateFile);
			
			this.ArtifactResolutionService = new()
			{
				Index = 0,
				Location = context.Site.AbsoluteUri(Routes.ARTIFACT, request.IsHttps)
			};
		}

		public Saml2Configuration AsSaml2Configuration()
		{
			Saml2Configuration result = new()
			{
				Issuer = this.Issuer,
				AllowedIssuer	=	this.AllowedIssuer,
				SigningCertificate = this.SigningCertificate,
				ArtifactResolutionService = this.ArtifactResolutionService,
				AuthnResponseSignType= Saml2AuthnResponseSignTypes.SignResponse,
				CertificateValidationMode= System.ServiceModel.Security.X509CertificateValidationMode.None,
				RevocationMode= X509RevocationMode.NoCheck
			};

			result.SignatureValidationCertificates.Add(this.SignatureValidationCertificate);

			return result;
		}

		public string Issuer { get; set; }

		public string AllowedIssuer { get; set; }

		public X509Certificate2 SigningCertificate { get; set; }
		public X509Certificate2 SignatureValidationCertificate { get; set; }

		public Saml2IndexedEndpoint ArtifactResolutionService { get; set; }
	}


}
