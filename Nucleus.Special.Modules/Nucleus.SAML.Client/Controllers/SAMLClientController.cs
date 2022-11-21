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

// https://www.itfoxtec.com/IdentitySaml2
// https://www.itfoxtec.com/License/3clauseBSD.htm
// https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/blob/master/test/TestWebAppCore/Controllers/AuthController.cs

// https://medium.com/the-new-control-plane/i-need-a-saml-idp-to-test-now-477761595b60

// SAML Response Validator:
// https://www.samltool.com/validate_response.php

namespace Nucleus.SAML.Client.Controllers
{
	[Extension("SAMLClient")]
	public class SAMLClientController : Controller
	{
		private IWebHostEnvironment WebHostEnvironment { get; }
		private IHttpClientFactory HttpClientFactory { get; }
		private IUrlHelperFactory UrlHelperFactory { get; }
		private Context Context { get; }

		private IUserManager UserManager { get; }

		private IRoleManager RoleManager { get; }
		private ISessionManager SessionManager { get; }

		private IOptions<Models.Configuration.SAMLProviders> Options { get; }
		private ILogger<SAMLClientController> Logger { get; }

		const string relayStateReturnUrl = "ReturnUrl";

		public SAMLClientController(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory, IUrlHelperFactory urlHelperFactory, Context Context, ISessionManager sessionManager, IUserManager userManager, IRoleManager roleManager, IOptions<Models.Configuration.SAMLProviders> options, ILogger<SAMLClientController> logger)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.HttpClientFactory = httpClientFactory;
			this.UrlHelperFactory = urlHelperFactory;
			this.Context = Context;
			this.SessionManager = sessionManager;
			this.UserManager = userManager;
			this.RoleManager = roleManager;
			this.Options = options;
			this.Logger = logger;
		}


		[HttpGet]
		public ActionResult Index(string returnUrl)
		{
			ViewModels.Viewer viewModel = BuildViewModel(returnUrl);
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
						return Challenge(new AuthenticationProperties() { RedirectUri = url }, providerOption.Name);
					}
				}
			}

			return View("Viewer", viewModel);
		}

		// https://golem.inventua.com:5001/extensions/SAMLClient/Authenticate/localtest
		// https://golem.inventua.com:5001/extensions/SAMLClient/Authenticate/start.sharpamericas.com
		// https://golem.inventua.com:5001/extensions/SAMLClient/Authenticate/start.sharpusa.net
		[HttpGet]
		[Route($"{Routes.AUTHENTICATE}/{{providerName}}")]
		public async Task<IActionResult> Authenticate(string providerName, string returnUrl)
		{
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);

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
					AssertionConsumerServiceUrl = Nucleus.ViewFeatures.UrlHelperExtensions.GetAbsoluteUri(urlHelper, $"{Routes.ASSERTION_CONSUMER}/{providerName}"),
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

					////case Models.Configuration.SAMLProvider.ProtocolBindingTypes.Soap:
					////{
					////	Saml2SoapBinding binding = new();
					////	binding.SetRelayStateQuery(new Dictionary<string, string> { { relayStateReturnUrl, returnUrl ?? Url.Content("~/") } });
					////	return binding.Bind(request).ToActionResult();
					////}

					default:
						throw new InvalidOperationException($"Unsupported request protocol binding: '{providerOption.RequestProtocolBinding}'");
				}								
			}
			else
			{
				Logger?.LogTrace("SAML provider {providername} not found.  Check your configuration files Nucleus:SAMLProviders section for a provider with a matching name, or a matching type and no name.", providerName);
				return BadRequest();
			}
		}

		private async Task<IActionResult> ParseAuthnResponse(string providerName)
		{
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);
			Saml2Configuration config = await BuildConfiguration(providerOption);
			Saml2AuthnResponse saml2AuthnResponse = new(config);
			Dictionary<string, string> relayStateQuery;

			switch (providerOption.GetResponseProtocolBinding())
			{
				case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpRedirect:
				{
					Saml2RedirectBinding binding = new();
					binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					relayStateQuery = binding.GetRelayStateQuery();
					break;
				}

				case Models.Configuration.SAMLProvider.ProtocolBindingTypes.HttpArtifact:
				{
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
					Saml2PostBinding binding = new();
					binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
					relayStateQuery = binding.GetRelayStateQuery();
					break;
				}

				default:
					throw new AuthenticationException($"SAML: Unhandled protocol binding {providerOption.ResponseProtocolBinding}");
			}

			if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
			{
				throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
			}

			// Translate SAML claims to local (Nucleus) claims, using values from config SAMLProviders[@Name=providerName]:MapClaims
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
				return Forbid();
			}

			return await HandleSignInAsync(this.Context.Site, principal, relayStateQuery);

		}

		[HttpGet]
		[Route($"{Routes.ASSERTION_CONSUMER}/{{providerName}}")]
		public async Task<IActionResult> Callback(string providerName, string SAMLArt)
		{
			try
			{
				return await ParseAuthnResponse(providerName);
			}
			catch (ITfoxtec.Identity.Saml2.Cryptography.InvalidSignatureException e)
			{
				Logger.LogError(e, "Exception thrown validating SAML2 Authn Response.");
				throw e;
			}
		}

		[HttpPost]
		[Route($"{Routes.ASSERTION_CONSUMER}/{{providerName}}")]
		public async Task<IActionResult> Callback(string providerName)
		{
			try
			{
				return await ParseAuthnResponse(providerName);
			}
			catch (ITfoxtec.Identity.Saml2.Cryptography.InvalidSignatureException e)
			{
				Logger.LogError(e, "Exception thrown validating SAML2 Authn Response.");
				throw e;
			}


			//try
			//{
			//	// This line doesn't work property if the assertion is signed (if the response is signed, it works fine).  This may be 
			//	// a bug in ITfoxtec.Identity.Saml2 or Microsoft.IdentityModel.Xml.dll
			//	binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
			//}
			//catch (ITfoxtec.Identity.Saml2.Cryptography.InvalidSignatureException e)
			//{
			//	Logger.LogError(e, "Exception thrown validating SAML2 Authn Response.");
			//	throw e;
			//}

			//// Translate SAML claims to local (Nucleus) claims, using values from config SAMLProviders[@Name=providerName]:MapClaims
			//ClaimsPrincipal principal = await saml2AuthnResponse.CreateSession
			//(
			//	HttpContext,
			//	claimsTransform: claimsPrincipal => ClaimsTransformExtensions.Transform
			//	(
			//		claimsPrincipal,
			//		providerOption.MapClaims,
			//		this.Logger
			//	)
			//);

			//return await HandleSignInAsync(this.Context.Site, principal, binding.GetRelayStateQuery());

		}

		[HttpGet]
		[Route($"{Routes.METADATA}/{{providerName}}")]
		public async Task<IActionResult> Metadata(string providerName)
		{
			var defaultSite = new Uri($"{Request.Scheme}://{Request.Host.ToUriComponent()}/");
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);

			if (providerOption == null)
			{
				return NotFound($"Provider '{providerName}' not found.");
			}

			Saml2Configuration config = await BuildConfiguration(providerOption);

			EntityDescriptor entityDescriptor = new(config)
			{
				ValidUntil = 365,
				SPSsoDescriptor = new()
				{
					WantAssertionsSigned = true,

					AuthnRequestsSigned = providerOption.SignAuthnRequest,

					SigningCertificates = new X509Certificate2[]
					{
						config.SigningCertificate
					},
					//EncryptionCertificates = new X509Certificate2[]
					//{
					//    config.DecryptionCertificate
					//},
					SingleLogoutServices = new SingleLogoutService[]
					{
						new SingleLogoutService
						{
							Binding = ProtocolBindings.HttpPost,
							Location = new Uri(defaultSite, $"{Routes.SINGLE_LOGOUT}/{providerName}"),
							//ResponseLocation = new Uri(defaultSite,Url.NucleusAction(nameof(SingleLogout),"SAMLClient","SAMLClient"))// $"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension=SAMLClient}}/{{action={nameof(LoggedOut)}}}/{{providerName}}")
						}
					},

					NameIDFormats = new Uri[] { NameIdentifierFormats.X509SubjectName },

					AssertionConsumerServices = new AssertionConsumerService[]
					{
						new AssertionConsumerService
						{
							Binding = ProtocolBindings.HttpPost,
							Location = new Uri(defaultSite, $"{Routes.ASSERTION_CONSUMER}/{providerName}")
						},
					},

					AttributeConsumingServices = new AttributeConsumingService[]
					{
						new AttributeConsumingService
						{
							ServiceName = new ServiceName($"{this.Context.Site.Name} [{providerName}] SP" , "en"),
							RequestedAttributes = CreateRequestedAttributes()
						}
					}
				},

				ContactPersons = new ContactPerson[] {
					new ContactPerson(ContactTypes.Administrative)
					{
						// todo: populate proper values
						Company = "Some Company",
						GivenName = "Some Given Name",
						SurName = "Some Sur Name",
						EmailAddress = "some@some-domain.com",
						TelephoneNumber = "11111111",
					}
				}
			};	

			return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
		}

		private IEnumerable<RequestedAttribute> CreateRequestedAttributes()
		{
			yield return new RequestedAttribute("urn:oid:2.5.4.41");                    // name
			yield return new RequestedAttribute("urn:oid:2.5.4.42");                    // givenName
			yield return new RequestedAttribute("urn:oid:2.5.4.4");                     // surname
			yield return new RequestedAttribute("urn:oid:2.5.4.3");                     // common name
			yield return new RequestedAttribute("urn:oid:0.9.2342.19200300.100.1.3");   // email
		}

		[HttpPost]
		[Route($"{Routes.LOGOUT}/{{providerName}}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout(string providerName)
		{
			if (!User.Identity.IsAuthenticated)
			{
				return Redirect(Url.Content("~/"));
			}

			Saml2PostBinding binding = new();
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);

			var saml2LogoutRequest = await new Saml2LogoutRequest(await BuildConfiguration(providerOption), User).DeleteSession(HttpContext);
			return binding.Bind(saml2LogoutRequest).ToActionResult();
		}

		[Route($"{Routes.LOGGED_OUT}/{{providerName}}")]
		public async Task<IActionResult> LoggedOut(string providerName)
		{
			var binding = new Saml2PostBinding();
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);

			binding.Unbind(Request.ToGenericHttpRequest(), new Saml2LogoutResponse(await BuildConfiguration(providerOption)));

			return Redirect(Url.Content("~/"));
		}

		[Route($"{Routes.SINGLE_LOGOUT}/{{providerName}}")]
		public async Task<IActionResult> SingleLogout(string providerName)
		{
			Saml2StatusCodes status;
			Saml2PostBinding requestBinding = new();
			Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);

			var logoutRequest = new Saml2LogoutRequest(await BuildConfiguration(providerOption), User);
			try
			{
				requestBinding.Unbind(Request.ToGenericHttpRequest(), logoutRequest);
				status = Saml2StatusCodes.Success;
				await logoutRequest.DeleteSession(HttpContext);
			}
			catch (Exception exc)
			{
				// log exception
				Debug.WriteLine("SingleLogout error: " + exc.ToString());
				status = Saml2StatusCodes.RequestDenied;
			}

			var responsebinding = new Saml2PostBinding();
			responsebinding.RelayState = requestBinding.RelayState;
			var saml2LogoutResponse = new Saml2LogoutResponse(await BuildConfiguration(providerOption))
			{
				InResponseToAsString = logoutRequest.IdAsString,
				Status = status,
			};
			return responsebinding.Bind(saml2LogoutResponse).ToActionResult();
		}


		private Models.Configuration.SAMLProvider GetProvider(string providerName)
		{
			if (!String.IsNullOrEmpty(providerName))
			{
				Logger?.LogTrace("SAML provider {providername} requested.", providerName);

				// Find provider configuration matching the supplied providerName.  
				return this.Options.Value
					.Where(option => (option.SafeProviderName().Equals(providerName, StringComparison.OrdinalIgnoreCase)))
					.FirstOrDefault();
			}

			return null;
		}

		private async Task<Saml2Configuration> BuildConfiguration(Models.Configuration.SAMLProvider providerOption)
		{
			Saml2Configuration saml2Configuration = new();

			saml2Configuration.SignAuthnRequest = providerOption.SignAuthnRequest;
			saml2Configuration.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
			saml2Configuration.RevocationMode = X509RevocationMode.NoCheck;

			if (!String.IsNullOrEmpty(providerOption.SigningCertificateFile))
			{
				saml2Configuration.SigningCertificate = CertificateUtil.Load(this.WebHostEnvironment.MapToPhysicalFilePath(providerOption.SigningCertificateFile), providerOption.SigningCertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
			}
			//Alternatively load the certificate by thumbprint from the machines Certificate Store.
			if (!String.IsNullOrEmpty(providerOption.SigningCertificateThumbprint))
			{
				saml2Configuration.SigningCertificate = CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, providerOption.SigningCertificateThumbprint);
			}

			if (saml2Configuration.SignAuthnRequest && saml2Configuration.SigningCertificate == null)
			{
				Logger?.LogError("SAML [{name}]: The certificate could not be loaded using the SigningCertificateFile or SigningCertificateThumbprint value.", providerOption.Name);
				throw new InvalidOperationException($"SAML [{providerOption.Name}]: The SignAuthnRequest property is set to true, but the certificate could not be loaded using the SigningCertificateFile or SigningCertificateThumbprint value.");
			}
			

			if (!String.IsNullOrEmpty(providerOption.SignatureValidationCertificateFile))
			{
				if (String.IsNullOrEmpty(providerOption.SignatureValidationCertificatePassword))
				{
					saml2Configuration.SignatureValidationCertificates.Add(CertificateUtil.Load(this.WebHostEnvironment.MapToPhysicalFilePath(providerOption.SignatureValidationCertificateFile)));
				}
				else
				{
					saml2Configuration.SignatureValidationCertificates.Add(CertificateUtil.Load(this.WebHostEnvironment.MapToPhysicalFilePath(providerOption.SignatureValidationCertificateFile), providerOption.SignatureValidationCertificatePassword));
				}
			}

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
						if (signingCertificate.IsValidLocalTime())
						{
							saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
						}
					}
					if (saml2Configuration.SignatureValidationCertificates.Count <= 0)
					{
						Logger?.LogError("SAML [{name}]: The IdP signing certificate has expired.", providerOption.Name);
						throw new InvalidOperationException($"SAML [{providerOption.Name}]: The IdP signing certificate has expired.");
					}
					if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
					{
						saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
					}
				}
			}
			else
			{
				saml2Configuration.AllowedIssuer = providerOption.AllowedIssuer;

				if (!String.IsNullOrEmpty(providerOption.Issuer))
				{
					saml2Configuration.Issuer = providerOption.Issuer;
					saml2Configuration.AllowedAudienceUris.Add(saml2Configuration.Issuer);
				}

				if (!String.IsNullOrEmpty(providerOption.SingleSignOnDestination))
				{
					saml2Configuration.SingleSignOnDestination = new Uri(providerOption.SingleSignOnDestination);
				}

				if (!String.IsNullOrEmpty(providerOption.SingleLogoutDestination))
				{
					saml2Configuration.SingleLogoutDestination = new Uri(providerOption.SingleLogoutDestination);
				}

				saml2Configuration.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;

				if (!String.IsNullOrEmpty(providerOption.SignatureAlgorithm))
				{
					saml2Configuration.SignatureAlgorithm = providerOption.SignatureAlgorithm;
				}
				else
				{
					saml2Configuration.SignatureAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
				}
			}

			//else
			//{
			//	throw new Exception("IdPSsoDescriptor not loaded from metadata.");
			//}

			return saml2Configuration;
		}
		

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
				return Forbid();
				//await base.ForbidAsync(properties);
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
						Logger?.LogTrace("A matching user was not found, and the SAML server CreateUsers setting is set to false.");
						return Forbid();
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

		private Boolean IsSpecialRole(string name)
		{
			if (name == this.Context.Site.AdministratorsRole.Name) return true;
			if (name == this.Context.Site.AllUsersRole.Name) return true;
			if (name == this.Context.Site.AnonymousUsersRole.Name) return true;
			if (name == this.Context.Site.RegisteredUsersRole.Name) return true;

			return false;
		}

		private ViewModels.Viewer BuildViewModel(string redirectUri)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.Options = this.Options.Value;
			viewModel.ReturnUrl = redirectUri;

			viewModel.ReadSettings(this.Context.Module);

			string layoutPath = $"ViewerLayouts\\{viewModel.Layout}.cshtml";

			if (!System.IO.File.Exists($"{this.WebHostEnvironment.ContentRootPath}\\{FolderOptions.EXTENSIONS_FOLDER}\\SAML Client\\Views\\{layoutPath}"))
			{
				layoutPath = $"ViewerLayouts\\List.cshtml";
			}

			viewModel.Layout = layoutPath;

			return viewModel;
		}

		private string BuildRedirectUrl(string returnUrl)
		{
			// Only allow a relative path for redirectUri (that is, the url must start with "/"), to ensure that it points to "this" site.					
			return Url.Content(String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "~/" : returnUrl);
		}


		////private async Task TestValidate(string providerName)
		////{
		////	Saml2PostBinding binding = new();

		////	Models.Configuration.SAMLProvider providerOption = GetProvider(providerName);
		////	Saml2Configuration config = await BuildConfiguration(providerOption);

		////	Saml2AuthnResponse saml2AuthnResponse = new(config);

		////	binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
		////	if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
		////	{
		////		throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
		////	}

		////	// Test use 
		////	{
		////		// Manually Validate assertion signature
		////		var xmlElements = saml2AuthnResponse.XmlDocument.DocumentElement.SelectNodes($"//*[local-name()='{ITfoxtec.Identity.Saml2.Schemas.Saml2Constants.Message.Assertion}']");
		////		var xmlElement = xmlElements[0] as System.Xml.XmlElement;
		////		var xmlSignatures = xmlElement.SelectNodes($"*[local-name()='Signature' and namespace-uri()='{System.Security.Cryptography.Xml.SignedXml.XmlDsigNamespaceUrl}']");

		////		// only validate signature if the assertion is signed
		////		if (xmlSignatures.Count > 0)
		////		{
		////			var signedXml = new ITfoxtec.Identity.Saml2.Cryptography.Saml2SignedXml(
		////			xmlElement,
		////			config.SignatureValidationCertificates.First(),
		////			config.SignatureAlgorithm,
		////			config.XmlCanonicalizationMethod);

		////			signedXml.LoadXml(xmlSignatures[0] as System.Xml.XmlElement);
		////			if (signedXml.CheckSignature())
		////			{
		////				// Signature is valid.

		////			}
		////		}
		////	}

		////	{
		////		// Manually Validate response signature

		////		var xmlElement = saml2AuthnResponse.XmlDocument.DocumentElement as System.Xml.XmlElement;
		////		var xmlSignatures = xmlElement.SelectNodes($"*[local-name()='Signature' and namespace-uri()='{System.Security.Cryptography.Xml.SignedXml.XmlDsigNamespaceUrl}']");

		////		// only validate signature if the response is signed
		////		if (xmlSignatures.Count > 0)
		////		{
		////			var signedXml = new ITfoxtec.Identity.Saml2.Cryptography.Saml2SignedXml(
		////			xmlElement,
		////			config.SignatureValidationCertificates.First(),
		////			config.SignatureAlgorithm,
		////			config.XmlCanonicalizationMethod);

		////			signedXml.LoadXml(xmlSignatures[0] as System.Xml.XmlElement);
		////			if (signedXml.CheckSignature())
		////			{
		////				// Signature is valid.

		////			}
		////		}
		////	}
		////}
	}
}