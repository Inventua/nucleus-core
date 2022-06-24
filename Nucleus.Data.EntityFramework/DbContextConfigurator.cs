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
	/// Database-specific EF implementations implement this interface in order to configure their EF DbContext.
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
		/// Gets the data provider that this DbContext is configured to use.
		/// </summary>
		
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
		public virtual Boolean PreConfigure() { return true; }


		/// <summary>
		/// Gets the database connection option for this DbContextConfigurator.
		/// </summary>
		public Nucleus.Abstractions.Models.Configuration.DatabaseConnectionOption DatabaseConnectionOption { get; protected set; }

		/// <summary>
		/// Parse a database exception and if recognized, throw an exception with a friendly message.
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
			switch (name)
			{
				case "IX_ScheduledTasks_Name":
					return "Another scheduled task in this site already has the name that you entered.";
				case "IX_SiteGroups_PrimarySiteId":
					return "Another site group is already assigned to the primary site that you have selected.";
				case "IX_SiteGroups_Name":
					return "Another site group already has the name that you entered.";
				case "IX_Sites_Name":
					return "Another site already has the name that you entered for this site.  Site names must be unique.";
				case "IX_SiteAlias_Alias":
					return "Another site is already using the alias that you entered.";
				case "IX_Pages_Name":
					return "Another page in this site already has the name that you entered.";
				case "IX_PageRoutes_Path":
					return "Another page is already using the page route that you entered.";
				case "IX_Users_SiteUser":
					return "Another user in this site already has the user name that you entered.";
				case "IX_RoleGroups_Name":
					return "Another role group in this site already has the name that you entered.";
				case "IX_Roles_Name":
					return "Another role in this site already has the name that you entered.";
				case "IX_UserProfileProperties_SiteId_TypeUri":
					return "The Type Uri that you entered has already been used.";
				case "IX_UserProfileProperties_Name":
					return "The user profile property name that you entered has already been used.";
				case "IX_MailTemplates_SiteId_Name":
					return "The mail template name that you entered has already been used.";
				case "IX_Lists_Name":
					return "The list name that you entered has already been used.";
				case "IX_ListItems_Name":
					return "The list item name has already been used in this list.";
				case "IX_ApiKeys_Name":
					return "The Api key name that you entered has already been used.";
				default:
					return "";
			}
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
						//if (match.Groups[1].Value == "constraint")
						//{
						//	if (ConstraintMessages.ContainsKey(group.Value))
						//	{
						//		return ConstraintMessages[group.Value];
						//	}
						//	else
						//	{
						//		return "missing: " + group.Value;
						//	}
						//}
						//else
						//{
						return group.Value;
						//}
					}
				}

				return "";
			}

		}

	}
}
