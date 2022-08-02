## Database scripts
Database scripts are used to create and manage your database schema (tables, indexes and other database objects).  The recommended
format for Nucleus database scripts is a json-based format which specifies one or more ==operations== which are executed using 
==Entity Framework==.  Database scripts are included in your extension assembly as ==Embedded Resources==, and must be located in 
the DataProvider/database-type/scripts folder, where database-type is:

| Name             |  Description                                                                          |
|------------------|---------------------------------------------------------------------------------------|
| Migrations       |  scripts which can be used with all of the database providers, normally in .json format.  |
| SqlServer        |  Microsoft SQL Server scripts, normally in .sql format. |
| MySql            |  MySql/MariaDb scripts, normally in .sql format. |
| Sqlite           |  Sqlite scripts, normally in .sql format. |
| PostgreSql       |  PostgreSql scripts, normally in .sql format. |

> You can create database scripts in SQL format, but database definition SQL commands differ between database 
providers.  This may be a useful option for developers of line-of-business applications which do not need to support more than
one database type.  Scripts in .sql format must be located in the `Migrations\SQLite\Scripts`, `Migrations\SqlServer\Scripts`,
`Migrations\MySql\Scripts` and `Migrations\PostgreSql\Scripts` folders.  Script files in .sql format are text files which contain 
one or more SQL commands, each of which must end in `GO`, followed by a new line.  
**The`.json` format is recommended, because you can use it to write scripts which target all of the database providers instead 
of writing separate scripts for each database provider.**

Script files must be named `{version}.json` or `{version}.sql`, where ==\{version\}== is a version number which can be parsed by 
the .Net [System.Version](https://docs.microsoft.com/en-us/dotnet/api/system.version) class - that is, a version number in the 
form ==major.minor.build.revision==, where all parts are numbers.  

> Script version numbers do not have to match your extension's assembly version number.  Version 0 is reserved, so you should start at 
version 1.0.0.0.  The version number is used to sort your scripts, so that they are executed in the correct order, and are 
also used to track whether a script has already been run.

### Syntax
Script files in `.json` format consist of a schema name, version and operations element which contains one or more operations:

| Name             |  Description                                                                          |
|------------------|---------------------------------------------------------------------------------------|
| schemaName       |  This is a unique name for your database objects (schema).  It is used to track whether a script has run, and must be unique to your extension.  It's a good idea to use your project namespace as the schema name.   |
| version          |  The version of the database schema represented by this script.  This value should match the script file name.  |
| operations       |  Contains one or more operations which are executed by the script. (see below for more information). |

#### Example
```
{
  "schemaName": "Nucleus.Sample",
  "version": "01.00.00",
  "operations": [
    {
      "createTable": {
        "name": "Item",
        "columns": [
          {
            "name": "Id",
            "clrType": "guid",
            "isnullable": false
          },
          {
            "name": "Name",
            "clrType": "string",
            "isnullable": false,
            "maxlength": 256
          }
        ],
        "primaryKey": {
          "name": "PK_Item",
          "isclustered": true,
          "columns": [ "Id" ]
        }
      }
    },
    {
      "addColumn": {
        "table": "Things",
        "name": "Type",
        "clrType": "int",
        "isnullable": true
      }
    },
    {
      "dropIndex": {
        "name": "IX_SomeTable_SomeColumn",
        "table": "SomeTable"
      }
    },
    {
      "createIndex": {
        "name": "IX_SomeTable_SomeColumn_AnotherColumn",
        "table": "SomeTable",
        "columns": [ "SomeColumn", "AnotherColumn" ],
        "isUnique": false
      }
    }
  ]
}
```

> **How it works**\
==Operation== elements in your json script are deserialized into Entity Framework objects from the 
[Microsoft.EntityFrameworkCore.Migrations.Operations](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.operations) 
namespace.  The element name in the script file matches the class name, with the 'Operation' suffix removed - for example, the json file 
operation ==CreateTable== represents the Entity Framework [CreateTableOperation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.operations.createtableoperation?view=efcore-6.0)
class.  For each operation, Nucleus uses another Entity Framework class, 
[IMigrationsSqlGenerator](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.imigrationssqlgenerator) 
to generate SQL for the operation represented by the operation object and then executes the SQL.  
The `DatabaseProviderSpecificOperation` operation is implemented by Nucleus to wrap an operation in a condition (see below).  Except for 
MySql, the entire script is executed within a transaction.  
[MySql does not support multiple data definition SQL statements in a single transaction](https://dev.mysql.com/doc/refman/8.0/en/implicit-commit.html), so we don't create a 
transaction when you are using a MySql or MariaDb database.

### Operations
| Name             |  Description                                                                          |
|------------------|---------------------------------------------------------------------------------------|
| **createTable**  |  Creates a new table.  Must contain a name (table name) and one or more columns.  May contain a primary key and foreign keys. |
| - columns        |  name: the column name                                                                  |
|                  |  clrType: a .Net type which Entity Frameworl can map to a column type: guid, string, int, boolean, datetime or another simple .Net type.  |
|                  |  isnullable: true/false                                                                 |
|                  |  maxlength: integer.  Maxlength has no effect when using Sqlite .                       |
| - primaryKey     |  name:  Name of the primary key.  Recommended: Use IX_tablename                         |
|                  |  isclustered: true/false                                                                |
|                  |  columns:  an array of column names, which must exist in the table.                     |
| - foreignKeys    |  (optional)  One or more foreign keys, which each have:                                 |
|                  |  name:  The index name.  Recommended: FK_tablename_column1_column2...                   |
|                  |  columns:  an array of column names in 'this' table.                                    |
|                  |  principalTable: The name of the 'other' table in the foreign key.                      |
|                  |  principalColumns:  an array of column names in the 'other' table.                      |
|                  |  onDelete:  (optional).  Referential action to perform when a record in the other table is deleted (Cascade, Restrict, SetNull, SetDefault ). |
| **createIndex**  |  Create a a new index on an existing table.  |
|                  |  name:   Index name.  Recommended: IX_tablename_column1_column2... |
|                  |  table:  Table name to apply the index to.              |
|                  |  columns:  Array of columns to include in the index. |
|                  |  isUnique:  true/false.                         |
| **addColumn**    |  Adds a column to an existing table.  Refer to the columns documentation for the createTable operation above. |
| **dropIndex**    |  Deletes an existing index from a table.  |
|                  |  name:   Index name to drop.                                       |
|                  |  table:  Table which has the existing index.             |
| **sql**          |  Executes an SQL command.  |
|                  |  sql:   SQL command.                                              |
| **databaseProviderSpecificOperation** |  This is a special operation, implemented by Nucleus and used to wrap another operation in a condition.  More information is below. |  

> Other properties are available, and are [documented here](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.operations). 

> Be careful using OnDelete or OnUpdate refererential actions on foreign keys. [Sql Server does not support multiple 
referential actions on the same table](https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1785-database-engine-error?view=sql-server-ver16).

#### Adjustments on Operations
Nucleus performs several adjustments to operations before executing them to address some of the differences between database types, and to 
apply defaults so that developers can skip specifying some operation properties:
1. Before executing a ==createTable== operation, Nucleus checks whether the table already exists.  If it does, the operation is not executed.
2. Before executing a ==createIndex== operation, Nucleus checks whether the index already exists.  If it does, the operation is not executed.
3. Before executing a ==dropIndex== operation, Nucleus checks whether the index already exists.  If it does not exist, the operation is not executed.
4. For Sql Server only, if the only column in a primary key column is a Guid, and is named ==Id==, or ==\{table_name\}Id==, the default value is set to `newsequentialid()`.
5. For all database types, if a column does not specify a value for the IsUnicode property, it is set to true.
6. For Sqlite only, if column collation is not specified for a string column, it is defaulted to `NOCASE`. 
7. For PostgreSql only, the default value for boolean columns is set to `true` or `false` rather than `1` or `0`, even if the DefaultValueSql value is 1 or 0.
8. For all databases except PostgreSql, the default value for boolean columns is set to `1` or `0` rather than `true` or `false`, even if the DefaultValueSql value is `true` or `false`.

> There are other operations available in the [Microsoft.EntityFrameworkCore.Migrations.Operations](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.operations) 
namespace, and you can use them in json database script files, but Nucleus doesn't make any special adjustments to the properties of other ==operations==.

### DatabaseProviderSpecificOperation
You may need to execute different operations for specific database providers.  The DatabaseProviderSpecificOperation wraps any operation with a set of `Include` or `Exclude` provider 
names to limit when the operation is executed.  Supported values for the `Include` and `Exclude` properties are Sqlite, SqlServer, MySql and PostgreSql.

> It is unusual to need to use `DatabaseProviderSpecificOperation` in an extension script.

```
{
  ...
  "operations": [
  {
    // Sql Server unique indexes do not allow multiple rows set to NULL by default, so we exclude Sql Server
    // and execute an alternative command below.
    "DatabaseProviderSpecificOperation": {
      "exclude": [ "SqlServer" ],
      "operation": {
        "createIndex": {
          "name": "IX_UserSecrets_PasswordResetToken",
          "table": "UserSecrets",
          "columns": [ "PasswordResetToken" ],
          "isUnique": true
        }
      }
    }
  },
  {
    // Sql Server unique indexes do not allow multiple rows set to NULL by default, so we have to execute a specific
    // command to create the index.
    "DatabaseProviderSpecificOperation": {
      "include": [ "SqlServer" ],
      "operation": {
        "sql": {
          "sql": "CREATE UNIQUE INDEX IX_UserSecrets_PasswordResetToken ON [UserSecrets] ([PasswordResetToken]) WHERE [PasswordResetToken] IS NOT NULL;"
        }
      }
    }
  }
  ...
}
```