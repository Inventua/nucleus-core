using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represent a site, which contains pages, users, roles, permissions and other data.
	/// </summary>
	/// <remarks>
	/// A Nucleus instance can host multiple sites.
	/// </remarks>
	public class Site
	{
		public static class SiteMailSettingKeys
		{
			public const string MAIL_HOSTNAME = "mail:hostname";
			public const string MAIL_PORT = "mail:port";
			public const string MAIL_USESSL = "mail:usessl";
			public const string MAIL_SENDER = "mail:sender";
			public const string MAIL_USERNAME = "mail:username";
			public const string MAIL_PASSWORD = "mail:password";
		}

		public static class SiteTemplatesKeys
		{
			public const string MAILTEMPLATE_WELCOMENEWUSER = "mailtemplate:welcomenewuser";
			public const string MAILTEMPLATE_PASSWORDRESET = "mailtemplate:passwordreset";
			public const string MAILTEMPLATE_ACCOUNTNAMEREMINDER = "mailtemplate:accountnamereminder";
			
		}

		public static class SiteImageKeys
		{
			public const string LOGO_FILEID = "logo:fileid";
			public const string FAVICON_FILEID = "favicon:fileid";			
		}

		public static class SitePagesKeys
		{
			public const string SITEPAGE_LOGIN = "sitepage:login";
			public const string SITEPAGE_USERREGISTER = "sitepage:userregister";
			public const string SITEPAGE_USERPROFILE = "sitepage:userprofile";
			public const string SITEPAGE_TERMS = "sitepage:terms";
			public const string SITEPAGE_PRIVACY = "sitepage:privacy";
			public const string SITEPAGE_ERROR = "sitepage:error";
			public const string SITEPAGE_NOTFOUND = "sitepage:notfound";

		}

		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the Site Name
		/// </summary>
		/// <remarks>
		/// Site names are displayed in the administrative interface.
		/// </remarks>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the Administrators role.
		/// </summary>
		/// <remarks>
		/// Users in the administrators role can manage pages, roles, permissions and other site-specific settings.
		/// </remarks>
		public Role AdministratorsRole { get; set; }

		/// <summary>
		/// Gets or sets the Registered users role.
		/// </summary>
		/// <remarks>
		/// New pages and page modules automatically default to allowing view access to users in the Registered Users role.  New
		/// users automatically default to being a member of the Registered Users role.
		/// </remarks>
		public Role RegisteredUsersRole { get; set; }

		/// <summary>
		/// Gets or sets the All users role.
		/// </summary>
		/// <remarks>
		/// This role represents all users whether they are logged in or not.
		/// </remarks>
		public Role AllUsersRole { get; set; }

		/// <summary>
		/// Gets or sets the Anonymous users role.
		/// </summary>
		/// <remarks>
		/// This role represents users who are not logged on.
		/// </remarks>
		public Role AnonymousUsersRole { get; set; }

		/// <summary>
		/// Gets or sets the Site Group Id
		/// </summary>
		/// <remarks>
		/// Represents the site group that a site belongs to.  This field is for future use.
		/// </remarks>		
		public Guid? SiteGroupId { get; set; }

		/// <summary>
		/// Gets or sets the Default layout for the site, or NULL to use the default.
		/// </summary>
		/// <remarks>
		/// Pages which do not specify a layout use the site's default layout.
		/// </remarks>
		public LayoutDefinition DefaultLayout { get; set; }

		/// <summary>
		/// Gets or sets the site's home directory.
		/// </summary>
		/// <remarks>
		/// Used by the LocalFileSystemProvider.
		/// </remarks>
		public string HomeDirectory { get; set; }

		/// <summary>
		/// Gets or sets the default <see cref="SiteAlias"/> for the site.
		/// </summary>
		public Guid DefaultAliasId { get; set; }

		/// <summary>
		/// List of <see cref="SiteAlias"/>es for the site.
		/// </summary>
		public IEnumerable<SiteAlias> Aliases { get; set; }

		/// <summary>
		/// List of user profile properties for users of the site.
		/// </summary>
		public IEnumerable<UserProfileProperty> UserProfileProperties { get; set; }

		/// <summary>
		/// Name/value pairs of site settings.
		/// </summary>
		public Dictionary<string, string> SiteSettings { get; set; } = new();
	}

	
}
