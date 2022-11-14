using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nucleus.Data.Common;
using Nucleus.Abstractions.Models.Configuration;
using System.Text.RegularExpressions;

namespace Nucleus.Data.EntityFramework
{ 
	/// <summary>
	/// Database providers which use Entity Framework implement this interface in order to configure their Entity Framework DbContext using  
	/// Nucleus configuration data.
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	/// <remarks>
	/// Register your implementation of this class in the dependency injection service container, Nucleus will use it automatically. 
	/// </remarks>
	/// <example>
	/// <![CDATA[services.AddSingleton<Nucleus.Data.EntityFramework.IDbContextConfigurator<LinksDataProvider>, Nucleus.Data.Sqlite.SqliteOptionsConfigurator<LinksDataProvider>>();]]>
	/// </example>
	public abstract class DbContextConfigurator<TDataProvider> : DbContextConfigurator where TDataProvider : Nucleus.Data.Common.DataProvider
	{

		/// <summary>
		/// Create a new instance of this class
		/// </summary>
		/// <param name="databaseOptions"></param>
		public DbContextConfigurator(IOptions<DatabaseOptions> databaseOptions)
		{
			this.DatabaseConnectionOption = databaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());
		}
	}

	/// <summary>
	/// This is the non-generic form of IDbContextConfigurator, used by <see cref="DbContext"/>.
	/// </summary>
	/// <remarks>
	/// Implementations of IDbContextConfigurator should use the generic form of <see cref="DbContextConfigurator{T}"/>.
	/// </remarks>
	public abstract class DbContextConfigurator
	{
		/// <summary>
		/// Dictionary of constraint names/messages for ParseException method.
		/// </summary>
		private static Dictionary<string, string> DB_MESSAGES = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "IX_SITEGROUPS_PRIMARYSITEID", DbContextConfigurator.Messages.IX_SITEGROUPS_PRIMARYSITEID_MESSAGE},
			{ "IX_SITEGROUPS_NAME", DbContextConfigurator.Messages.IX_SITEGROUPS_NAME_MESSAGE},
			{ "IX_SITES_NAME" , DbContextConfigurator.Messages.IX_SITES_NAME_MESSAGE},
			{ "IX_SITEALIAS_ALIAS" , DbContextConfigurator.Messages.IX_SITEALIAS_ALIAS_MESSAGE},
			{ "IX_PAGES_NAME" , DbContextConfigurator.Messages.IX_PAGES_NAME_MESSAGE},
			{ "IX_PAGEROUTES_PATH" , DbContextConfigurator.Messages.IX_PAGEROUTES_PATH_MESSAGE},
			{ "IX_USERS_SITEUSER" , DbContextConfigurator.Messages.IX_USERS_SITEUSER_MESSAGE},
			{ "IX_ROLEGROUPS_NAME" , DbContextConfigurator.Messages.IX_ROLEGROUPS_NAME_MESSAGE},
			{ "IX_ROLES_NAME" , DbContextConfigurator.Messages.IX_ROLES_NAME_MESSAGE},
			{ "IX_USERPROFILEPROPERTIES_SITEID_TYPEURI" , DbContextConfigurator.Messages.IX_USERPROFILEPROPERTIES_SITEID_TYPEURI_MESSAGE},
			{ "IX_USERPROFILEPROPERTIES_NAME" , DbContextConfigurator.Messages.IX_USERPROFILEPROPERTIES_NAME_MESSAGE},
			{ "IX_MAILTEMPLATES_SITEID_NAME" , DbContextConfigurator.Messages.IX_MAILTEMPLATES_SITEID_NAME_MESSAGE},
			{ "IX_LISTS_NAME" , DbContextConfigurator.Messages.IX_LISTS_NAME_MESSAGE},
			{ "IX_LISTITEMS_NAME" , DbContextConfigurator.Messages.IX_LISTITEMS_NAME_MESSAGE},
			{ "IX_APIKEYS_NAME", DbContextConfigurator.Messages.IX_APIKEYS_NAME_MESSAGE}
		};

		/// <summary>
		/// Configure entity-framework with the specified options.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <remarks>
		/// Entity-framework base database providers implement this method to call the initialization methods which are appropriate to 
		/// their specific implementation.
		/// </remarks>
		public abstract Boolean Configure(DbContextOptionsBuilder options);

		/// <summary>
		/// Perform (or retry) pre-configuration checks.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Implementation of this method is optional, database providers often don't need to execute any checks.  An example of a pre-configuration 
		/// check is the SQLite database provider, which checks that the folder which will store the database exists, and creates it if it does not.
		/// </remarks>
		public virtual Boolean PreConfigure() { return true; }

		/// <summary>
		/// Gets the database connection option for this DbContextConfigurator.
		/// </summary>
		public Nucleus.Abstractions.Models.Configuration.DatabaseConnectionOption DatabaseConnectionOption { get; protected set; }

		/// <summary>
		/// Parse a database exception and if recognized, throw an exception with a more friendly message.
		/// </summary>
		public abstract void ParseException(DbUpdateException exception);

		/// <summary>
		/// Gets the tokens and values which were matched in the most recent call to ParseException.
		/// </summary>
		protected Dictionary<string, string> ExceptionMessageTokens { get; private set; }

		/// <summary>
		/// Match an original exception message to a pattern and generate an improved error message.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="pattern"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		protected string ParseException(Exception exception, string pattern, string message)
		{
			Match match = Regex.Match(exception.Message, pattern, RegexOptions.IgnoreCase);
			if (match.Success)
			{
				this.ExceptionMessageTokens = new(StringComparer.OrdinalIgnoreCase);
				List<string> keys = match.Groups.Keys.ToList();
				List<Group> values = match.Groups.Values.ToList();

				for (int count=0; count < keys.Count; count++)
				{
					this.ExceptionMessageTokens.Add(keys[count], values[count].ToString());

					if (keys[count] == "constraint_name")
					{
						// Contraint names are a special case.  If the database being used is able to return the constraint name, we can match
						// the constraint name to a friendly message.
						string constraintMessage = ConstraintMessage(values[count].ToString());
						if (!String.IsNullOrEmpty(constraintMessage))
						{
							return constraintMessage;
						}
					}
				}

				return Regex.Replace(message, "{(.*?)}", new MatchEvaluator(new MessageParser(match.Groups).MatchEvaluator));
			}
			else
			{
				return "";
			}
		}

		private string ConstraintMessage(string name)
		{
			return (DB_MESSAGES.ContainsKey(name) ? DB_MESSAGES[name] : "");
			
			////switch (name)
			////{
			////	// Unique constraints.  We only provide "friendly" messages for the ones that can happen due to user input.
			////	case "IX_ScheduledTasks_Name":
			////		return Messages.IX_SCHEDULEDTASKS_NAME_MESSAGE;
			////		// "The name that you entered is already used by an existing Scheduled Task.  Please enter a unique name.";
			////	case "IX_SiteGroups_PrimarySiteId":
			////		return Messages.IX_SITEGROUPS_PRIMARYSITEID_MESSAGE;
			////		//return "Another site group is already assigned to the primary site that you have selected.";
			////	case "IX_SiteGroups_Name":
			////		return Messages.IX_SITEGROUPS_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing site group.  Please enter a unique name.";
			////	case "IX_Sites_Name":
			////		return Messages.IX_SITES_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing site.  Please enter a unique name.";
			////	case "IX_SiteAlias_Alias":
			////		return Messages.IX_SITEALIAS_ALIAS_MESSAGE;
			////	//return "Another site is already using the alias that you entered.";
			////	case "IX_Pages_Name":
			////		return Messages.IX_PAGES_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing page.  Please enter a unique name.";
			////	case "IX_PageRoutes_Path":
			////		return Messages.IX_PAGEROUTES_PATH_MESSAGE;
			////	//return "Another page is already using the page route that you entered.";
			////	case "IX_Users_SiteUser":
			////		return Messages.IX_USERS_SITEUSER_MESSAGE;
			////	//return "The name that you entered is already used by an existing user.  Please enter a unique name.";
			////	case "IX_RoleGroups_Name":
			////		return Messages.IX_ROLEGROUPS_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing role group.  Please enter a unique name.";
			////	case "IX_Roles_Name":
			////		return Messages.IX_ROLES_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing role.  Please enter a unique name.";
			////	case "IX_UserProfileProperties_SiteId_TypeUri":
			////		return Messages.IX_USERPROFILEPROPERTIES_SITEID_TYPEURI_MESSAGE;
			////	//return "The Type Uri that you entered has already been used.";
			////	case "IX_UserProfileProperties_Name":
			////		return Messages.IX_USERPROFILEPROPERTIES_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing user profile property.  Please enter a unique name.";
			////	case "IX_MailTemplates_SiteId_Name":
			////		return Messages.IX_MAILTEMPLATES_SITEID_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing mail template.  Please enter a unique name.";
			////	case "IX_Lists_Name":
			////		return Messages.IX_LISTS_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing list.  Please enter a unique name.";
			////	case "IX_ListItems_Name":
			////		return Messages.IX_LISTITEMS_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing list item.  Please enter a unique name.";
			////	case "IX_ApiKeys_Name":
			////		return Messages.IX_APIKEYS_NAME_MESSAGE;
			////	//return "The name that you entered is already used by an existing Api key.  Please enter a unique name.";

			////	// Foreign key constraints.  Most of these will never happen unless there is a bug in the UI/Controllers.
			////	case "FK_SiteGroups_PrimarySiteId":
			////		return Messages.FK_SITEGROUPS_PRIMARYSITEID_MESSAGE;
			////	//return "Primary site is invalid.";				
			////	case "FK_Sites_AdministratorsRoleId":
			////		return Messages.FK_SITES_ADMINISTRATORSROLEID_MESSAGE;
			////	//return "Administrator role is invalid.";
			////	case "FK_Sites_AllUsersRoleId":
			////		return Messages.FK_SITES_ALLUSERSROLEID_MESSAGE;
			////	//return "All users role is invalid";
			////	case "FK_Sites_AnonymousUsersRoleId":
			////		return Messages.FK_SITES_ANONYMOUSUSERSROLEID_MESSAGE;
			////	//return "Anonymous users role is invalid";
			////	case "FK_Sites_RegisteredUsersRoleId":
			////		return Messages.FK_SITES_REGISTEREDUSERSROLEID_MESSAGE;
			////	//return "Registered users role is invalid";
			////	case "FK_Pages_SiteId":
			////		return Messages.FK_PAGES_SITEID_MESSAGE;
			////	//return "Site is invalid.";
			////	case "FK_Pages_ParentId":
			////		return Messages.FK_PAGES_PARENTID_MESSAGE;
			////	//return "Parent page is invalid.";
			////	case "FK_Roles_RoleGroupId":
			////		return Messages.FK_ROLES_ROLEGROUPID_MESSAGE;
			////	//return "Role group is invalid.";

			////	default:
			////		return "";
			////}
		}

		/// <summary>
		/// Error message constants
		/// </summary>
		public class Messages
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public const string IX_SCHEDULEDTASKS_NAME_MESSAGE = "The name that you entered is already used by an existing Scheduled Task. Please enter a unique name.";

			public const string IX_SITEGROUPS_PRIMARYSITEID_MESSAGE = "Another site group is already assigned to the primary site that you have selected.";
			public const string IX_SITEGROUPS_NAME_MESSAGE = "The name that you entered is already used by an existing site group. Please enter a unique name.";
			public const string IX_SITES_NAME_MESSAGE = "The name that you entered is already used by an existing site. Please enter a unique name.";
			public const string IX_SITEALIAS_ALIAS_MESSAGE = "Another site is already using the alias that you entered.";
			public const string IX_PAGES_NAME_MESSAGE = "The name that you entered is already used by an existing page. Please enter a unique name.";
			public const string IX_PAGEROUTES_PATH_MESSAGE = "Another page is already using the page route that you entered.";
			public const string IX_USERS_SITEUSER_MESSAGE = "The name that you entered is already used by an existing user. Please enter a unique name.";
			public const string IX_ROLEGROUPS_NAME_MESSAGE = "The name that you entered is already used by an existing role group. Please enter a unique name.";
			public const string IX_ROLES_NAME_MESSAGE = "The name that you entered is already used by an existing role. Please enter a unique name.";
			public const string IX_USERPROFILEPROPERTIES_SITEID_TYPEURI_MESSAGE = "The Type Uri that you entered has already been used.";
			public const string IX_USERPROFILEPROPERTIES_NAME_MESSAGE = "The name that you entered is already used by an existing user profile property. Please enter a unique name.";
			public const string IX_MAILTEMPLATES_SITEID_NAME_MESSAGE = "The name that you entered is already used by an existing mail template. Please enter a unique name.";
			public const string IX_LISTS_NAME_MESSAGE = "The name that you entered is already used by an existing list. Please enter a unique name.";
			public const string IX_LISTITEMS_NAME_MESSAGE = "The name that you entered is already used by an existing list item. Please enter a unique name.";
			public const string IX_APIKEYS_NAME_MESSAGE = "The name that you entered is already used by an existing Api key. Please enter a unique name.";

			// Foreign key constraints. Most of these will never happen unless there is a bug in the UI/Controllers.
			public const string FK_SITEGROUPS_PRIMARYSITEID_MESSAGE = "Primary site is invalid.";				
			public const string FK_SITES_ADMINISTRATORSROLEID_MESSAGE = "Administrator role is invalid.";
			public const string FK_SITES_ALLUSERSROLEID_MESSAGE = "All users role is invalid";
			public const string FK_SITES_ANONYMOUSUSERSROLEID_MESSAGE = "Anonymous users role is invalid";
			public const string FK_SITES_REGISTEREDUSERSROLEID_MESSAGE = "Registered users role is invalid";
			public const string FK_PAGES_SITEID_MESSAGE = "Site is invalid.";
			public const string FK_PAGES_PARENTID_MESSAGE = "Parent page is invalid.";
			public const string FK_ROLES_ROLEGROUPID_MESSAGE = "Role group is invalid.";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member		
		}

		/// <summary>
		/// Class used to parse regular expressions for the ParseExceptionMessage function.
		/// </summary>
		private class MessageParser
		{
			private GroupCollection Groups { get; }

			public MessageParser(GroupCollection groups)
			{
				this.Groups = groups;
			}

			internal string MatchEvaluator(Match match)
			{
				if (match.Groups.Count > 0)
				{
					Group group = this.Groups[match.Groups[1].Value];

					if (group != null)
					{
						return group.Value;
					}
				}

				return "";
			}
		}
	}
}
