using ITfoxtec.Identity.Saml2;
using System;
using System.Security.Cryptography.X509Certificates;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.SAML.Server;
using ITfoxtec.Identity.Saml2.Util;

namespace Nucleus.SAML.Server.Models
{
	public class Config
	{
		public Config(Microsoft.AspNetCore.Http.HttpRequest request, Context context, Models.ClientApp clientApp)
		{
			this.Issuer = request.Issuer();

			this.AllowedIssuer = clientApp.AllowedIssuer;

			if (clientApp.SigningCertificateSource == ClientApp.CertificateSource.Store )
			{
				this.SigningCertificate = LoadCertificateFromStore(clientApp.SigningCertificateThumbprint, "IdP signing");
				//if (String.IsNullOrEmpty(clientApp.SigningCertificateThumbprint))
				//{
				//	throw new InvalidOperationException("Signing certificate thumbprint is not selected.");
				//}
			}
			else if (clientApp.SigningCertificateSource == ClientApp.CertificateSource.File)
			{
				this.SigningCertificate = LoadCertificateFromFile(clientApp.SigningCertificateFile,clientApp.SigningCertificatePassword,"IdP signing");

				//if (String.IsNullOrEmpty(clientApp.SigningCertificateFile))
				//{
				//	throw new InvalidOperationException("SP Signing certificate file is not selected.");
				//}

				//if (!String.IsNullOrEmpty(clientApp.SigningCertificatePassword))
				//{
				//	try
				//	{
				//		this.SigningCertificate = new X509Certificate2(clientApp.SigningCertificateFile, clientApp.SigningCertificatePassword);
				//	}
				//	catch (System.Security.Cryptography.CryptographicException e)
				//	{
				//		throw new InvalidOperationException(e.Message + " (signing certificate)", e);
				//	}
				//}
				//else
				//{
				//	this.SigningCertificate = new X509Certificate2(clientApp.SigningCertificateFile);
				//}
			}

			if (clientApp.SignatureValidationCertificateSource == ClientApp.CertificateSource.Store)
			{
				this.SignatureValidationCertificate = LoadCertificateFromStore(clientApp.SignatureValidationCertificateThumbprint, "SP validation");
				//if (String.IsNullOrEmpty(clientApp.SignatureValidationCertificateThumbprint))
				//{
				//	throw new InvalidOperationException("SP Signing certificate thumbprint is not selected.");
				//}
				//try
				//{
				//	this.SignatureValidationCertificate=CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, clientApp.SignatureValidationCertificateThumbprint.Replace(" ", ""));
				//}
				//catch (System.Security.Cryptography.CryptographicException e)
				//{
				//	throw new InvalidOperationException(e.Message + " (signature validation certificate/local machine-cert-store)", e);
				//}
			}
			else if (clientApp.SignatureValidationCertificateSource == ClientApp.CertificateSource.File)
			{
				this.SignatureValidationCertificate = LoadCertificateFromFile(clientApp.SignatureValidationCertificateFile, "", "SP validation");
				//if (String.IsNullOrEmpty(clientApp.SignatureValidationCertificateFile))
				//{
				//	throw new InvalidOperationException("SP Signing certificate file is not selected.");
				//}
				//this.SignatureValidationCertificate = new X509Certificate2(clientApp.SignatureValidationCertificateFile);
			}

			this.ArtifactResolutionService = new()
			{
				Index = 0,
				Location = context.Site.AbsoluteUri(Routes.ARTIFACT, request.IsHttps)
			};
		}

		private X509Certificate2 LoadCertificateFromFile(string filepath, string password, string idpOrSpMessage)
		{
			if (String.IsNullOrEmpty(filepath))
			{
				throw new InvalidOperationException($"{idpOrSpMessage} Signing certificate file is not selected.");
			}

			if (!String.IsNullOrEmpty(password))
			{
				try
				{
					return new X509Certificate2(filepath, password);
				}
				catch (System.Security.Cryptography.CryptographicException e)
				{
					throw new InvalidOperationException(e.Message + " (signing certificate)", e);
				}
			}
			else
			{
				return new X509Certificate2(filepath);
			}
		}

		private X509Certificate2 LoadCertificateFromStore(string thumbprint, string idpOrSpMessage)
		{
			if (String.IsNullOrEmpty(thumbprint))
			{
				throw new InvalidOperationException($"{idpOrSpMessage} certificate thumbprint is not selected.");
			}
			try
			{
				return CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, thumbprint.Replace(" ", ""));
			}
			catch (System.Security.Cryptography.CryptographicException e)
			{
				throw new InvalidOperationException(e.Message + $" ({idpOrSpMessage} certificate/Local Machine/My)", e);
			}
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
