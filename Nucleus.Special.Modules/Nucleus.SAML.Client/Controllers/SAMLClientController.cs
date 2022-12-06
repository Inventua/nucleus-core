using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.MvcCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.Util;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Extensions.Authorization;
using Microsoft.AspNetCore.Authentication;
using Nucleus.Abstractions.Models.Configuration;
using System.ServiceModel.Channels;
using Nucleus.ViewFeatures;
using System.Net;

// https://www.itfoxtec.com/IdentitySaml2
// https://www.itfoxtec.com/License/3clauseBSD.htm
// https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/blob/master/test/TestWebAppCore/Controllers/AuthController.cs

// https://medium.com/the-new-control-plane/i-need-a-saml-idp-to-test-now-477761595b60

// SAML Response Validator:
// https://www.samltool.com/validate_response.php


// todo: cache IdP meta-data?

namespace Nucleus.SAML.Client.Controllers
{
	[Extension("SAMLClient")]
	public class SAMLClientController : Controller
	{
    private IWebHostEnvironment WebHostEnvironment { get; }
    private FolderOptions FolderOptions { get; }
    private IHttpClientFactory HttpClientFactory { get; }
		private IUrlHelperFactory UrlHelperFactory { get; }
		private Context Context { get; }

		private IUserManager UserManager { get; }

		private IRoleManager RoleManager { get; }
		private ISessionManager SessionManager { get; }

		private IOptions<Models.Configuration.SAMLProviders> Options { get; }
		private ILogger<SAMLClientController> Logger { get; }

		const string relayStateReturnUrl = "ReturnUrl";

		public SAMLClientController(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory, IOptions<FolderOptions> folderOptions, IUrlHelperFactory urlHelperFactory, Context Context, ISessionManager sessionManager, IUserManager userManager, IRoleManager roleManager, IOptions<Models.Configuration.SAMLProviders> options, ILogger<SAMLClientController> logger)
		{
      this.WebHostEnvironment = webHostEnvironment;
			this.HttpClientFactory = httpClientFactory;
      this.FolderOptions = folderOptions.Value; 
      this.UrlHelperFactory = urlHelperFactory;
			this.Context = Context;
			this.SessionManager = sessionManager;
			this.UserManager = userManager;
			this.RoleManager = roleManager;
			this.Options = options;
			this.Logger = logger;
		}

		/// <summary>
		/// Display a list or drop-down showing the configured SAML identity providers that the user can connect to.  If there is 
		/// only one provider, and the "AutoLogin" option is set to true, automatically redirect to the provider.
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Index(string returnUrl)
		{
			ViewModels.Viewer viewModel = BuildViewModel(returnUrl);

			// If there is only one provider, and the user is not logged in, and the "AutoLogin" option is set to true,
			// automatically redirect to the (one available) provider.
			if (viewModel.AutoLogin)
			{
				if (!User.Identity.IsAuthenticated && this.Options.Value.Count == 1)
				{
					Models.Configuration.SAMLProvider providerOption = this.Options.Value.FirstOrDefault();

					if (providerOption != null)
					{
						string url = BuildRedirectUrl(returnUrl);
						Logger?.LogTrace("SAML Provider Selector: AutoLogin is enabled, automatically redirecting to '{url}'.", url);
						// redirect to SAML provider 
						return Challenge(new AuthenticationProperties() { RedirectUri = url }, providerOption.Key);
					}
				}
			}

			return View("Viewer", viewModel);
		}


		/// <summary>
		/// Use the specified <paramref name="providerKey"/> to retrieve provider settings and start a SAML login.
		/// </summary>
		/// <param name="providerKey">The "Key" value for a SAML IdP which is configured in configuration files [Nucleus:SAMLProviders]</param>
		/// <param name="returnUrl">Relative url to redirect to after the SAML sign-on is complete.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		[HttpGet]
		[Route($"{Routes.AUTHENTICATE}/{{providerKey}}")]
		public async Task<IActionResult> Authenticate(string providerKey, string returnUrl)
		{
			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);

			if (providerOption != null)
			{
				Logger?.LogTrace("Starting the remote login process.");

				switch (providerOption.GetResponseProtocolBinding())
				{
					case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpRedirect:
					case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpArtifact:
					case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpPost:
						break;

					default:
						throw new InvalidOperationException($"Unsupported response protocol binding: '{providerOption.ResponseProtocolBinding}'");
				}

				Saml2Configuration config = await BuildConfiguration(providerOption);
				IUrlHelper urlHelper = this.UrlHelperFactory.GetUrlHelper(this.ControllerContext);
				Saml2AuthnRequest request = new(config)
				{
					AssertionConsumerServiceUrl = Nucleus.ViewFeatures.UrlHelperExtensions.GetAbsoluteUri(urlHelper, $"{Routes.ASSERTION_CONSUMER}/{providerKey}"),
					ProtocolBinding = new Uri(String.IsNullOrEmpty(providerOption.ResponseProtocolBinding) ? "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST" : providerOption.ResponseProtocolBinding),
					Subject = new Subject { NameID = new NameID { ID = "" } },
					NameIdPolicy = new NameIdPolicy { AllowCreate = false, Format = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent" }
				};

				switch (providerOption.GetRequestProtocolBinding())
				{
					case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpPost:
					{
						Saml2PostBinding binding = new();
						binding.SetRelayStateQuery(new Dictionary<string, string> { { relayStateReturnUrl, returnUrl ?? Url.Content("~/") } });
						return binding.Bind(request).ToActionResult();
					}

					case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpRedirect:
					{
						Saml2RedirectBinding binding = new();
						binding.SetRelayStateQuery(new Dictionary<string, string> { { relayStateReturnUrl, returnUrl ?? Url.Content("~/") } });
						return binding.Bind(request).ToActionResult();
					}

					default:
						throw new InvalidOperationException($"Unsupported request protocol binding: '{providerOption.RequestProtocolBinding}'");
				}
			}
			else
			{
				Logger?.LogTrace("SAML provider {providerKey} not found.  Check your configuration files Nucleus:SAMLProviders section for a provider with a matching name, or a matching type and no name.", providerKey);
				return BadRequest();
			}
		}

		
		/// <summary>
		/// Implementation of SAML SP assertion consumer service.  This action receives a SAML artifact as a query string
		/// value, which it uses to call the Identity Provider artifact resolution service.
		/// </summary>
		/// <param name="providerKey"></param>
		/// <param name="SAMLArt"></param>
		/// <returns></returns>
		[HttpGet]
		[Route($"{Routes.ASSERTION_CONSUMER}/{{providerKey}}")]
		public async Task<IActionResult> Callback(string providerKey, string SAMLArt)
		{
			try
			{
				return await ParseAuthnResponse(providerKey);
			}
			catch (ITfoxtec.Identity.Saml2.Cryptography.InvalidSignatureException e)
			{
				Logger.LogError(e, "Exception thrown while validating SAML2 Authn Response.");
				throw e;
			}
		}

		/// <summary>
		/// Implementation of SAML SP Assertion consumer service.  This action receives the AuthnResponse as a form  
		/// value, or in the POST body. 
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		[HttpPost]
		[Route($"{Routes.ASSERTION_CONSUMER}/{{providerKey}}")]
		public async Task<IActionResult> Callback(string providerKey)
		{
			try
			{
				return await ParseAuthnResponse(providerKey);
			}
			catch (ITfoxtec.Identity.Saml2.Cryptography.InvalidSignatureException e)
			{
				Logger.LogError(e, "Exception thrown validating SAML2 Authn Response.");
				throw e;
			}
		}

		/// <summary>
		/// Prepare and output SAML Service Provider (SP) metadata for the specified <paramref name="providerKey"/>
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		[HttpGet]
		[Route($"{Routes.METADATA}/{{providerKey}}")]
		public async Task<IActionResult> Metadata(string providerKey)
		{
			Uri defaultSite = new ($"{Request.Scheme}://{Request.Host.ToUriComponent()}/{(!String.IsNullOrEmpty(Request.PathBase) ? Request.PathBase + "/" : "")}");

			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);

			if (providerOption == null)
			{
				return NotFound($"SAML Identity Provider '{providerKey}' not found.");
			}

			Saml2Configuration config = await BuildConfiguration(providerOption);

			EntityDescriptor entityDescriptor = new(config, signMetadata: providerOption.SignMetadata && config.SigningCertificate != null)
			{
				ValidUntil = 30,  // days
				SPSsoDescriptor = new()
				{
					// we always ask for the assertion to be signed
					WantAssertionsSigned = true,

					AuthnRequestsSigned = config.SignAuthnRequest,

					NameIDFormats = new Uri[] { NameIdentifierFormats.Persistent },

					SingleLogoutServices = new SingleLogoutService[]
					{
						new() { Binding = ProtocolBindings.HttpPost, Location = new Uri(defaultSite, $"{Routes.SINGLE_LOGOUT}/{providerKey}")	}
					},									

					AssertionConsumerServices = new AssertionConsumerService[]
					{
						new() { Binding = ProtocolBindings.HttpPost, Location = new Uri(defaultSite, $"{Routes.ASSERTION_CONSUMER}/{providerKey}") }
					},

					AttributeConsumingServices = new AttributeConsumingService[]
					{
						new() 
						{							  
							ServiceName = new ServiceName($"{this.Context.Site.Name} [{providerKey}] SAML Service Provider" , "en"),
							RequestedAttributes = CreateRequestedAttributes(providerOption)
						}
					}
				}
				// Contact persons element is optional(SAML spec 2.3.2.2).  
				//ContactPersons = new ContactPerson[] {
				//	new ContactPerson(ContactTypes.Administrative)
				//	{
				//		Company = "Some Company", GivenName = "Some Given Name", SurName = "Some Surname",	EmailAddress = "some@some-domain.com", TelephoneNumber = "11111111"
				//	}
				//}
			};

			if (config.SigningCertificate != null)
			{
				entityDescriptor.SPSsoDescriptor.SigningCertificates = new X509Certificate2[] { config.SigningCertificate };
			}

			// Encryption not implemented
			// EncryptionCertificates = new X509Certificate2[] { config.DecryptionCertificate },

			return new Saml2Metadata(entityDescriptor)
				.CreateMetadata()
				.ToActionResult();
		}

		/// <summary>
		/// Send a request to the IdP asking to be logged out.
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		[HttpPost]
		[Route($"{Routes.LOGOUT}/{{providerKey}}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout(string providerKey)
		{
			if (!User.Identity.IsAuthenticated)
			{
				return Redirect(Url.Content("~/"));
			}

			Saml2PostBinding binding = new();
			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);
			Uri defaultSite = new($"{Request.Scheme}://{Request.Host.ToUriComponent()}/{(!String.IsNullOrEmpty(Request.PathBase) ? Request.PathBase + "/" : "")}");

			Saml2LogoutRequest saml2LogoutRequest = new(await BuildConfiguration(providerOption), User);
			saml2LogoutRequest.Config.SingleLogoutDestination = new System.Uri(defaultSite, $"{Routes.SINGLE_LOGOUT}/{providerKey}");
			saml2LogoutRequest = await saml2LogoutRequest.DeleteSession(HttpContext);
			
			await this.SessionManager.SignOut(HttpContext);

			return binding.Bind(saml2LogoutRequest).ToActionResult();
		}

		/// <summary>
		/// Log out locally.
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		[Route($"{Routes.LOGGED_OUT}/{{providerKey}}")]
		public async Task<IActionResult> LoggedOut(string providerKey)
		{
			var binding = new Saml2PostBinding();
			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);

			binding.Unbind(Request.ToGenericHttpRequest(), new Saml2LogoutResponse(await BuildConfiguration(providerOption)));

			await this.SessionManager.SignOut(HttpContext);

			return Redirect(Url.Content("~/"));
		}

		/// <summary>
		/// Process IdP-initated local logout
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		[Route($"{Routes.SINGLE_LOGOUT}/{{providerKey}}")]
		public async Task<IActionResult> SingleLogout(string providerKey)
		{
			Saml2StatusCodes status;
			Saml2PostBinding requestBinding = new();
			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);

			Saml2LogoutRequest logoutRequest = new (await BuildConfiguration(providerOption), User);
			
			try
			{
				requestBinding.Unbind(Request.ToGenericHttpRequest(), logoutRequest);
				status = Saml2StatusCodes.Success;
				await logoutRequest.DeleteSession(HttpContext);
				
				await this.SessionManager.SignOut(HttpContext);
			}
			catch (Exception exc)
			{
				// log exception
				Debug.WriteLine("SingleLogout error: " + exc.ToString());
				status = Saml2StatusCodes.RequestDenied;
			}

			Saml2PostBinding responsebinding = new();
			responsebinding.RelayState = requestBinding.RelayState;
			Saml2LogoutResponse saml2LogoutResponse = new (await BuildConfiguration(providerOption))
			{
				InResponseToAsString = logoutRequest.IdAsString,
				Status = status,
			};
			return responsebinding.Bind(saml2LogoutResponse).ToActionResult();
		}


		/// <summary>
		/// Parse an AuthResponse, or use the value of a SAMLartifact to call the IdP artifact resolution service (which
		/// returns an AuthnResponse) and then parse it.  If successful, use the information in the AuthnResponse to log in
		/// locally.
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		/// <remarks>
		/// The IdP provider ResponseProtocolBinding setting (specifed by <paramref name="providerKey"/>) controls whether
		/// we expect the incoming data to be a SAMLartifact in the query string, a AuthnResponse in form data, or an 
		/// AuthnResponse in the request body.
		/// 
		/// The Saml2RedirectBinding, Saml2ArtifactBinding and Saml2PostBinding take care of validating the HTTP method and
		/// retrieving/parsing the query string, form data or request body, so we don't have to check any of that here.
		/// </remarks>
		private async Task<IActionResult> ParseAuthnResponse(string providerKey)
		{
			Models.Configuration.SAMLProvider providerOption = GetProviderSettings(providerKey);
			Saml2Configuration config = await BuildConfiguration(providerOption);
			Saml2AuthnResponse saml2AuthnResponse = new(config);
			Dictionary<string, string> relayStateQuery;

			switch (providerOption.GetResponseProtocolBinding())
			{
				case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpArtifact:
				{
					// get the SAML artifact from form data, then call the IdP's artifact resolution service to get an AuthnResponse
					// and parse it.
					config.Issuer = providerOption.Issuer;
					Saml2ArtifactBinding binding = new();
					config.ArtifactResolutionService = new() { Index = 0, Location = new(providerOption.ArtifactResolutionServiceUrl) };
					Saml2ArtifactResolve resolver = new(config);

					binding.Unbind(Request.ToGenericHttpRequest(), resolver);

					Saml2SoapEnvelope soapEnvelope = new();
					await soapEnvelope.ResolveAsync(this.HttpClientFactory, resolver, saml2AuthnResponse);

					relayStateQuery = binding.GetRelayStateQuery();
					break;
				}

				case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpPost:
				{
					// The AuthnResponse is in the rrequest body, parse it
					Saml2PostBinding binding = new();
					binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					relayStateQuery = binding.GetRelayStateQuery();
					break;
				}

				default:
					throw new AuthenticationException($"SAML: Unhandled response protocol binding {providerOption.ResponseProtocolBinding}");
			}

			if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
			{
				throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
			}

			// If a user is already logged in locally, log them out so that we can log on the new user
			if (HttpContext.User.Identity.IsAuthenticated)
			{
				await this.SessionManager.SignOut(HttpContext);
				HttpContext.User = null;
			}

			// Create a ClaimsPrincipal and translate SAML claims to local (Nucleus) claims, using mapping from config SAMLProviders[@Key=providerKey]:MapClaims
			ClaimsPrincipal principal = await saml2AuthnResponse.CreateSession
			(
				HttpContext,
				claimsTransform: claimsPrincipal => ClaimsTransformExtensions.Transform
				(
					claimsPrincipal,
					providerOption.MapClaims,
					this.Logger
				)
			);

			if (principal.IsSystemAdministrator() || principal.IsSiteAdmin(this.Context.Site))
			{
				// admins can't use remote authentication
				Logger?.LogWarning("Access denied for user '{name}' because admins can't use remote authentication.", principal.Identity.Name);
				throw new InvalidOperationException("Access denied for user, because admins can't use remote authentication.");
			}

			// Log in locally
			return await HandleSignInAsync(this.Context.Site, principal, relayStateQuery);
		}

		/// <summary>
		/// Request user values (claims) which are commonly used by Nucleus
		/// </summary>
		/// <returns></returns>
		private IEnumerable<RequestedAttribute> CreateRequestedAttributes(Models.Configuration.SAMLProvider provider)
		{
			// return the "SAML-side" names of the claims which we can consume.  The <requestedAttributes> element is a SAML
			// extension, and IdPs can choose to ignore it.  
			return provider.MapClaims
				.DistinctBy(mapClaim=>mapClaim.SAMLKey)
				.Select(map => new RequestedAttribute(map.SAMLKey, false, "urn:oasis:names:tc:SAML:2.0:attrname-format:uri")).ToList();

			//yield return new RequestedAttribute("urn:oid:2.5.4.41");                    // name
			//yield return new RequestedAttribute("urn:oid:2.5.4.42");                    // givenName
			//yield return new RequestedAttribute("urn:oid:2.5.4.4");                     // surname
			//yield return new RequestedAttribute("urn:oid:2.5.4.3");                     // common name
			//yield return new RequestedAttribute("urn:oid:0.9.2342.19200300.100.1.3");   // email
		}

		/// <summary>
		/// Return provider config settings specified by <paramref name="providerKey"/>, or null if there is no matching provider.
		/// </summary>
		/// <param name="providerKey"></param>
		/// <returns></returns>
		private Models.Configuration.SAMLProvider GetProviderSettings(string providerKey)
		{
			if (!String.IsNullOrEmpty(providerKey))
			{
				Logger?.LogTrace("SAML provider {providerKey} requested.", providerKey);

				// Find provider configuration matching the supplied providerKey.  
				return this.Options.Value
					.Where(option => (option.SafeProviderKey().Equals(providerKey, StringComparison.OrdinalIgnoreCase)))
					.FirstOrDefault();
			}

			return null;
		}

		/// <summary>
		/// Create and populate a Saml2Configuration object from configuration data.
		/// </summary>
		/// <param name="providerOption"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">
		/// Certificate loading/validation errors generate an InvalidOperationException.
		/// </exception>
		private async Task<Saml2Configuration> BuildConfiguration(Models.Configuration.SAMLProvider providerOption)
		{
			Saml2Configuration saml2Configuration = new();

			saml2Configuration.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
			saml2Configuration.RevocationMode = X509RevocationMode.NoCheck;

			if (!String.IsNullOrEmpty(providerOption.Issuer))
			{
				saml2Configuration.Issuer = providerOption.Issuer;
			}

			// only use these settings from config if IdPMetadataUrl is blank or not specified
			if (String.IsNullOrEmpty(providerOption.IdPMetadataUrl))
			{
				saml2Configuration.SignAuthnRequest = providerOption.SignAuthnRequest;
				saml2Configuration.AllowedIssuer = providerOption.AllowedIssuer;

				if (!String.IsNullOrEmpty(providerOption.SingleSignOnDestination))
				{
					saml2Configuration.SingleSignOnDestination = new Uri(providerOption.SingleSignOnDestination);
				}

				if (!String.IsNullOrEmpty(providerOption.SingleLogoutDestination))
				{
					saml2Configuration.SingleLogoutDestination = new Uri(providerOption.SingleLogoutDestination);
				}

				if (!String.IsNullOrEmpty(providerOption.SignatureAlgorithm))
				{
					saml2Configuration.SignatureAlgorithm = providerOption.SignatureAlgorithm;
				}
				else
				{
					saml2Configuration.SignatureAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
				}

				// load signature validation certificate from file
				if (!String.IsNullOrEmpty(providerOption.SignatureValidationCertificateFile))
				{
					try
					{
						saml2Configuration.SignatureValidationCertificates.Add(CertificateUtil.Load(this.WebHostEnvironment.MapToPhysicalFilePath(providerOption.SignatureValidationCertificateFile)));
					}
					catch (System.Security.Cryptography.CryptographicException e)
					{
						throw new InvalidOperationException(e.Message + " (signature validation certificate/file)", e);
					}
				}

				// load signature validation certificate from certifiate store
				if (!String.IsNullOrEmpty(providerOption.SignatureValidationCertificateThumbprint))
				{
					try
					{
						saml2Configuration.SignatureValidationCertificates.Add(CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, providerOption.SignatureValidationCertificateThumbprint.Replace(" ", "")));
					}
					catch (System.Security.Cryptography.CryptographicException e)
					{
						throw new InvalidOperationException(e.Message + " (signature validation certificate/local machine-cert-store)", e);
					}
				}
			}				

			// read settings from IdP meta-data, if specifed.  Settings from meta-data override any specified setitngs.
			if (!String.IsNullOrEmpty(providerOption.IdPMetadataUrl))
			{
				EntityDescriptor entityDescriptor = new();
				await entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(this.HttpClientFactory, new Uri(providerOption.IdPMetadataUrl));

				if (entityDescriptor.IdPSsoDescriptor != null)
				{
					saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
					saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
					saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;
					foreach (X509Certificate2 signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
					{
						if (!signingCertificate.IsValidLocalTime())
						{
							Logger?.LogError("SAML: The signature validation certificate provided by '{url}' has expired.", providerOption.IdPMetadataUrl);
							throw new InvalidOperationException($"The signature validation certificate provided by '{providerOption.IdPMetadataUrl}' has expired.");
						}
						saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
					}
					
					if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
					{
						saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
					}
					else
					{
						// if it is not specified by the meta-data, allow users to specify SignAuthnRequest in config file
						saml2Configuration.SignAuthnRequest = providerOption.SignAuthnRequest;
					}
				}
			}

			// AuthnRequest signature certificate is always specified by the SP (won't ever come from metadata)
			if (saml2Configuration.SignAuthnRequest)
			{
				// Load request signing certificate from a file
				if (!String.IsNullOrEmpty(providerOption.SigningCertificateFile))
				{
					try
					{
						saml2Configuration.SigningCertificate = CertificateUtil.Load(this.WebHostEnvironment.MapToPhysicalFilePath(providerOption.SigningCertificateFile), providerOption.SigningCertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
					}
					catch (System.Security.Cryptography.CryptographicException e)
					{
						throw new InvalidOperationException(e.Message + " (signing certificate/file)", e);
					}
				}

				// Load request signing certificate from the certificate store
				if (!String.IsNullOrEmpty(providerOption.SigningCertificateThumbprint))
				{
					try
					{
						saml2Configuration.SigningCertificate = CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, providerOption.SigningCertificateThumbprint.Replace(" ", ""));
					}
					catch (System.Security.Cryptography.CryptographicException e)
					{
						throw new InvalidOperationException(e.Message + " (signing certificate/local machine-cert-store)", e);
					}
				}

				if (saml2Configuration.SignAuthnRequest && saml2Configuration.SigningCertificate == null)
				{
					Logger?.LogError("SAML [{name}]: The certificate could not be loaded using the SigningCertificateFile or SigningCertificateThumbprint value.", providerOption.Key);
					throw new InvalidOperationException($"SAML [{providerOption.Key}]: The SignAuthnRequest property is set to true, but the certificate could not be loaded using the SigningCertificateFile or SigningCertificateThumbprint value.");
				}
			}
			
			// set other options which are required in config & can't come from IdP metadata
			if (!String.IsNullOrEmpty(providerOption.Issuer))
			{
				saml2Configuration.AllowedAudienceUris.Add(saml2Configuration.Issuer);
			}

			return saml2Configuration;
		}

		/// <summary>
		/// Log the user identified in <paramref name="user"/> into Nucleus.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <remarks>
		/// Depending on configuration, this function matches a user by name or email address, and (if enabled) can create a new local user 
		/// if no matching local user exists.
		/// </remarks>
		private async Task<IActionResult> HandleSignInAsync(Site site, ClaimsPrincipal user, Dictionary<string, string> properties)
		{
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}

			ViewModels.SiteClientSettings settings = new();
			settings.ReadSettings(site);

			if (String.IsNullOrEmpty(user.Identity.Name))
			{
				Logger?.LogTrace("The remote user data does not contain a user name.");
				throw new InvalidOperationException("The SAML response does not contain a user name.");
			}
			else
			{
				User loginUser = null;
				object email = user.GetUserClaim<string>(ClaimTypes.Email);

				Logger?.LogTrace("Signing in remote user '{username}'", user.Identity.Name);

				if (settings.MatchByName && !String.IsNullOrEmpty(user.Identity.Name))
				{
					Logger?.LogTrace("Checking for existing user '{username}' by name.", user.Identity.Name);
					loginUser = await this.UserManager.Get(site, user.Identity.Name);
					if (loginUser == null)
					{
						Logger?.LogTrace("Existing user '{username}' not found by name.", user.Identity.Name);
					}
				}

				if (loginUser == null && settings.MatchByEmail)
				{
					if (email != null && !String.IsNullOrEmpty(email.ToString()))
					{
						Logger?.LogTrace("Checking for existing user with email address '{email}'.", email);
						loginUser = await this.UserManager.GetByEmail(site, email.ToString());
						if (loginUser == null)
						{
							Logger?.LogTrace("Existing user with email '{email}' not found by name, or more than one user with that email address is present in the database.", email);
						}
					}
				}

				if (loginUser == null)
				{
					// user does not exist		
					if (!settings.CreateUsers)
					{
						Logger?.LogTrace("A matching user was not found, and the SAML client 'CreateUsers' setting is set to false.");
						throw new InvalidOperationException("You cannot use SAML to log in with this login name because because no user matching your login name was found on this site, and the site is not configured to automatically create new users.");
					}
					else
					{
						Logger?.LogTrace("A matching user was not found, creating a new user '{username}'.", user.Identity.Name);

						// create new user 
						loginUser = await this.UserManager.CreateNew(site);
						loginUser.UserName = user.Identity.Name;

						// fill in all of the user properties that we can find a value for
						foreach (var prop in site.UserProfileProperties)
						{
							string userPropertyValue = user.FindFirstValue(prop.TypeUri);
							if (userPropertyValue != null)
							{
								Logger?.LogTrace("Adding profile value {name}:'{value}'.", prop.TypeUri, userPropertyValue);
								UserProfileValue existing = loginUser.Profile.Where(value => value.UserProfileProperty.TypeUri == prop.TypeUri).FirstOrDefault();
								if (existing == null)
								{
									loginUser.Profile.Add(new UserProfileValue() { UserProfileProperty = prop, Value = userPropertyValue });
								}
								else
								{
									existing.Value = userPropertyValue;
								}
							}
						}

						this.UserManager.SetNewUserFlags(site, loginUser);

						if (settings.AutomaticallyVerifyNewUsers)
						{
							loginUser.Verified = true;
						}

						if (settings.AutomaticallyApproveNewUsers)
						{
							loginUser.Approved = true;
						}

						await this.UserManager.Save(site, loginUser);
					}
				}

				if (loginUser != null)
				{
					Boolean userProfileUpdated = false;
					Boolean userRolesUpdated = false;

					if (settings.SynchronizeProfile)
					{
						Logger?.LogTrace("Synchronizing profile for user '{name}'.", loginUser.UserName);

						foreach (Claim claim in user.Claims)
						{
							UserProfileValue prop = loginUser?.Profile.Where(profileProperty => profileProperty.UserProfileProperty.TypeUri == claim.Type).FirstOrDefault();
							// If a property with the same type as the incoming claim is found, update the property if the new value is different than the old value (with extra
							// checking so that an empty string is equivalent to null for the purpose of checking the value)
							if (prop != null && (String.IsNullOrEmpty(prop.Value) ? "" : prop.Value) != (String.IsNullOrEmpty(claim.Value) ? "" : claim.Value))
							{
								Logger?.LogTrace("Setting profile value '{typeuri}' [{profilePropertyName}] to '{value}'.", prop.UserProfileProperty.TypeUri, prop.UserProfileProperty.Name, claim.Value);
								prop.Value = claim.Value;
								userProfileUpdated = true;
							}
						}
					}

					if (settings.SynchronizeRoles)
					{
						IEnumerable<Claim> roleClaims = user.Claims.Where(claim => claim.Type == ClaimTypes.Role);

						// Only sync roles if the SAML provider returned role claims
						if (roleClaims.Any())
						{
							Logger?.LogTrace("Synchronizing roles for user '{name}'.", loginUser.UserName);

							if (settings.AddToRoles)
							{
								// Add user to roles in role claims, if a matching role name is found, and it isn't one of the special site roles.
								foreach (Claim claim in roleClaims)
								{
									Role role = await this.RoleManager.GetByName(site, claim.Value);
									if (role != null)
									{
										if (!IsSpecialRole(claim.Value))
										{
											if (!loginUser.Roles.Where(existing => existing.Name == role.Name).Any())
											{

												loginUser.Roles.Add(role);
												Logger?.LogTrace("Added user '{name}' to role '{role}'.", loginUser.UserName, claim.Value);
												userRolesUpdated = true;
											}
											else
											{
												Logger?.LogTrace("Did not add a role named '{roleName}' for user '{name}' because the user is already a member of that role.", claim.Value, loginUser.UserName);
											}
										}
										else
										{
											Logger?.LogTrace("Ignored a role named '{roleName}' because the role is a special role.", claim.Value);
										}
									}
									else
									{
										Logger?.LogTrace("Did not add a role named '{roleName}' for user '{name}' because no role with that name exists.", claim.Value, loginUser.UserName);
									}
								}
							}

							if (settings.RemoveFromRoles)
							{
								foreach (Role role in loginUser.Roles.ToArray())
								{
									if (!roleClaims.Where(claim => claim.Value == role.Name).Any())
									{
										if (!IsSpecialRole(role.Name))
										{
											// Role assigned to user is not present in role claims, remove it if it isn't one of the special site roles
											loginUser.Roles.Remove(role);
											Logger?.LogTrace("Removed user '{name}' from role '{role}'.", loginUser.UserName, role.Name);
											userRolesUpdated = true;
										}
										else
										{
											Logger?.LogTrace("Ignored a role named '{roleName}' because the role is a special role.", role.Name);
										}
									}
								}
							}
						}
						else
						{
							Logger?.LogTrace("Nucleus did not synchronize roles for user '{name}' because the SAML provider did not return any roles.", loginUser.UserName);
						}

						if (userProfileUpdated || userRolesUpdated)
						{
							// save user if any roles have changed
							await this.UserManager.Save(site, loginUser);
						}
					}

					UserSession session = await this.SessionManager.CreateNew(site, loginUser, false, HttpContext.Connection.RemoteIpAddress);

					string redirectUrl = properties.ContainsKey(relayStateReturnUrl) ? properties[relayStateReturnUrl] : "~/";
					await this.SessionManager.SignIn(session, HttpContext, redirectUrl);

					Logger?.LogTrace("Signin for user '{name}' was successful, redirecting to '{url}'.", loginUser.UserName, redirectUrl);
					return Redirect(redirectUrl);
				}
				else
				{
					return Forbid();
				}
			}
		}

		/// <summary>
		/// Return true if the role identified by <paramref name="name"/> is a "special meaning" role.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is used to determine whether to add/remove a local user from roles provided by the Identity Provider. User membership in 
		/// "special roles" is not synchronized using data from the Identity Provider.  Only "non-special" roles are synchronized.
		/// </remarks>
		private Boolean IsSpecialRole(string name)
		{
			if (name == this.Context.Site.AdministratorsRole.Name) return true;
			if (name == this.Context.Site.AllUsersRole.Name) return true;
			if (name == this.Context.Site.AnonymousUsersRole.Name) return true;
			if (name == this.Context.Site.RegisteredUsersRole.Name) return true;

			return false;
		}

		/// <summary>
		/// Render a list of configured identity providers.
		/// </summary>
		/// <param name="redirectUri"></param>
		/// <returns></returns>
		private ViewModels.Viewer BuildViewModel(string redirectUri)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.Options = this.Options.Value;
			viewModel.ReturnUrl = redirectUri;

			viewModel.ReadSettings(this.Context.Module);

			string layoutPath = $"ViewerLayouts/{viewModel.Layout}.cshtml";

			if (!System.IO.File.Exists($"{this.FolderOptions.GetExtensionFolder("SAML Client", false)}/Views/{layoutPath}"))
			{
				layoutPath = $"ViewerLayouts/List.cshtml";
			}

			viewModel.Layout = layoutPath;

			return viewModel;
		}

		/// <summary>
		/// Generate an absolute Url to redirect to after login.  The specified <paramref name="returnUrl"/> must be 
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		private string BuildRedirectUrl(string returnUrl)
		{
			// Only allow a relative path for redirectUri (that is, the url must start with "/"), to ensure that it points to "this"
			// site.					
			return Url.Content(String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "~/" : returnUrl);
		}
	}
}
