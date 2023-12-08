using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Data.EntityFramework;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Data.Common;
using Nucleus.Abstractions.Models.Configuration;
using static SQLitePCL.raw;

namespace Nucleus.Data.Sqlite;

/// <summary>
/// DbContext options configuration class for Sqlite
/// </summary>
/// <typeparam name="TDataProvider"></typeparam>
public class SqliteDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
  where TDataProvider : Nucleus.Data.Common.DataProvider
{
  private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

  /// <summary>
  /// Dictionary of column names/messages for ParseException method.
  /// </summary>
  private static Dictionary<string, string> SQLITE_MESSAGES = new(StringComparer.OrdinalIgnoreCase)
    {
      { "SiteGroups.PrimarySiteId", DbContextConfigurator.Messages.IX_SITEGROUPS_PRIMARYSITEID_MESSAGE},
      { "SiteGroups.Name", DbContextConfigurator.Messages.IX_SITEGROUPS_NAME_MESSAGE},
      { "Sites.Name" , DbContextConfigurator.Messages.IX_SITES_NAME_MESSAGE},
      { "SiteAlias.Alias" , DbContextConfigurator.Messages.IX_SITEALIAS_ALIAS_MESSAGE},
      { "Pages.SiteId, Pages.ParentId, Pages.Name" , DbContextConfigurator.Messages.IX_PAGES_NAME_MESSAGE},
      { "PageRoutes.SiteId, PageRoutes.Path" , DbContextConfigurator.Messages.IX_PAGEROUTES_PATH_MESSAGE},
      { "Users.UserName, Users.SiteId" , DbContextConfigurator.Messages.IX_USERS_SITEUSER_MESSAGE},
      { "RoleGroups.SiteId, RoleGroups.Name" , DbContextConfigurator.Messages.IX_ROLEGROUPS_NAME_MESSAGE},
      { "Roles.SiteId, Roles.Name" , DbContextConfigurator.Messages.IX_ROLES_NAME_MESSAGE},
      { "UserProfileProperties.SiteId, UserProfileProperties.TypeUri" , DbContextConfigurator.Messages.IX_USERPROFILEPROPERTIES_SITEID_TYPEURI_MESSAGE},
      { "UserProfileProperties.SiteId, UserProfileProperties.Name" , DbContextConfigurator.Messages.IX_USERPROFILEPROPERTIES_NAME_MESSAGE},
      { "MailTemplates.SiteId, MailTemplates.Name" , DbContextConfigurator.Messages.IX_MAILTEMPLATES_SITEID_NAME_MESSAGE},
      { "Lists.SiteId, Lists.Name" , DbContextConfigurator.Messages.IX_LISTS_NAME_MESSAGE},
      { "ListItems.ListId, ListItems.Name" , DbContextConfigurator.Messages.IX_LISTITEMS_NAME_MESSAGE},
      { "ApiKeys.Name", DbContextConfigurator.Messages.IX_APIKEYS_NAME_MESSAGE}
    };

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="databaseOptions"></param>
  /// <param name="folderOptions"></param>
  public SqliteDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions) : base(databaseOptions)
  {
    this.FolderOptions = folderOptions;
  }


  /// <summary>
  /// Perform (or retry) pre-configuration checks.
  /// </summary>
  /// <returns></returns>
  public override Boolean PreConfigure()
  {
    // Special case for Sqlite - ensure that the folder exists
    FolderOptions folderOptions = this.FolderOptions.Value;

    Microsoft.Data.Sqlite.SqliteConnection connection = new(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(this.DatabaseConnectionOption.ConnectionString));

    try
    {
      if (!connection.DataSource.Contains(":memory:"))
      {
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(connection.DataSource)))
        {
          System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(connection.DataSource));
        }
      }
      return true;
    }
    catch (System.UnauthorizedAccessException)
    {
      // permissions error on the Sqlite database folder.  Ignore here, and allow a database connection error
      // when Nucleus tries to connect to the database.
      return false;
    }
  }

  /// <summary>
  /// Configure the DbContextOptionsBuilder for Sqlite
  /// </summary>
  /// <param name="options"></param>
  /// <returns></returns>
  public override Boolean Configure(DbContextOptionsBuilder options)
  {
    if (this.DatabaseConnectionOption != null)
    {
      FolderOptions folderOptions = this.FolderOptions.Value;

      Microsoft.Data.Sqlite.SqliteConnection connection = new(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(this.DatabaseConnectionOption.ConnectionString));
      options.UseSqlite(connection.ConnectionString, options =>
      {
        // Sqlite locks the entire database during a write, so concurrent writes can cause SQLITE_BUSY ("Database is locked").  Entity
        // framework (actually Microsoft.Data.Sqlite) retries writes automatically until the time frame specified by .CommandTimeout.
        options.CommandTimeout(30);
      });

      return true;
    }

    return false;
  }

  /// <summary>
  /// Parse a database exception and match it to a friendly error message.  If a match is found, throw a <see cref="DataProviderException"/> to wrap the
  /// original exception with a more friendly error message.
  /// </summary>
  /// <param name="exception"></param>
  public override void ParseException(DbUpdateException exception)
  {
    // This code is inspired by, and uses code from https://github.com/Giorgi/EntityFramework.Exceptions.  https://www.apache.org/licenses/LICENSE-2.0
    if (exception == null) return;
    if (exception.InnerException == null) return;

    if (exception.InnerException is Microsoft.Data.Sqlite.SqliteException)
    {
      string message = "";

      Microsoft.Data.Sqlite.SqliteException dbException = exception.InnerException as Microsoft.Data.Sqlite.SqliteException;

      if (dbException.SqliteErrorCode == SQLITE_CONSTRAINT || dbException.SqliteErrorCode == SQLITE_TOOBIG)
      {
        switch (dbException.SqliteExtendedErrorCode)
        {
          case SQLITE_TOOBIG:
            // This error is generated when a blob is more than a million bytes.  This would typically be caused by a bug rather than user action, 
            // so we don't parse them.
            break;
          case SQLITE_CONSTRAINT_NOTNULL:
            message = ParseException(dbException, MessagePatterns.NOT_NULL_PATTERN, MessagePatterns.NOT_NULL_MESSAGE);
            break;
          case SQLITE_CONSTRAINT_UNIQUE:
            message = ParseException(dbException, MessagePatterns.UNIQUE_CONSTRAINT_PATTERN, MessagePatterns.UNIQUE_CONSTRAINT_MESSAGE);
            break;
          case SQLITE_CONSTRAINT_PRIMARYKEY:
            // primary key constraint failed errors would typically be caused by a bug rather than user action, so we don't parse them.
            break;
          case SQLITE_CONSTRAINT_FOREIGNKEY:
            // SQLite doesn't provide any useful information in the error message when a foreign key constraint fails, so the parsing for this
            // isn't very useful - it just generates an exception with message 'FOREIGN KEY constraint failed'.  
            message = ParseException(dbException, MessagePatterns.FOREIGN_KEY_PATTERN, MessagePatterns.FOREIGN_KEY_MESSAGE);
            break;
        };

        if (!String.IsNullOrEmpty(message))
        {
          throw new Nucleus.Abstractions.DataProviderException(message, exception);
        }
      }
    }
  }

  /// <summary>
  /// Special error message replacement for SQLite.
  /// </summary>
  /// <param name="exception"></param>
  /// <param name="pattern"></param>
  /// <param name="message"></param>
  /// <returns></returns>
  /// <remarks>
  /// SQLite does not return unique constraint names when a unique constraint fails, so instead we use the column names in the error message to match  
  /// to a custom error.  The list of table.column names / messages is in SQLITE_MESSAGES.
  /// </remarks>
  protected string ParseException(Microsoft.Data.Sqlite.SqliteException exception, string pattern, string message)
  {
    // get "default" message, parse the message and populate the base.ExceptionMessageTokens dictionary.
    string customMessage = base.ParseException(exception, pattern, message);

    // replace the error message with a friendly message
    if (base.ExceptionMessageTokens != null && base.ExceptionMessageTokens.ContainsKey("columns"))
    {
      switch (exception.SqliteExtendedErrorCode)
      {
        case SQLITE_CONSTRAINT_UNIQUE:
          if (SQLITE_MESSAGES.ContainsKey(base.ExceptionMessageTokens["columns"]))
          {
            return SQLITE_MESSAGES[(base.ExceptionMessageTokens["columns"])];
          }
          break;
      }
    }
    return customMessage;
  }

  internal class MessagePatterns
  {
    internal const string UNIQUE_CONSTRAINT_PATTERN = @"'UNIQUE constraint failed: (?<columns>.*)'.";
    internal const string UNIQUE_CONSTRAINT_MESSAGE = @"The combination of {columns} must be unique.";

    internal const string NOT_NULL_PATTERN = @"NOT NULL constraint failed: (?<table>.*)\.(?<column>.*)'";
    internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

    internal const string FOREIGN_KEY_PATTERN = @"FOREIGN KEY constraint failed.";
    internal const string FOREIGN_KEY_MESSAGE = @"FOREIGN KEY constraint failed.";
  }
}
