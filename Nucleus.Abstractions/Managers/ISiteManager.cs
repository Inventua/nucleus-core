using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Site"/>s and <see cref="SiteAlias"/>es.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface ISiteManager
	{
		/// <summary>
		/// Create a new <see cref="Site"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Site"/> to the database.  Call <see cref="Save(Site)"/> to save the role group.
		/// </remarks>
		public Task<Site> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="Site"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Site> Get(Guid id);


		/// <summary>
		/// Retrieve an existing <see cref="Site"/> from the database which has a <see cref="SiteAlias"/> which matches the specified 
		/// requestUri and pathBase.
		/// </summary>
		/// <param name="requestUri"></param>
		/// <param name="pathBase"></param>
		/// <returns></returns>
		public Task<Site> Get(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase);

		/// <summary>
		/// Retrieve the existing <see cref="Site"/> from the database which contains the specified page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public Task<Site> Get(Page page);

		/// <summary>
		/// Delete the specified <see cref="Site"/> from the database.
		/// </summary>
		/// <param name="site"></param>
		public Task Delete(Site site);

		/// <summary>
		/// Get the specified <see cref="SiteAlias"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<SiteAlias> GetAlias(Guid id);

		/// <summary>
		/// Returns an existing <see cref="UserProfileProperty"/>
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<UserProfileProperty> GetUserProfileProperty(Guid id);

		/// <summary>
		/// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the next-highest <see cref="UserProfileProperty.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public Task MoveUserProfilePropertyDown(Site site, Guid id);

		/// <summary>
		/// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the previous <see cref="UserProfileProperty.SortOrder"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public Task MoveUserProfilePropertyUp(Site site, Guid id);

		/// <summary>
		/// List all <see cref="Site"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<IList<Site>> List();

		/// <summary>
		/// List all <see cref="Site"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<long> Count();

		/// <summary>
		/// Create or update the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		public Task Save(Site site);

		/// <summary>
		/// Create or update the specified <see cref="SiteAlias"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="siteAlias"></param>
		public Task SaveAlias(Site site, SiteAlias siteAlias);

		/// <summary>
		/// Create or update the specified <see cref="UserProfileProperty"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="property"></param>
		public Task SaveUserProfileProperty(Site site, UserProfileProperty property);

		/// <summary>
		/// Delete the specified <see cref="UserProfileProperty"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public Task DeleteUserProfileProperty(Site site, Guid id);

		/// <summary>
		/// Delete the specified <see cref="SiteAlias"/> from the database.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		public Task DeleteAlias(Site site, Guid id);

		/// <summary>
		/// Export the site as XML
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<System.IO.MemoryStream> Export(Site site);

		/// <summary>
		/// Parse a site template file from a stream and return the deserialized result
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Export.SiteTemplate> ParseTemplate(System.IO.Stream stream);

		/// <summary>
		/// Save a parsed template to a temporary file
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		public Task<string> SaveTemplateTempFile(Nucleus.Abstractions.Models.Export.SiteTemplate template);
		
		/// <summary>
		/// Read a template temp file and return a parsed site template.
		/// </summary>
		/// <param name="templateTempFileName"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Export.SiteTemplate> ReadTemplateTempFile(string templateTempFileName);

		/// <summary>
		/// Import a site template
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		public Task<Site> Import(Models.Export.SiteTemplate template);


	}
}
