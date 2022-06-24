using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using System.Text.RegularExpressions;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension for providing friendly exception messages for database constraint errors.
	/// </summary>
	public static class DbExceptionExtensions
	{
		/// <summary>
		/// Parse a DbException and return a friendly message in a DataProviderException, or return the original exception if the 
		/// message is not recognized.
		/// </summary>
		/// <param name="ex"></param>
		public static Exception Parse(this Exception ex)
		{
			if (ex != null)
			{
				if (ex.InnerException != null)
				{
					return ParseException(ex.InnerException);
				}
				else
				{
					return ParseException(ex);
				}
			}
			return ex;
		}

		private class MessageData
		{
			public string Pattern { get; set; }
			public string Message { get; set; }
		}

		private const string GENERAL_NOT_NULL_MESSAGE = "The '{column}' field is required.";

		private const string SQLITE_NOT_NULL_PATTERN = @"SQLite Error 19: 'NOT NULL constraint failed: (?<table>.*)\.(?<column>.*)'.";
		private const string SQLITE_NOT_NULL_MESSAGE = GENERAL_NOT_NULL_MESSAGE;

		private const string SQLITE_FOREIGN_KEY_PATTERN = @"SQLite Error 19: 'FOREIGN KEY constraint failed.";
		private const string SQLITE_FOREIGN_KEY_MESSAGE = @"SQLite Error 19: 'FOREIGN KEY constraint failed.";

		private const string SQLITE_FOREIGN_KEY_PATTERN_LISTITEMS = @"SQLite Error 19: 'UNIQUE constraint failed: ListItems.ListId, ListItems.Name'.";
		private const string SQLITE_FOREIGN_KEY_MESSAGE_LISTITEMS = @"One of your list items has a duplicate name. List item names must be unique within the list.";

		private const string SQLITE_UNIQUE_CONSTRAINT_PATTERN = @"SQLite Error 19: 'UNIQUE constraint failed: (?<columns>.*)'.";
		private const string SQLITE_UNIQUE_CONSTRAINT_MESSAGE = @"The combination of {columns} must be unique.";

		private const string SQLSERVER_FOREIGN_KEY_PATTERN = "the delete statement conflicted .*constraint \"(?<constraint>.*?)\"";
		private const string SQLSERVER_FOREIGN_KEY_MESSAGE = "{constraint}";

		private const string SQLSERVER_NOT_NULL_PATTERN = @"cannot insert the value NULL into column '(?<column>.*?)', table '.*\..*\.(?<table>.*?)'";
		private const string SQLSERVER_NOT_NULL_MESSAGE = GENERAL_NOT_NULL_MESSAGE;

		private const string MYSQL_FOREIGN_KEY_PATTERN = "the delete statement conflicted .*constraint \"(?<constraint>.*?)\"";
		private const string MYSQL_FOREIGN_KEY_MESSAGE = "{constraint}";

		private const string MYSQL_NOT_NULL_PATTERN = @"a foreign key constraint fails.*constraint `(?<column>.*?)`";
		private const string MYSQL_NOT_NULL_MESSAGE = GENERAL_NOT_NULL_MESSAGE;

		private const string POSTGRES_FOREIGN_KEY_PATTERN = "update or delete.*constraint \"(?<constraint>.*?)\"";
		private const string POSTGRES_FOREIGN_KEY_MESSAGE = "{constraint}";

		private const string POSTGRES_NOT_NULL_PATTERN = "null value in column \"(?<column>.*?)\" of relation \"(?<table>.*)\" violates not-null constraint";
		private const string POSTGRES_NOT_NULL_MESSAGE = GENERAL_NOT_NULL_MESSAGE;

		private static List<MessageData> ErrorPatterns = new()
		{
			new() { Pattern = SQLITE_NOT_NULL_PATTERN, Message = SQLITE_NOT_NULL_MESSAGE },
			new() { Pattern = SQLITE_FOREIGN_KEY_PATTERN, Message = SQLITE_FOREIGN_KEY_MESSAGE },
			new() { Pattern = SQLSERVER_FOREIGN_KEY_PATTERN, Message = SQLSERVER_FOREIGN_KEY_MESSAGE },
			new() { Pattern = SQLSERVER_NOT_NULL_PATTERN, Message = SQLSERVER_NOT_NULL_MESSAGE },
			new() { Pattern = MYSQL_FOREIGN_KEY_PATTERN, Message = MYSQL_FOREIGN_KEY_MESSAGE },
			new() { Pattern = MYSQL_NOT_NULL_PATTERN, Message = MYSQL_NOT_NULL_MESSAGE },
			new() { Pattern = POSTGRES_FOREIGN_KEY_PATTERN, Message = POSTGRES_FOREIGN_KEY_MESSAGE },
			new() { Pattern = POSTGRES_NOT_NULL_PATTERN, Message = POSTGRES_NOT_NULL_MESSAGE },
			new() { Pattern = SQLITE_FOREIGN_KEY_PATTERN_LISTITEMS, Message = SQLITE_FOREIGN_KEY_MESSAGE_LISTITEMS },
			new() { Pattern = SQLITE_UNIQUE_CONSTRAINT_PATTERN, Message = SQLITE_UNIQUE_CONSTRAINT_MESSAGE }
		};

		private static Dictionary<string, string> ConstraintMessages = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "FK_Pages_ParentId", "Cannot delete a page with child pages. Please delete or reassign your child pages first." }
		};

		private static Exception ParseException(Exception ex)
		{
			if (ex != null && ex is System.Data.Common.DbException)
			{
				foreach (MessageData item in ErrorPatterns)
				{
					Match match = Regex.Match(ex.Message, item.Pattern, RegexOptions.IgnoreCase);
					if (match.Success)
					{
						return new Nucleus.Abstractions.DataProviderException
						(
							Regex.Replace(item.Message, "{(.*?)}", new MatchEvaluator(new MessageParser(match.Groups).MatchEvaluator)),
							ex
						);
					}
				}
			}

			return ex;
		}

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
						if (match.Groups[1].Value == "constraint")
						{
							if (ConstraintMessages.ContainsKey(group.Value))
							{
								return ConstraintMessages[group.Value];
							}
							else
							{
								return "missing: " + group.Value;
							}
						}
						else
						{
							return group.Value;
						}
					}
				}

				return "";
			}

		}
	}
}
