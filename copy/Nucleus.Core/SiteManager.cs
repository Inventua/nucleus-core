using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Site"/>s and <see cref="SiteAlias"/>es.
	/// </summary>
	public class SiteManager
	{
		
		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }

		public SiteManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="Site"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Site"/> to the database.  Call <see cref="Save(Site, Site)"/> to save the role group.
		/// </remarks>
		public Site CreateNew()
		{
			Site result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Site"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Site Get(Guid id)
		{
			return this.CacheManager.SiteCache.Get(id, id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return provider.GetSite(id);
				}
			});
		}


		/// <summary>
		/// Retrieve an existing <see cref="Site"/> from the database which has a <see cref="SiteAlias"/> which matches the specified 
		/// requestUri and pathBase.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Site Get(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase)
		{
			string siteDetectCacheKey = (requestUri + "^" + pathBase).ToLower();

			Guid? siteId = this.CacheManager.SiteDetectCache.Get(siteDetectCacheKey, id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return provider.DetectSite(requestUri, pathBase);
				}
			});

			return siteId.HasValue ? this.Get(siteId.Value) : null;			
		}

		/// <summary>
		/// Retrieve the existing <see cref="Site"/> from the database which contains the specified page.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Site Get(Page page)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				Guid siteId = provider.GetPageSiteId(page);
				if (siteId==Guid.Empty)
				{
					return null;
				}
				else
				{
					return Get(siteId);
				}
			}
		}

		/// <summary>
		/// Delete the specified <see cref="Site"/> from the database.
		/// </summary>
		/// <param name="site"></param>
		public void Delete(Site site)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteSite(site);
				this.CacheManager.SiteCache.Remove(site.Id);
			}
		}

		/// <summary>
		/// Get the specified <see cref="SiteAlias"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public SiteAlias GetAlias(Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.GetSiteAlias(id);
			}
		}

		/// <summary>
		/// Returns an existing <see cref="UserProfileProperty"/>
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public UserProfileProperty GetUserProfileProperty(Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.GetUserProfileProperty(id);
			}
		}

		/// <summary>
		/// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the next-highest <see cref="UserProfileProperty.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public void MoveUserProfilePropertyDown(Site site, Guid id)
		{
			UserProfileProperty previousProp = null;
			UserProfileProperty thisProp;
		
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisProp = this.GetUserProfileProperty(id);
			
				List<UserProfileProperty> properties = provider.ListSiteUserProfileProperties(site.Id).ToList();

				properties.Reverse();
				foreach (UserProfileProperty prop in properties)
				{
					if (prop.Id == id)
					{
						if (previousProp != null)
						{
							long temp = prop.SortOrder;
							prop.SortOrder = previousProp.SortOrder;
							previousProp.SortOrder = temp;

							provider.SaveUserProfileProperty(site.Id, previousProp);
							provider.SaveUserProfileProperty(site.Id, prop);

							site.UserProfileProperties = provider.ListSiteUserProfileProperties(site.Id);

							// Properties are cached as part of their site, so we have invalidate the cache for the site
							this.CacheManager.SiteCache.Remove(site.Id);

							// User properties are cached as part of user proerty values, so we have to invalidate the cache
							// for ALL users when a site's user properties change
							this.CacheManager.UserCache.Clear();

							break;
						}
					}
					else
					{
						previousProp = prop;
					}
				}
			}
		}

		/// <summary>
		/// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the previous <see cref="UserProfileProperty.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public void MoveUserProfilePropertyUp(Site site, Guid id)
		{
			UserProfileProperty previousProp = null;
			UserProfileProperty thisProp;

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				thisProp = provider.GetUserProfileProperty(id);

				List<UserProfileProperty> properties = provider.ListSiteUserProfileProperties(site.Id).ToList();

				foreach (UserProfileProperty prop in provider.ListSiteUserProfileProperties(site.Id))
				{
					if (prop.Id == id)
					{
						if (previousProp != null)
						{
							long temp = prop.SortOrder;
							prop.SortOrder = previousProp.SortOrder;
							previousProp.SortOrder = temp;

							provider.SaveUserProfileProperty(site.Id, previousProp);
							provider.SaveUserProfileProperty(site.Id, prop);

							site.UserProfileProperties = provider.ListSiteUserProfileProperties(site.Id);

							// Properties are cached as part of their site, so we have invalidate the cache for the site
							this.CacheManager.SiteCache.Remove(site.Id);

							// User properties are cached as part of user proerty values, so we have to invalidate the cache
							// for ALL users when a site's user properties change
							this.CacheManager.UserCache.Clear();

							break;
						}
					}
					else
					{
						previousProp = prop;
					}
				}
			}
		}

		/// <summary>
		/// List all <see cref="Site"/>s.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Site> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListSites();
			}
		}

		/// <summary>
		/// List all <see cref="SiteAlias"/>es for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<SiteAlias> ListAliases(Site site)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListSiteAliases(site.Id);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		public void Save(Site site)
		{
			if (System.IO.Path.IsPathRooted(site.HomeDirectory))
			{
				throw new ArgumentException($"'{site.HomeDirectory}' is not a valid home directory.  The home directory must not start with a backslash or a drive letter.");
			}

			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveSite(site);
				this.CacheManager.SiteCache.Remove(site.Id);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="SiteAlias"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="siteAlias"></param>
		public void SaveAlias(Site site, SiteAlias siteAlias)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveSiteAlias(site.Id, siteAlias);
				this.CacheManager.SiteCache.Remove(site.Id);
			}
		}


		/// <summary>
		/// Delete the specified <see cref="SiteAlias"/> from the database.
		/// </summary>
		/// <param name="siteAlias"></param>
		public void DeleteAlias(Site site, Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteSiteAlias(id);
				this.CacheManager.SiteCache.Remove(site.Id);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="UserProfileProperty"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="property"></param>
		public void SaveUserProfileProperty(Site site, UserProfileProperty property)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveUserProfileProperty(site.Id, property);
				this.CacheManager.SiteCache.Remove(site.Id);

				// User properties are cached as part of user proerty values, so we have to invalidate the cache
				// for ALL users when a site's user properties change
				this.CacheManager.UserCache.Clear();
			}
		}

		/// <summary>
		/// Delete the specified <see cref="UserProfileProperty"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public void DeleteUserProfileProperty(Site site, Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteUserProfileProperty(id);
				this.CacheManager.SiteCache.Remove(site.Id);
			}
		}

	}
}
