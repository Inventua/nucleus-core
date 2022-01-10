using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a site, which contains pages, users, roles, permissions and other data.
	/// </summary>
	/// <remarks>
	/// A Nucleus instance can host multiple sites.
	/// </remarks>
	public class Site : ModelBase
	{
		/// <summary>
		/// SiteSettings keys for mail server settings.
		/// </summary>
		public static class SiteMailSettingKeys
		{
			/// <summary>
			/// SMTP server host name
			/// </summary>
			public const string MAIL_HOSTNAME = "mail:hostname";
			/// <summary>
			/// SMTP server port name
			/// </summary>
			public const string MAIL_PORT = "mail:port";
			/// <summary>
			/// Use SSL to connect 
			/// </summary>
			public const string MAIL_USESSL = "mail:usessl";
			/// <summary>
			/// Default mail sender name
			/// </summary>
			public const string MAIL_SENDER = "mail:sender";
			/// <summary>
			/// SMTP server user name
			/// </summary>
			public const string MAIL_USERNAME = "mail:username";
			/// <summary>
			/// SMTP server password
			/// </summary>
			public const string MAIL_PASSWORD = "mail:password";
		}

		/// <summary>
		/// SiteSettings keys for mail template settings.
		/// </summary>
		public static class SiteTemplatesKeys
		{
			/// <summary>
			/// Email template for user welcome message.
			/// </summary>
			public const string MAILTEMPLATE_WELCOMENEWUSER = "mailtemplate:welcomenewuser";
			/// <summary>
			/// Email template for user password resets emails.
			/// </summary>
			public const string MAILTEMPLATE_PASSWORDRESET = "mailtemplate:passwordreset";

			/// <summary>
			/// Email template for user account reminders.
			/// </summary>
			public const string MAILTEMPLATE_ACCOUNTNAMEREMINDER = "mailtemplate:accountnamereminder";

		}

		/// <summary>
		/// SiteSettings keys for image settings.
		/// </summary>
		public static class SiteImageKeys
		{
			/// <summary>
			/// Site logo file
			/// </summary>
			public const string LOGO_FILEID = "logo:fileid";

			/// <summary>
			/// Site icon file
			/// </summary>
			public const string FAVICON_FILEID = "favicon:fileid";
		}

		/// <summary>
		/// SiteSettings keys for site pages settings.
		/// </summary>
		public static class SitePagesKeys
		{
			/// <summary>
			/// Site login page Id
			/// </summary>
			public const string SITEPAGE_LOGIN = "sitepage:login";

			/// <summary>
			/// Site user registration page Id
			/// </summary>
			public const string SITEPAGE_USERREGISTER = "sitepage:userregister";

			/// <summary>
			/// Site user profile Id
			/// </summary>
			public const string SITEPAGE_USERPROFILE = "sitepage:userprofile";

			/// <summary>
			/// Site change passwordpage Id
			/// </summary>
			public const string SITEPAGE_USERCHANGEPASSWORD = "sitepage:userchangepassword";

			/// <summary>
			/// Site terms page Id
			/// </summary>
			public const string SITEPAGE_TERMS = "sitepage:terms";

			/// <summary>
			/// Site privacy page Id
			/// </summary>
			public const string SITEPAGE_PRIVACY = "sitepage:privacy";

			/// <summary>
			/// Site error page Id
			/// </summary>
			public const string SITEPAGE_ERROR = "sitepage:error";

			/// <summary>
			/// Site "not found" (404) page Id
			/// </summary>
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
		/// Gets or sets the default layout for the site, or NULL to use the default.
		/// </summary>
		/// <remarks>
		/// Pages which do not specify a layout use the site's default layout.
		/// </remarks>
		public LayoutDefinition DefaultLayoutDefinition { get; set; }

		/// <summary>
		/// Gets or sets the default container for the site, or NULL to use the default.
		/// </summary>
		/// <remarks>
		/// Modules which do not specify a container use the site's default container.
		/// </remarks>
		public ContainerDefinition DefaultContainerDefinition { get; set; }

		/// <summary>
		/// Gets or sets the Default alias for the site
		/// </summary>
		public SiteAlias DefaultSiteAlias { get; set; }

		/// <summary>
		/// Gets or sets the site's home directory.
		/// </summary>
		/// <remarks>
		/// Used by the LocalFileSystemProvider.
		/// </remarks>
		public string HomeDirectory { get; set; }

		/// <summary>
		/// List of <see cref="SiteAlias"/>es for the site.
		/// </summary>
		public List<SiteAlias> Aliases { get; set; }

		/// <summary>
		/// List of user profile properties for users of the site.
		/// </summary>
		public List<UserProfileProperty> UserProfileProperties { get; set; }

		/// <summary>
		/// Name/value pairs of site settings.
		/// </summary>
		public List<SiteSetting> SiteSettings { get; set; } = new();

	}


}
