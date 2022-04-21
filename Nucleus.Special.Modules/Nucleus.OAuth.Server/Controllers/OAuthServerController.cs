using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Extensions.Authorization;
using Nucleus.ViewFeatures;
using System.Security.Claims;

namespace Nucleus.OAuth.Server.Controllers
{
	[Extension("OAuthServer")]
	public class OAuthServerController : Controller
	{
		private Context Context { get; }
		private ClientAppTokenManager ClientAppTokenManager { get; }
		private ClientAppManager ClientAppManager { get; }
		private IPageManager PageManager { get; }
		private IUserManager UserManager { get; }

		private const string TOKEN_TYPE = "bearer";

		public OAuthServerController(Context Context, ClientAppManager clientAppManager, ClientAppTokenManager clientAppTokenManager, IPageManager pageManager, IUserManager userManager) 
		{
			this.Context = Context;
			this.ClientAppTokenManager = clientAppTokenManager;
			this.ClientAppManager = clientAppManager;
			this.PageManager = pageManager;
			this.UserManager = userManager;
		}

		/// <summary>
		/// Start the authentication process.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// By convention, OAuth refers to this endpoint as "authorize" even though it's really "authenticate".
		/// </remarks>
		[HttpGet]
		[Route("/oauth2/authorize")]
		public async Task<ActionResult> Authorize(string response_type, string client_id, string redirect_uri, string scope, string state)
		{
			switch (response_type)
			{
				case "code":
				case "token":
					break;
				default:
					return ErrorRedirect(redirect_uri, state, "unsupported_response_type");
			}

			if (String.IsNullOrEmpty(client_id))
			{
				return BadRequest("Missing client_id");
			}

			if (String.IsNullOrEmpty(redirect_uri))
			{
				return BadRequest("Missing redirect_uri");
			}

			if (!Guid.TryParse(client_id, out Guid parsedClientId))
			{
				return BadRequest("invalid_client");
			}
			else
			{
				ClientApp clientApp = await this.ClientAppManager.GetByApiKey(parsedClientId);
				if (clientApp == null)
				{
					return BadRequest("invalid_client");
				}

				if (!clientApp.RedirectUri.Split("\r\n").Contains(redirect_uri))
				{
					return BadRequest($"Invalid redirect_uri '{redirect_uri}'.");
				}

				if (!clientApp.ApiKey.Scope.Split(' ').Contains(scope))
				{
					return ErrorRedirect(redirect_uri, state, "invalid_scope");
				}

				ClientAppToken token = this.ClientAppTokenManager.CreateNew();

				token.ClientApp = clientApp;
				token.Scope = scope;
				token.RedirectUri = redirect_uri;
				token.Type = response_type;
				token.State = state;

				await this.ClientAppTokenManager.Save(token);
				return await RedirectToLogin(token);
			}
		}


		/// <summary>
		/// Receive a redirect from the Nucleus login module, proces it and redirect back to the original caller (Oauth client).
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("/oauth2/respond/{id}")]
		public async Task<ActionResult> Respond(Guid id)
		{
			ClientAppToken token = await this.ClientAppTokenManager.Get(id);

			token.UserId = User.GetUserId();

			if (token.Type == "code")
			{
				// For token type "code" we send back a code, and *do not* set a refresh token.  Token type=token does not support
				// refresh tokens.
				token.Code = GenerateSecret(512);

				// We don't currently support refresh tokens
				//token.RefreshToken = GenerateSecret(512); 
			}

			token.AccessToken = GenerateSecret(512);
			token.ExpiryDate = DateTime.UtcNow.Add(TimeSpan.FromMinutes(token.ClientApp.TokenExpiryMinutes));
						
			await this.ClientAppTokenManager.Save(token);

			if (token.Type == "code")
			{
				return Redirect($"{token.RedirectUri}{(token.RedirectUri.Contains('?') ? "&" : "?")}code={token.Code}&state={token.State}");				
			}
			else if (token.Type == "grant")
			{
				return Redirect($"{token.RedirectUri}#access_token={token.AccessToken}&state={token.State}&token_type={TOKEN_TYPE}&scope={token.Scope}&expires_in={(token.ExpiryDate - DateTime.Now).TotalMinutes}");
			}
			else
			{
				return BadRequest();
			}
		}

		/// <summary>
		/// Return an access token corresponding to the specified code.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		[Route("/oauth2/token")]
		public async Task<ActionResult> Token(string grant_type, string code, string redirect_uri, string client_id)
		{
			if (grant_type != "authorization_code")
			{
				return ErrorRedirect(redirect_uri, "unsupported_grant_type");
			}

			if (!Guid.TryParse(client_id, out Guid parsedClientId))
			{
				return BadRequest("invalid_client");
			}
			else
			{
				ClientApp clientApp = await this.ClientAppManager.GetByApiKey(parsedClientId);
				if (clientApp == null)
				{
					return BadRequest("invalid_client");
				}

				ClientAppToken token = await this.ClientAppTokenManager.GetByCode(code);

				if (token == null)
				{
					return ErrorRedirect(redirect_uri, "invalid_authorization_code");
				}

				if (token.RedirectUri != redirect_uri)
				{
					return BadRequest("Invalid redirect_uri");
				}

				if (token.ClientApp.ApiKey.Id != parsedClientId)
				{
					return BadRequest("invalid_client");
				}

				if (!clientApp.ApiKey.Scope.Split(' ').Contains(token.Scope))
				{
					return ErrorRedirect(redirect_uri, "invalid_scope");
				}

				return Json(new 
				{
					access_token = token.AccessToken,
					token_type = "Bearer",
					expires_in = (token.ExpiryDate - DateTime.Now).TotalMinutes
					// we don't currently support refresh tokens
					//refresh_token = token.RefreshToken
				});
			}
		}


		[HttpGet]
		[Route("/oauth2/userinfo")]
		public async Task<ActionResult> UserInfo()
		{
			string[] authHeaderValues = this.ControllerContext.HttpContext.Request.Headers.Authorization;

			if (authHeaderValues.Length != 1 || !authHeaderValues[0].StartsWith(TOKEN_TYPE, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest("Invalid_request");
			}
			else
			{
				ClientAppToken token = await this.ClientAppTokenManager.GetByAccessToken(authHeaderValues[0].Substring(TOKEN_TYPE.Length).Trim());

				if (token == null)
				{
					return BadRequest("Invalid_token");
				}

				if (token.ExpiryDate < DateTime.UtcNow)
				{
					return Unauthorized("Expired_token");
				}

				Nucleus.Abstractions.Models.User user = await this.UserManager.Get(this.Context.Site, token.UserId.Value);

				if (user == null)
				{
					return BadRequest("Invalid_request");
				}

				System.Dynamic.ExpandoObject result = new();

				result.TryAdd(ClaimTypes.NameIdentifier, user.Id);
				result.TryAdd(ClaimTypes.Name, user.UserName);
				
				// A user can be in more than one role, so the role claim is set to an array
				if (user.Roles != null && user.Roles.Any())
				{
					result.TryAdd("roles", user.Roles.Select(role => role.Name));
					//result.TryAdd(ClaimTypes.Role, String.Join(",", user.Roles.Select(role => role.Name)));
				}

				foreach (UserProfileValue value in user.Profile)
				{
					result.TryAdd(value.UserProfileProperty.TypeUri, value.Value);
				}

				return Json(result);
			}
		}

		private async Task<RedirectResult> RedirectToLogin(ClientAppToken token)
		{
			if (token.ClientApp.LoginPage != null)
			{
				return Redirect($"{token.ClientApp.LoginPage.DefaultPageRoute}?returnUrl=/oauth2/respond/{token.Id}");
			}
			else
			{
				string uri;
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
						uri = this.DefaultLoginUri();
					}
					else
					{
						uri = loginPageRoute.Path;
					}
				}
				else
				{
					// use default login page
					uri = this.DefaultLoginUri();
				}

				return Redirect($"{uri}?returnUrl=/oauth2/respond/{token.Id}");
			}
		}

		private RedirectResult ErrorRedirect(string redirectUrl, string error)
		{
			return ErrorRedirect(redirectUrl, "", error);
		}

		private RedirectResult ErrorRedirect(string redirectUrl, string state, string error)
		{
			if (String.IsNullOrEmpty(state))
			{
				return Redirect($"{redirectUrl}{(redirectUrl.Contains("?") ? "&" : "?")}error={error}");
			}
			else
			{
				return Redirect($"{redirectUrl}{(redirectUrl.Contains("?") ? "&" : "?")}state={state}?error={error}");
			}
		}

		private string DefaultLoginUri()
		{
			return this.Url.AreaAction("Index", "Account", "User");
		}

		// From https://stackoverflow.com/questions/730268/unique-random-string-generation
		private string GenerateSecret(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
		{
			if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
			if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

			const int byteSize = 0x100;
			var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
			if (byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				var result = new System.Text.StringBuilder();
				var buf = new byte[128];
				while (result.Length < length)
				{
					rng.GetBytes(buf);
					for (var i = 0; i < buf.Length && result.Length < length; ++i)
					{
						// Divide the byte into allowedCharSet-sized groups. If the
						// random value falls into the last group and the last group is
						// too small to choose from the entire allowedCharSet, ignore
						// the value in order to avoid biasing the result.
						var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
						if (outOfRangeStart <= buf[i]) continue;
						result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
					}
				}
				return result.ToString();
			}
		}


	}
}