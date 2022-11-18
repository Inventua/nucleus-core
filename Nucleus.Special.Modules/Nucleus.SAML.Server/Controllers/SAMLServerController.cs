using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.MvcCore;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens.Saml2;
using Nucleus.SAML.Server.Models;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using System.Xml;
using Saml2Constants = ITfoxtec.Identity.Saml2.Schemas.Saml2Constants;
using DocumentFormat.OpenXml.Wordprocessing;
using ITfoxtec.Identity.Saml2.Schemas.Conditions;

// https://www.itfoxtec.com/IdentitySaml2
// https://www.itfoxtec.com/License/3clauseBSD.htm
// https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/blob/master/test/TestWebAppCore/Controllers/AuthController.cs

namespace Nucleus.SAML.Server.Controllers
{
	[Extension("SAMLServer")]
	public class SAMLServerController : Controller
	{
		private Context Context { get; }

		private IUserManager UserManager { get; }
		private ISessionManager SessionManager { get; }

		private IPageManager PageManager { get; }

		//private Saml2Configuration Config { get; }

		private IHttpClientFactory HttpClientFactory { get; }

		private ClientAppManager ClientAppManager { get; }

		private ClientAppTokenManager ClientAppTokenManager { get; }

		private ILogger<SAMLServerController> Logger { get; }

		private string _issuer;

		public SAMLServerController(Context context, ISessionManager sessionManager, IUserManager userManager, IPageManager pageManager, ClientAppManager clientAppManager, ClientAppTokenManager clientAppTokenManager, IHttpClientFactory httpClientFactory, ILogger<SAMLServerController> logger)
		{
			this.Context = context;
			this.SessionManager = sessionManager;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.ClientAppManager = clientAppManager;
			this.ClientAppTokenManager = clientAppTokenManager;
			this.HttpClientFactory = httpClientFactory;
			this.Logger = logger;
		}

		private string Issuer
		{
			get
			{
				if (_issuer == null)
				{
					Saml2AuthnRequest saml2AuthnRequest = new(new());
					if (Request.Method == "POST")
					{
						Saml2PostBinding requestBinding = new();
						requestBinding.ReadSamlRequest(Request.ToGenericHttpRequest(), saml2AuthnRequest);
						_issuer = saml2AuthnRequest.Issuer;
					}
					else
					{
						Saml2RedirectBinding requestBinding = new();
						requestBinding.ReadSamlRequest(Request.ToGenericHttpRequest(), saml2AuthnRequest);
						_issuer = saml2AuthnRequest.Issuer;
					}
				}
				return _issuer;
			}
		}

		[Route(Routes.LOGIN)]
		public async Task<IActionResult> Login(string relayState)
		{
			RelyingParty relyingParty = await ValidateRelyingParty(this.Issuer);
			Saml2AuthnRequest saml2AuthnRequest = new(await GetRpSaml2Configuration(relyingParty));

			try
			{
				if (Request.Method == "POST")
				{
					Saml2PostBinding requestBinding = new();
					requestBinding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnRequest);
				}
				else
				{
					Saml2RedirectBinding requestBinding = new();
					requestBinding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnRequest);
				}

				ClientAppToken token = this.ClientAppTokenManager.CreateNew();

				token.ClientApp = await this.ClientAppManager.GetByIssuer(this.Issuer);
				token.RequestId = saml2AuthnRequest.IdAsString;
				token.AssertionConsumerServiceUrl = saml2AuthnRequest.AssertionConsumerServiceUrl.ToString();
				token.ProtocolBinding = saml2AuthnRequest.ProtocolBinding.ToString();
				token.RelayState = relayState;

				await this.ClientAppTokenManager.Save(token);

				if (User.Identity.IsAuthenticated && !saml2AuthnRequest.ForceAuthn == true)
				{
					Logger.LogTrace("A user is already logged in.");
					// User is already logged in, redirect back immediately
					return await Respond(token.Id);
				}
				else
				{
					// Redirect to the login page
					return await RedirectToLogin(token);
				}
			}
			catch (Exception exc)
			{
				Logger?.LogError(exc, "SAML Login Exception");
				return await LoginResponse(new() { RequestId = saml2AuthnRequest.IdAsString, RelayState = relayState }, Saml2StatusCodes.Responder, relyingParty);
			}
		}

		private async Task<RedirectResult> RedirectToLogin(ClientAppToken token)
		{
			string url;

			if (token.ClientApp.LoginPage != null)
			{
				url = $"{token.ClientApp.LoginPage.DefaultPageRoute}?returnUrl=/SAML2/respond/{token.Id}";
			}
			else
			{
				if (this.Context.Site != null)
				{
					SitePages sitePage = this.Context.Site.GetSitePages();
					PageRoute loginPageRoute = null;

					if (sitePage.LoginPageId.HasValue)
					{
						Page loginPage = await this.PageManager.Get(sitePage.LoginPageId.Value);
						if (loginPage != null)
						{
							loginPageRoute = loginPage.DefaultPageRoute();
						}
					}
					if (loginPageRoute == null)
					{
						// Use default login page
						url = this.DefaultLoginUri();
					}
					else
					{
						url = loginPageRoute.Path;
					}
				}
				else
				{
					// use default login page
					url = this.DefaultLoginUri();
				}

				url = $"{url}?returnUrl={Routes.RESPOND}/{token.Id}";
			}

			Logger?.LogTrace("Redirecting to '{url}'.", url);
			return Redirect(url);
		}

		/// <summary>
		/// Receive a redirect from the Nucleus login module, process it and redirect back to the original caller (SAML client).
		/// </summary>
		/// <param name="id">The id of an app token created by /Authorize and included in the ReturnUri sent to the login module.</param>
		/// <returns></returns>
		[HttpGet]
		[Route($"{Routes.RESPOND}/{{id}}")]
		public async Task<ActionResult> Respond(Guid id)
		{
			ClientAppToken token = await this.ClientAppTokenManager.Get(id);

			if (token == null)
			{
				return BadRequest("Your login session has expired.");
			}

			token.UserId = User.GetUserId();
			await this.ClientAppTokenManager.Save(token);

			RelyingParty relyingParty = new()
			{
				AssertionConsumerServiceUri = new System.Uri(token.AssertionConsumerServiceUrl),
				Issuer = token.ClientApp.AllowedIssuer,
				ProtocolBinding = token.ProtocolBinding
			};

			var result = await LoginResponse(token, Saml2StatusCodes.Success, relyingParty);

			// save artifact
			await this.ClientAppTokenManager.Save(token);

			return (ActionResult)result;
		}

		[Route(Routes.ARTIFACT)]
		public async Task<IActionResult> Artifact()
		{
			try
			{
				Saml2SoapEnvelope soapEnvelope = new();
				ITfoxtec.Identity.Saml2.Http.HttpRequest httpRequest = await Request.ToGenericHttpRequestAsync(readBodyAsString: true);

				// We get the signature validation certificate from this.ClientAppTokenManager.GetByCode (which needs the artifact)
				// but we can't get the artifact without calling soapEnvelope.Unbind - which needs the signature validation certificate.

				// So we have to cheat and get the artifact ourselves
				System.Xml.XmlDocument xmlDoc = new();
				xmlDoc.LoadXml(httpRequest.Body);
				string artifact = xmlDoc.DocumentElement.SelectSingleNode("//*[local-name()='Artifact']").InnerText;

				ClientAppToken token = await this.ClientAppTokenManager.GetByCode(artifact);

				if (token == null)
				{
					return BadRequest();
				}

				Config config = new(this.Request, this.Context, token.ClientApp);

				Saml2ArtifactResolve saml2ArtifactResolve = new(config.AsSaml2Configuration());
				soapEnvelope.Unbind(httpRequest, saml2ArtifactResolve);

				Saml2AuthnResponse saml2AuthnResponse = new(config.AsSaml2Configuration())
				{
					ClaimsIdentity = new ClaimsIdentity(await BuildUserClaims(token.UserId))
				};

				Saml2ArtifactResponse saml2ArtifactResponse = new(config.AsSaml2Configuration(), saml2AuthnResponse)
				{
					InResponseTo = saml2ArtifactResolve.Id,
					NameId = new Saml2NameIdentifier(token.UserId.ToString(), NameIdentifierFormats.Persistent)
				};

				// We have to set destination here rather than above, because it gets NULLed by the Saml2ArtifactResponse constructor
				saml2AuthnResponse.Destination = saml2ArtifactResolve.Destination;

				saml2AuthnResponse.CreateSecurityToken(token.ClientApp.AllowedIssuer, subjectConfirmationLifetime: 5, issuedTokenLifetime: 60);
				soapEnvelope.Bind(saml2ArtifactResponse);

				return soapEnvelope.ToActionResult();
			}
			catch (Exception exc)
			{
				Logger?.LogError(exc, "SAML Artifact Exception");
				throw;
			}
		}

		[HttpPost(Routes.LOGOUT)]
		public async Task<IActionResult> Logout()
		{
			// validate request
			Saml2PostBinding requestBinding = new();
			RelyingParty relyingParty = await ValidateRelyingParty(await ReadRelyingPartyFromLogoutRequest(requestBinding));
			Saml2LogoutRequest saml2LogoutRequest = new (await GetRpSaml2Configuration(relyingParty));

			try
			{
				requestBinding.Unbind(Request.ToGenericHttpRequest(), saml2LogoutRequest);

				// log out
				await	this.SessionManager.SignOut(HttpContext);

				return await LogoutResponse(saml2LogoutRequest.Id, Saml2StatusCodes.Success, requestBinding.RelayState, saml2LogoutRequest.SessionIndex, relyingParty);
			}
			catch (Exception exc)
			{
				Logger?.LogError(exc, "SAML Logout Exception");
				return await LogoutResponse(saml2LogoutRequest.Id, Saml2StatusCodes.Responder, requestBinding.RelayState, saml2LogoutRequest.SessionIndex, relyingParty);
			}
		}


		[Route($"{Routes.METADATA}/{{issuer}}")]
		public async Task<IActionResult> Metadata(string issuer)
		{
			ClientApp clientApp = await this.ClientAppManager.GetByIssuer(issuer);

			if (clientApp == null)
			{
				throw new InvalidOperationException($"Client App for issuer '{issuer}' not found.");
			}

			Config config = new(this.Request, this.Context, clientApp);

			Uri defaultSite = new(Request.Scheme + Uri.SchemeDelimiter + Request.Host.ToUriComponent() + Request.PathBase);

			EntityDescriptor entityDescriptor = new(config.AsSaml2Configuration());

			entityDescriptor.ValidUntil = 365;
			entityDescriptor.IdPSsoDescriptor = new IdPSsoDescriptor
			{
				SigningCertificates = new X509Certificate2[]
				{
					config.SigningCertificate
				},
				//EncryptionCertificates = new X509Certificate2[]
				//{
				//    config.DecryptionCertificate
				//},
				SingleSignOnServices = new SingleSignOnService[]
				{
					new SingleSignOnService
					{
						Binding = ProtocolBindings.HttpRedirect,
						Location = new Uri(defaultSite, $"{Routes.LOGIN}/{issuer}")
					}
				},
				SingleLogoutServices = new SingleLogoutService[]
				{
					new SingleLogoutService
					{
						Binding = ProtocolBindings.HttpPost,
						Location = new Uri(defaultSite, $"{Routes.LOGOUT}/{issuer}")
					}
				},
				ArtifactResolutionServices = new ArtifactResolutionService[]
				{
					new ArtifactResolutionService
					{
						Binding = ProtocolBindings.ArtifactSoap,
						Index = 0,
						Location = new Uri(defaultSite, $"{Routes.ARTIFACT}")
					}
				},
				NameIDFormats = new Uri[] { NameIdentifierFormats.X509SubjectName },
			};

			entityDescriptor.ContactPersons = new[]
			{
				new ContactPerson(ContactTypes.Administrative)
				{
					// todo
					Company = "Some Company",
					GivenName = "Some Given Name",
					SurName = "Some Sur Name",
					EmailAddress = "some@some-domain.com",
					TelephoneNumber = "11111111",
				}
			};


			return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
		}

		private async Task<string> ReadRelyingPartyFromLogoutRequest<T>(Saml2Binding<T> binding)
		{
			return binding.ReadSamlRequest(Request.ToGenericHttpRequest(), new Saml2LogoutRequest(await GetRpSaml2Configuration()))?.Issuer;
		}

		private async Task<IActionResult> LoginResponse(ClientAppToken token, Saml2StatusCodes status, RelyingParty relyingParty)
		{
			if (relyingParty.ProtocolBinding == "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST")
			{
				return await LoginPostResponse(new Saml2Id(token.RequestId), status, token.RelayState, relyingParty, await BuildUserClaims(token.UserId));
			}
			else  // urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Artifact
			{
				return await LoginArtifactResponse(token, status, relyingParty);
			}
		}

		private async Task<IActionResult> LoginPostResponse(Saml2Id inResponseTo, Saml2StatusCodes status, string relayState, RelyingParty relyingParty, IEnumerable<Claim> claims = null)
		{
			ClientApp clientApp = await this.ClientAppManager.GetByIssuer(relyingParty.Issuer);
			Config config = new(this.Request, this.Context, clientApp);

			Saml2Configuration responseConfig = new()
			{
				ArtifactResolutionService = config.ArtifactResolutionService,
				SigningCertificate = config.SigningCertificate
			};

			Saml2AuthnResponse saml2AuthnResponse = new(responseConfig)
			{
				Issuer = Request.Issuer(),
				InResponseTo = inResponseTo,
				Status = status,
				Destination = relyingParty.AssertionConsumerServiceUri,
			};

			if (status == Saml2StatusCodes.Success && claims != null)
			{
				ClaimsIdentity claimsIdentity = new(claims);
				saml2AuthnResponse.NameId = new Saml2NameIdentifier(claimsIdentity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).Single(), NameIdentifierFormats.Persistent);
				saml2AuthnResponse.ClaimsIdentity = claimsIdentity;

				saml2AuthnResponse.CreateSecurityToken(clientApp.AllowedIssuer, subjectConfirmationLifetime: 5, issuedTokenLifetime: 60);
			}

			Saml2PostBinding responsebinding = new()
			{
				RelayState = relayState
			};

			return responsebinding.Bind(saml2AuthnResponse).ToActionResult();
		}

		private async Task<IActionResult> LoginArtifactResponse(Models.ClientAppToken token, Saml2StatusCodes status, RelyingParty relyingParty)
		{
			Saml2ArtifactBinding responsebinding = new() 
			{
				RelayState = token.RelayState
			};

			Saml2ArtifactResolve saml2ArtifactResolve = new(await GetRpSaml2Configuration(relyingParty))
			{
				Issuer = Request.Issuer(),
				Destination = relyingParty.AssertionConsumerServiceUri
			};

			responsebinding.Bind(saml2ArtifactResolve);

			Saml2AuthnResponse saml2AuthnResponse = new(new Config(this.Request, this.Context, token.ClientApp).AsSaml2Configuration())
			{
				Issuer = Request.Issuer(),
				InResponseTo = new Saml2Id(token.RequestId),// inResponseTo,
				Status = status,
				Destination = this.Context.Site.AbsoluteUri(Routes.ARTIFACT, Request.IsHttps)
			};

			IEnumerable<Claim> claims = await BuildUserClaims(token.UserId);

			if (status == Saml2StatusCodes.Success && claims != null)
			{
				ClaimsIdentity claimsIdentity = new(claims);
				saml2AuthnResponse.NameId = new Saml2NameIdentifier
				(
					token.UserId.ToString(),
					NameIdentifierFormats.Persistent
				);
				saml2AuthnResponse.ClaimsIdentity = claimsIdentity;

				saml2AuthnResponse.CreateSecurityToken(token.ClientApp.AllowedIssuer, subjectConfirmationLifetime: 5, issuedTokenLifetime: 60);
			}

			token.Code = saml2ArtifactResolve.Artifact;

			return responsebinding.ToActionResult();
		}

		private async Task<IActionResult> LogoutResponse(Saml2Id inResponseTo, Saml2StatusCodes status, string relayState, string sessionIndex, RelyingParty relyingParty)
		{
			var responsebinding = new Saml2PostBinding();
			responsebinding.RelayState = relayState;

			Saml2LogoutResponse saml2LogoutResponse = new(await GetRpSaml2Configuration(relyingParty))
			{
				InResponseTo = inResponseTo,
				Status = status,
				Destination = relyingParty.SingleLogoutDestination,
				SessionIndex = sessionIndex
			};

			return responsebinding.Bind(saml2LogoutResponse).ToActionResult();
		}

		private async Task<Saml2Configuration> GetRpSaml2Configuration(RelyingParty relyingParty = null)
		{
			ClientApp clientApp = await this.ClientAppManager.GetByIssuer(relyingParty?.Issuer ?? this.Issuer);
			Config config = new(this.Request, this.Context, clientApp);

			var rpConfig = new Saml2Configuration()
			{
				Issuer = config.Issuer,
				ArtifactResolutionService = new()
				{
					Index = 0,
					Location = relyingParty.AssertionConsumerServiceUri
				},
				SigningCertificate = config.SigningCertificate
			};

			if (relyingParty != null)
			{
				rpConfig.SignatureValidationCertificates.Add(relyingParty.SignatureValidationCertificate);
				rpConfig.EncryptionCertificate = relyingParty.EncryptionCertificate;
			}

			return rpConfig;
		}

		private async Task<RelyingParty> ValidateRelyingParty(string issuer)
		{
			var clientApp = await this.ClientAppManager.GetByIssuer(issuer);

			if (clientApp == null)
			{
				throw new InvalidOperationException($"Client app '{issuer}' not found.");
			}

			RelyingParty relyingParty = new()
			{
				Metadata = clientApp.ServiceProviderMetadataUrl
			};

			if (!String.IsNullOrEmpty(relyingParty.Metadata))
			{
				// Use SP metadata endpoint to validate caller
				using var cancellationTokenSource = new CancellationTokenSource(3 * 1000); // Cancel after 3 seconds.
				await LoadRelyingPartyAsync(relyingParty, cancellationTokenSource);
			}
			else
			{
				// No SP metadata endpoint, do not validate
				relyingParty.Issuer = this.Issuer;
			}

			return relyingParty;
		}

		private async Task LoadRelyingPartyAsync(RelyingParty relyingParty, CancellationTokenSource cancellationTokenSource)
		{
			try
			{
				// Load RP if not already loaded.
				if (string.IsNullOrEmpty(relyingParty.Issuer))
				{
					EntityDescriptor entityDescriptor = new();
					await entityDescriptor.ReadSPSsoDescriptorFromUrlAsync(this.HttpClientFactory, new Uri(relyingParty.Metadata), cancellationTokenSource.Token);

					if (entityDescriptor.SPSsoDescriptor != null)
					{
						relyingParty.Issuer = entityDescriptor.EntityId;
						relyingParty.AssertionConsumerServiceUri = entityDescriptor.SPSsoDescriptor.AssertionConsumerServices.Where(a => a.IsDefault).OrderBy(a => a.Index).First().Location;
						var singleLogoutService = entityDescriptor.SPSsoDescriptor.SingleLogoutServices.First();
						relyingParty.SingleLogoutDestination = singleLogoutService.ResponseLocation ?? singleLogoutService.Location;
						relyingParty.SignatureValidationCertificate = entityDescriptor.SPSsoDescriptor.SigningCertificates.First();
					}
					else
					{
						throw new Exception($"SPSsoDescriptor not loaded from metadata '{relyingParty.Metadata}'.");
					}
				}
			}
			catch (Exception exc)
			{
				//log error
				Logger?.LogError(exc, "SAML SPSsoDescriptor Exception");
				throw;
			}
		}

		private async Task<IEnumerable<Claim>> BuildUserClaims(Guid? userId)
		{
			List<Claim> claims = new();

			if (userId.HasValue)
			{
				User user = await this.UserManager.Get(userId.Value);

				claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
				claims.Add(new Claim(ClaimTypes.Name, user.UserName));

				foreach (UserProfileValue value in user.Profile)
				{
					claims.Add(new Claim(value.UserProfileProperty.TypeUri, value.Value ?? ""));
				}

				// A user can be in more than one role, so the role claim is set to a comma-separated list
				if (user.Roles != null && user.Roles.Any())
				{
					claims.Add(new Claim(ClaimTypes.Role, String.Join(',', user.Roles.Select(role => role.Name))));
				}
			}

			return claims;
		}

		private string DefaultLoginUri()
		{
			return this.Url.AreaAction("Index", "Account", "User");
		}
	}
}