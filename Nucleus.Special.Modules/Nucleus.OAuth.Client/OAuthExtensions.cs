using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Nucleus.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;

namespace Nucleus.OAuth.Client
{
	internal static class OAuthExtensions
	{
		private static string[] ReservedProperties = GetReservedProperties();

		private static string[] GetReservedProperties()
		{
			System.Reflection.PropertyInfo[] properties = typeof(Models.Configuration.OAuthProvider).GetProperties();

			return properties.Select(prop => prop.Name).ToArray();

		}

		/// <summary>
		/// Add Nucleus core Authentication to DI.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		/// <remarks>
		/// Configures and adds Nucleus remote (Oauth) authentication to DI, including the <see cref="AuthenticationOptions"/> class.
		/// </remarks>
		public static IServiceCollection AddRemoteAuthentication(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<RemoteAuthenticationOptions>(configuration.GetSection(RemoteAuthenticationOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);
			
			services.AddAuthentication(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME)
				.AddScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME, options =>
				{
					
				});

			return services;
		}

		public static void AddOAuth(this AuthenticationBuilder builder, IConfiguration configuration)
		{			
			AddRemoteAuthentication(builder.Services, configuration);
				
			// Look for configured OAuth providers

			// Get config for immediate use
			Models.Configuration.OAuthProviders config = new();
			configuration.Bind(Models.Configuration.OAuthProviders.Section, config);

			// Add config to dependency injection for later use
			builder.Services.Configure<Models.Configuration.OAuthProviders>(configuration.GetSection(Models.Configuration.OAuthProviders.Section), options => options.BindNonPublicProperties = true);

			IConfigurationSection[] configurationSections = configuration.GetSection(Models.Configuration.OAuthProviders.Section).GetChildren().ToArray();

			for (int count = 0; count < config.Count; count++)
			{
				IConfigurationSection configurationSection = configurationSections[count];
				Models.Configuration.OAuthProvider providerConfig = config[count];
				string providerName = providerConfig.Name ?? providerConfig.Type; 
				string providerType = providerConfig.Type;
        string providerDisplayName = providerConfig.FriendlyName ?? providerName;

        switch (providerType)
				{
					case "OAuth":
						builder.AddOAuth(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
							options.Events.OnCreatingTicket = ReadUserData;
						});
						break;

					case "OpenIdConnect":
						builder.AddOpenIdConnect(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
						});
						break;

					case "Google":
						builder.AddGoogle(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
							options.Events.OnCreatingTicket = ReadUserData;
						});
						break;

					case "Facebook":
						builder.AddFacebook(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
							options.Scope.Add("public_profile");
							options.Events.OnCreatingTicket = ReadUserData;
						});
						break;

					case "Twitter":
						builder.AddTwitter(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
						});
						break;

					case "Microsoft":
						builder.AddMicrosoftAccount(providerName, providerDisplayName, options =>
						{
							SetOptions(options, providerName, configurationSection, providerConfig);
							options.Events.OnCreatingTicket = ReadUserData;
						});
						break;

					default:
						throw new InvalidOperationException($"OAuth provider {providerName} not recognized.");
				}
			}
		}

		private static async Task ReadUserData(Microsoft.AspNetCore.Authentication.OAuth.OAuthCreatingTicketContext ctx)
		{
			// If a JWT (id_token) is returned along with the OAuth2 access_token response, parse it.  Otherwise, call the
			// UserInformationEndpoint to retrieve user data.

			ILogger<Controllers.OAuthClientController> logger = ctx.HttpContext.RequestServices.GetService<ILogger<Controllers.OAuthClientController>>();

			// if the response contains an id_token (JWT token), read it and copy any claims that are 
			if (ctx.TokenResponse.Response.RootElement.TryGetProperty("id_token", out System.Text.Json.JsonElement value))
			{
				logger?.LogTrace("The OAUTH token contains a id_token property (JWT token).");

				JwtSecurityTokenHandler handler = new();

				// Make the JwtSecurityTokenHandler use the claim types we give it instead of changing them to different types
				handler.InboundClaimTypeMap.Clear();

				JwtSecurityToken token = handler.ReadJwtToken(value.ToString());
				
				// Look for claim actions (set in config/code with MapJsonType) in the JWT payload (token claims).  If found, add claims
				// to the identity with the claim types specified.  Normally claim actions are populated (automatically) by a call to
				// options.UserInformationEndpoint, and .net core doesn't seem to pay any attention to JWT tokens (hence this code is required).
				foreach (Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction action in ctx.Options.ClaimActions)
				{
					// The JWT token can contain multiple claims with the same claim type (roles, for example), so we must loop through them.
					IEnumerable<System.Security.Claims.Claim> claims = token.Claims
						.Where(claim => claim.Type.Equals((action as Microsoft.AspNetCore.Authentication.OAuth.Claims.JsonKeyClaimAction)?.JsonKey, StringComparison.OrdinalIgnoreCase));

					if (claims.Any())
					{
						foreach (System.Security.Claims.Claim claim in claims)
						{
							logger?.LogTrace("Adding claim {claimtype}: '{value}' from the JWT {inputClaimType} property.", action.ClaimType, claim.Value, claim.Type);
							ctx.Identity.AddClaim(new(action.ClaimType, claim.Value, claim.ValueType, claim.Issuer, claim.Issuer));
						}
					}
					else
					{
						logger?.LogTrace("No mapping was found for the JWT token {inputClaimType} property.", (action as Microsoft.AspNetCore.Authentication.OAuth.Claims.JsonKeyClaimAction)?.JsonKey);
					}
				}
			}
			else
			{
				logger?.LogTrace("The OAUTH token does not contain an id_token property (JWT token), calling UserInformationEndpoint '{url}' using Access Token '{token}'.", ctx.Options.UserInformationEndpoint, ctx.AccessToken);

				// Get user info from the UserInformationEndpoint and parse it.
				var request = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);

				var response = await ctx.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted);

				if (response.IsSuccessStatusCode)
				{
					logger?.LogTrace("The request to UserInformationEndpoint '{url}' was successful, reading claims.", ctx.Options.UserInformationEndpoint);

					System.Text.Json.JsonDocument user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
					ctx.RunClaimActions(user.RootElement);
				}
				else
				{
					logger?.LogTrace("The request to UserInformationEndpoint '{url}' failed with status {statuscode}:{reason}.", ctx.Options.UserInformationEndpoint, response.StatusCode, response.ReasonPhrase);
				}
			}
		}

		private static void SetOptions(Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions options, string providerName, IConfigurationSection configurationSection, Models.Configuration.OAuthProvider providerConfig)
		{
			options.SignInScheme = RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME;
			options.CallbackPath = GetCallbackPath(providerName);
			
			foreach (IConfigurationSection configValue in configurationSection.GetChildren())
			{
				if (!ReservedProperties.Contains(configValue.Key, StringComparer.OrdinalIgnoreCase))
				{ 
					// use reflection to set properties from config (if present).  Throw an exception if config contains any unrecognized values.
					var prop = options.GetType().GetProperty(configValue.Key);
					if (prop != null)
					{
						prop.SetValue(options, Convert.ChangeType(configValue.Value, prop.PropertyType));
					}
					else
					{
						throw new InvalidOperationException($"OAuth provider value '{configValue.Key}' for configured provider '{providerName}' not recognized [{configValue.Path}].");
					}
				}
			}

			// if the options class inherits OAuthOptions, add scopes and json key mappings from config
			Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions oauthOptions = options as Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions;

			if (oauthOptions != null)
			{
				foreach (string scope in providerConfig.Scope)
				{
					oauthOptions.Scope.Add(scope);
				}

				foreach (Models.Configuration.MapJsonKey key in providerConfig.MapJsonKeys)
				{
					oauthOptions.ClaimActions.MapJsonKey(key.ClaimType, key.JsonKey ?? key.ClaimType);
				}
			}
			options.Events.OnTicketReceived = ((TicketReceivedContext ctx) =>
			{
				return Task.CompletedTask;
			});

			//options.Events.OnRemoteFailure = ((RemoteFailureContext ctx) => 
			//{ 
			//	return Task.CompletedTask;
			//});

			//options.Events.OnAccessDenied = ((AccessDeniedContext ctx) => 
			//{

			//	return Task.CompletedTask;
			//});

		}

		private static string GetCallbackPath(string provider)
		{
			return $"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/oauthclient/callback/{provider.ToLower()}";
		}
	}
}
