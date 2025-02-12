﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers;

/// <summary>
/// Defines the interface for the ApiKey Manager class, which provides functions to manage database data for <see cref="Nucleus.Abstractions.Models.ApiKey"/>s.
/// </summary>
/// <remarks>
/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
/// </remarks>
/// <example>
/// public class MyClass
/// {
///		private IApiKeyManager ApiKeyManager { get; }
///		public MyClass(IUserManager userManager, IApiKeyManager apiKeyManager, Context context)
///		{
///			this.ApiKeyManager = apiKeyManager;
///		}
///	}
/// </example>
public interface IApiKeyManager
{
  /// <summary>
  /// Create a new <see cref="Nucleus.Abstractions.Models.ApiKey"/> with default values.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// This function does not save the new <see cref="Nucleus.Abstractions.Models.ApiKey"/> to the database.  Call <see cref="Save(ApiKey)"/> to save the ApiKey.
  /// </remarks>
  public Task<ApiKey> CreateNew();

  /// <summary>
  /// Retrieve an existing <see cref="Nucleus.Abstractions.Models.ApiKey"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public Task<ApiKey> Get(Guid id);

  /// <summary>
  /// List all <see cref="Nucleus.Abstractions.Models.ApiKey"/>s.
  /// </summary>
  /// <returns></returns>
  public Task<IEnumerable<ApiKey>> List();

  /// <summary>
  /// List paged <see cref="Nucleus.Abstractions.Models.ApiKey"/>s.
  /// </summary>
  /// <param name="pagingSettings"></param>
  /// <returns></returns>
  /// <remarks>
  /// This function is used by the API keys administration user interface.
  /// </remarks>
  public Task<Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey>> List(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

  /// <summary>
  /// Create or update the specified <see cref="Nucleus.Abstractions.Models.ApiKey"/>.
  /// </summary>
  /// <param name="key"></param>
  public Task Save(ApiKey key);

  /// <summary>
  /// Delete the specified <see cref="Nucleus.Abstractions.Models.ApiKey"/> from the database.
  /// </summary>
  /// <param name="key"></param>
  public Task Delete(ApiKey key);

}
