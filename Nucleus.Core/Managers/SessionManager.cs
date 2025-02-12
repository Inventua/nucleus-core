﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Managers;

/// <summary>
/// Class used to access/manage user sessions.
/// </summary>
/// <remarks>
/// User sessions are not cached.  This allows us to immediately log a user out, if required.
/// </remarks>
public class SessionManager : ISessionManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }

  private IUserManager UserManager { get; }
  private IOptions<Authentication.AuthenticationOptions> Options { get; }

  public SessionManager(IDataProviderFactory dataProviderFactory, IUserManager userManager, ICacheManager cacheManager, IOptions<Authentication.AuthenticationOptions> options)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.UserManager = userManager;
    this.CacheManager = cacheManager;
    this.Options = options;
  }

  /// <summary>
  /// Create a new <see cref="UserSession"/> with default values.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// This function does not save the new <see cref="UserSession"/> to the database.  Call <see cref="Save(Site, UserSession)"/> to save the role group.
  /// </remarks>
  public Task<UserSession> CreateNew(Site site, User user, Boolean rememberMe, System.Net.IPAddress remoteIpAddress)
  {
    if (rememberMe)
    {
      return Task.FromResult(new UserSession(site, user, rememberMe, remoteIpAddress, DateTime.UtcNow.Add(this.Options.Value.LongExpiryTimeSpan), false));
    }
    else
    {
      return Task.FromResult(new UserSession(site, user, rememberMe, remoteIpAddress, DateTime.UtcNow.Add(this.Options.Value.ExpiryTimeSpan), !this.Options.Value.ExpiryTimeSpan.Equals(TimeSpan.Zero)));
    }
  }

  /// <summary>
  /// Retrieve an existing <see cref="UserSession"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<UserSession> Get(Guid id)
  {
    return await this.CacheManager.SessionCache().GetAsync(id, async id =>
    {
      using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
      {
        return await provider.GetUserSession(id);
      }
    });
  }

  /// <summary>
  /// Delete the specified <see cref="UserSession"/> from the database.
  /// </summary>
  /// <param name="userSession"></param>
  public async Task Delete(UserSession userSession)
  {
    using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
    {
      await provider.DeleteUserSession(userSession);
    }
    this.CacheManager.SessionCache().Remove(userSession.Id);
  }

  /// <summary>
  /// Create or update the specified <see cref="UserSession"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="userSession"></param>
  public async Task Save(UserSession userSession)
  {
    using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
    {
      await provider.SaveUserSession(userSession);
    }
  }

  public async Task DeleteExpiredSessions()
  {
    using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
    {
      await provider.DeleteExpiredSessions();
    }
  }

  public async Task SignIn(UserSession userSession, HttpContext httpContext, string returnUrl)
  {
    await Save(userSession);

    Site site;
    User user;

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      site = await provider.GetSite(userSession.SiteId);
    }

    using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
    {
      user = await provider.GetUser(userSession.UserId);
      if (user.Secrets == null)
      {
        user.Secrets = new();
      }
    }

    user.Secrets.LastLoginDate = DateTime.UtcNow;
    await this.UserManager.SaveSecrets(user);

    List<Claim> claims = new();

    claims.Add(new Claim(ClaimTypes.Name, user.UserName));
    claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.SESSION_ID_CLAIMTYPE, userSession.Id.ToString()));

    ClaimsIdentity claimsIdentity = new(claims, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME);

    await httpContext.SignInAsync
    (
      Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME,
      new ClaimsPrincipal(claimsIdentity),
      BuildAuthenticationProperties(userSession)
    );
  }

  public AuthenticationProperties BuildAuthenticationProperties(UserSession userSession)
  {
    return new()
    {
      AllowRefresh = true,
      IsPersistent = userSession.IsPersistent,
      IssuedUtc = DateTime.UtcNow,
      ExpiresUtc = userSession.IsPersistent ? userSession.ExpiryDate : null
    };
  }

  public async Task SignOut(HttpContext httpContext)
  {
    // The AuthenticationHandler manages deleting the session
    await httpContext.SignOutAsync(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME);
  }

  public async Task<long> CountUsersOnline(Site site)
  {
    using (ISessionDataProvider provider = this.DataProviderFactory.CreateProvider<ISessionDataProvider>())
    {
      return await provider.CountUsersOnline(site);
    }
  }
}
