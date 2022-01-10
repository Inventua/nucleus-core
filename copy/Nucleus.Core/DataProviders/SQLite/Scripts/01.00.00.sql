CREATE TABLE IF NOT EXISTS [Schema] 
(
	[SchemaName] TEXT PRIMARY KEY,
	[SchemaVersion] TEXT NULL
);
GO

CREATE TABLE IF NOT EXISTS [ModuleDefinitions] 
(
	[Id] GUID PRIMARY KEY,
	[FriendlyName] TEXT NOT NULL,
	[ClassTypeName] TEXT NOT NULL,
	[ViewAction] TEXT NOT NULL,
	[EditAction] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([FriendlyName])
);
GO

CREATE TABLE IF NOT EXISTS [LayoutDefinitions] 
(
	[Id] GUID PRIMARY KEY,
	[FriendlyName] TEXT NOT NULL,
	[RelativePath] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL
);
GO

CREATE TABLE IF NOT EXISTS [ContainerDefinitions] 
(
	[Id] GUID PRIMARY KEY,
	[FriendlyName] TEXT NOT NULL,
	[RelativePath] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL
);
GO

CREATE TABLE IF NOT EXISTS [ScheduledTasks] 
(
	[Id] GUID PRIMARY KEY,
	[TypeName] TEXT NOT NULL,
	[Name] TEXT NOT NULL,
	[Interval] INT NOT NULL,
	[IntervalType] INT NOT NULL,
	[Enabled] BIT NOT NULL,
	[NextScheduledRun] DATETIME NULL,
	[KeepHistoryCount] int NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Name])
);
GO

CREATE TABLE IF NOT EXISTS [ScheduledTasksHistory] 
(
	[Id] GUID PRIMARY KEY,
	[ScheduledTaskId] GUID NOT NULL,
	[StartDate] DATETIME NOT NULL,
	[FinishDate] DATETIME NULL,
	[NextScheduledRun] DATETIME NULL,
	[Server] TEXT NOT NULL,
	[Status] INT NULL,
	[DateAdded] DATETIME NULL,
	[DateChanged] DATETIME NULL,
	FOREIGN KEY([ScheduledTaskId]) REFERENCES ScheduledTasks(Id) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [SiteGroups] 
(
	[Id] GUID PRIMARY KEY,
	[Name] TEXT NOT NULL,
	[Description] TEXT,
	[PrimarySiteId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Name]),
	UNIQUE([PrimarySiteId]),
	FOREIGN KEY([PrimarySiteId]) REFERENCES Sites(Id) ON DELETE SET NULL
);
GO

CREATE TABLE IF NOT EXISTS [Sites] 
(
	[Id] GUID PRIMARY KEY,
	[Name] TEXT NOT NULL,
	[AdministratorsRoleId] GUID NULL,
	[RegisteredUsersRoleId] GUID NULL,
	[AnonymousUsersRoleId] GUID NULL,
	[AllUsersRoleId] GUID NULL,
	[SiteGroupId] GUID NULL,
	[DefaultLayoutDefinitionId] GUID NULL,
	[HomeDirectory] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Name]),
	FOREIGN KEY([AdministratorsRoleId]) REFERENCES Roles(Id),
	FOREIGN KEY([RegisteredUsersRoleId]) REFERENCES Roles(Id),
	FOREIGN KEY([AnonymousUsersRoleId]) REFERENCES Roles(Id),
	FOREIGN KEY([RegisteredUsersRoleId]) REFERENCES Roles(Id)
);
GO

CREATE TABLE IF NOT EXISTS [SiteAlias] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID NOT NULL,
	[Alias] TEXT NOT NULL COLLATE NOCASE,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Alias]),
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);

CREATE INDEX SiteAlias_Alias ON [SiteAlias]([Alias]);
GO

CREATE INDEX SiteAlias_SiteId ON [SiteAlias]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [SiteSettings] 
(
	[SiteId] GUID NOT NULL,
	[SettingName] TEXT NOT NULL,
	[SettingValue] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	PRIMARY KEY([SiteId],[SettingName]),
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX SiteSettings_SiteId ON [SiteSettings]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [Pages] 
(
	[Id] GUID PRIMARY KEY,
	[ParentId] GUID NULL,
	[SiteId] GUID NULL,
	[Name] TEXT NOT NULL,
	[Title] TEXT NULL,
	[Description] TEXT NULL,
	[Keywords] TEXT NULL,
	[DefaultPageRouteId] GUID NULL,
	[Disabled] BIT NOT NULL,
	[ShowInMenu] BIT NOT NULL,
	[DisableInMenu] BIT NOT NULL,
	[LayoutDefinitionId] GUID NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([SiteId],[ParentId],[Name]),
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE,
	FOREIGN KEY([DefaultPageRouteId]) REFERENCES PageRoutes(Id) ON DELETE SET NULL,
	FOREIGN KEY([ParentId]) REFERENCES Pages(Id) ON DELETE SET NULL
);
GO

CREATE INDEX Pages_SiteId ON [Pages]([SiteId]);
GO

CREATE INDEX Pages_ParentId ON [Pages]([ParentId]);
GO

CREATE TABLE IF NOT EXISTS [PageRoutes] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID,
	[PageId] GUID,
	[Path] TEXT NOT NULL COLLATE NOCASE,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id),
	FOREIGN KEY([PageId]) REFERENCES Pages(Id) ON DELETE CASCADE
);
GO

CREATE INDEX PageRoutes_PageId ON [PageRoutes]([PageId]);
GO

CREATE UNIQUE INDEX PageRoutes_Path ON [PageRoutes]([SiteId],[Path]);
GO

CREATE TABLE IF NOT EXISTS [PageModules] 
(
	[Id] GUID PRIMARY KEY,
	[PageId] GUID NOT NULL,
	[Pane] TEXT NOT NULL,
	[ContainerDefinitionId] GUID NULL,
	[Title] TEXT NULL,
	[ModuleDefinitionId] GUID NOT NULL,
	[SortOrder] INT NOT NULL,
	[InheritPagePermissions] BIT NOT NULL,
	[Style] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([PageId]) REFERENCES Pages(Id) ON DELETE CASCADE,
	FOREIGN KEY([ModuleDefinitionId]) REFERENCES ModuleDefinitions(Id) ON DELETE CASCADE
);
GO

CREATE INDEX PageModules_PageId ON [PageModules]([PageId]);
GO

CREATE TABLE IF NOT EXISTS [PageModuleSettings] 
(
	[ModuleId] GUID NOT NULL,
	[SettingName] TEXT NOT NULL,
	[SettingValue] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	PRIMARY KEY([ModuleId],[SettingName]),
	FOREIGN KEY([ModuleId]) REFERENCES PageModules(Id) ON DELETE CASCADE
);
GO

CREATE INDEX PageModuleSettings_ModuleId ON [PageModuleSettings]([ModuleId]);
GO

CREATE TABLE IF NOT EXISTS [Users] 
(
	[Id] GUID PRIMARY KEY,
	[UserName] TEXT NOT NULL,
	[SiteId] GUID,
	[IsSystemAdministrator] BIT,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX Users_SiteUser ON [Users]([UserName],[SiteId]);
GO

CREATE INDEX Users_SiteId ON [Users]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [UserRoles] 
(
	[UserId] GUID NOT NULL,
	[RoleId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	PRIMARY KEY([UserId],[RoleId]),
	FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE,
	FOREIGN KEY([RoleId]) REFERENCES Roles(Id) ON DELETE CASCADE
);
GO

CREATE INDEX UserRoles_UserId ON [UserRoles]([UserId]);
GO

CREATE INDEX UserRoles_RoleId ON [UserRoles]([RoleId]);
GO

CREATE TABLE IF NOT EXISTS [UserSecrets] 
(
	[UserId] GUID PRIMARY KEY,
	[PasswordHash] TEXT NOT NULL,
	[PasswordHashAlgorithm] TEXT NOT NULL,
	[PasswordResetToken] TEXT NULL,
	[PasswordResetTokenExpiryDate] DATETIME NULL,
	[PasswordQuestion] TEXT NULL,
	[PasswordAnswer] TEXT NULL,
	[LastLoginDate] DATETIME NULL,
	[LastPasswordChangedDate] DATETIME NULL,
	[LastLockoutDate] DATETIME NULL,
	[IsLockedOut] BIT NULL,
	[FailedPasswordAttemptCount] INT,
	[FailedPasswordWindowStart] DATETIME NULL,
	[FailedPasswordAnswerAttemptCount] INT,
	[FailedPasswordAnswerWindowStart] DATETIME NULL,
	[Salt] TEXT,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_UserSecrets_PasswordResetToken ON [UserSecrets] ([PasswordResetToken]);
GO;

CREATE TABLE IF NOT EXISTS [UserProfileProperties] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID NOT NULL,
	[Name] TEXT NOT NULL,
	[TypeUri] TEXT NULL,
	[HelpText] TEXT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX UserProfileProperties_SiteId ON [UserProfileProperties]([SiteId]);
GO;

CREATE UNIQUE INDEX IX_UserProfileProperties_SiteId_TypeUri ON [UserProfileProperties] ([SiteId],[TypeUri]);
GO;

CREATE UNIQUE INDEX IX_UserProfileProperties_SiteId_Name ON [UserProfileProperties] ([SiteId],[Name]);
GO;

CREATE TABLE IF NOT EXISTS [UserProfileValues] 
(
	[UserId] GUID NOT NULL,
	[UserProfilePropertyId] GUID NOT NULL,
	[Value] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	PRIMARY KEY([UserId],[UserProfilePropertyId]),
	FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE,
	FOREIGN KEY([UserProfilePropertyId]) REFERENCES UserProfileProperties(Id) ON DELETE CASCADE
);
GO

CREATE INDEX UserProfile_UserId ON [UserProfileValues]([UserId]);
GO


CREATE TABLE IF NOT EXISTS [UserSessions] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID NOT NULL,
	[UserId] GUID NOT NULL,
	[SlidingExpiry] BIT NOT NULL,
	[IsPersistent] BIT NOT NULL,
	[ExpiryDate] DATETIME NOT NULL,
	[IssuedDate] DATETIME NOT NULL,
	[RemoteIpAddress] TEXT NOT NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE,
	FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_UserSession_UserId ON UserSessions([UserId]);
GO;

CREATE INDEX IX_UserSession_SiteId ON UserSessions([SiteId]);
GO;

CREATE TABLE IF NOT EXISTS [RoleGroups] 
(
	[Id] GUID PRIMARY KEY,
	[Name] TEXT NOT NULL,
	[Description] TEXT,
	[SiteId] GUID,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([SiteId],[Name]),
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX RoleGroups_SiteId ON [RoleGroups]([SiteId]);
GO


CREATE TABLE IF NOT EXISTS [Roles] 
(
	[Id] GUID PRIMARY KEY,
	[RoleGroupId] GUID,
	[Name] TEXT NOT NULL,
	[Description] TEXT,
	[SiteId] GUID,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([SiteId],[Name]),
	FOREIGN KEY([RoleGroupId]) REFERENCES RoleGroups(Id) ON DELETE SET NULL
);
GO

CREATE INDEX Roles_SiteId ON [Roles]([SiteId]);
GO

CREATE INDEX Roles_RoleGroupId ON [Roles]([RoleGroupId]);
GO


CREATE TABLE IF NOT EXISTS [PermissionTypes] 
(
	[Id] GUID PRIMARY KEY,
	[ModuleDefinitionId] GUID NULL,
	[Scope] TEXT NOT NULL,
	[Name] TEXT NOT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Scope]),
	FOREIGN KEY([ModuleDefinitionId]) REFERENCES ModuleDefinitions(Id) ON DELETE CASCADE
);
GO

CREATE INDEX PermissionTypes_ModuleDefinitionId ON [PermissionTypes]([ModuleDefinitionId]);
GO

CREATE TABLE IF NOT EXISTS [Permissions] 
(
	[Id] GUID PRIMARY KEY,
	[RelatedId] GUID NOT NULL,
	[PermissionTypeId] GUID NOT NULL,
	[RoleId] GUID NOT NULL,
	[AllowAccess] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([RelatedId], [PermissionTypeId], [RoleId]),
	FOREIGN KEY([PermissionTypeId]) REFERENCES PermissionTypes(Id) ON DELETE CASCADE
);
GO

CREATE INDEX Permissions_RelatedId ON [Permissions]([RelatedId]);
GO

CREATE TABLE IF NOT EXISTS [MailTemplates] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID NOT NULL,
	[Name] TEXT NOT NULL,
	[Subject] TEXT NOT NULL,
	[Body] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX MailTemplates_SiteId_Name ON [Roles]([SiteId],[Name]);
GO

CREATE TABLE IF NOT EXISTS [FileSystemItems] 
(
	[Id] GUID PRIMARY KEY,
	[SiteId] GUID,
	[Provider] TEXT NOT NULL COLLATE NOCASE,
	[Path] TEXT NOT NULL COLLATE NOCASE,
	[ItemType] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX FileSystemItems_Path ON [FileSystemItems]([Provider],[Path]);
GO

CREATE TABLE IF NOT EXISTS [Lists] 
(
	[Id] GUID PRIMARY KEY,
	[Name] TEXT NOT NULL,
	[Description] TEXT NULL,
	[SiteId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([Name]),
	FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [ListItems] 
(
	[Id] GUID PRIMARY KEY,
	[ListId] GUID NOT NULL,
	[Name] TEXT NOT NULL,
	[Value] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	UNIQUE([ListId],[Name]),
	FOREIGN KEY([ListId]) REFERENCES Lists(Id) ON DELETE CASCADE
);
GO


INSERT INTO ModuleDefinitions
([Id], [FriendlyName], [ClassTypeName],	[ViewAction], [EditAction])
VALUES
('B516D8DD-C793-4776-BE33-902EB704BEF6', 'Html', 'Nucleus.Modules.TextHtml.Controllers.TextHtmlController,Nucleus.Modules.TextHtml' ,'Index', 'Edit');
GO

INSERT INTO LayoutDefinitions
([Id], [FriendlyName], [RelativePath])
VALUES
('2FF6818A-09FE-4EE2-BEFF-495A876AB6D6', 'Default', 'Shared\Layouts\default.cshtml');
GO

INSERT INTO LayoutDefinitions
([Id], [FriendlyName], [RelativePath])
VALUES
('DFAFC982-5CCE-4E09-91F3-A01B9130F1A2', 'Default 3 Column', 'Shared\Layouts\default-3-column.cshtml');
GO

INSERT INTO LayoutDefinitions
([Id], [FriendlyName], [RelativePath])
VALUES
('07F3C7F2-97E6-46D3-8218-1E02CBBB5CF2', 'Default 2 Column', 'Shared\Layouts\default-2-column.cshtml');
GO

INSERT INTO ContainerDefinitions
([Id], [FriendlyName], [RelativePath])
VALUES
('80A7F079-F61D-42A4-9A4B-DA7692415952', 'Default', 'Shared\Containers\default.cshtml');
GO

INSERT INTO ContainerDefinitions
([Id], [FriendlyName], [RelativePath])
VALUES
('CC0B83AC-A2D4-426F-B455-1715102D4A5E', 'no-container', 'Shared\Containers\no-container.cshtml');
GO

INSERT INTO Sites
([Id], [Name], [AdministratorsRoleId], [RegisteredUsersRoleId], [SiteGroupId], [HomeDirectory])
VALUES
('FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Default Site', NULL, NULL, NULL, '');
GO

INSERT INTO SiteAlias
([Id], [SiteId], [Alias])
VALUES
('65CA22FB-EF9D-45C1-8EC0-5F5AB3333C36', 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', '');
GO

INSERT INTO UserProfileProperties
([Id], [SiteId], [Name], [HelpText], [TypeUri], [SortOrder])
VALUES
('26826E18-7C9A-493A-9B10-97360078FD6F', 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Email', 'User email address', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', 10);
GO

INSERT INTO Pages
([Id], [ParentId], [SiteId], [Name], [Title], [Description], [DefaultPageRouteId], [ShowInMenu], [Disabled], [DisableInMenu], [LayoutDefinitionId], [SortOrder])
VALUES
('6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', null, 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Home', 'Home', NULL, NULL , 1, 0, 0, NULL, 10);
GO

INSERT INTO PageRoutes
([Id], [SiteId],[PageId], [Path], [Type])
VALUES
('D39B1B24-D845-4F73-A6A9-079AAFF29B4E', 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', '/', 0);
GO

UPDATE Pages
SET DefaultPageRouteId = 'D39B1B24-D845-4F73-A6A9-079AAFF29B4E'
WHERE Id = '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1';
GO

INSERT INTO PageModules
([Id], [PageId], [Pane], [Title], [ModuleDefinitionId], [SortOrder], [InheritPagePermissions])
VALUES
('9A41BDD2-AC49-4068-ACB0-DA821E47BE04', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', 'ContentPane', 'Welcome', 'B516D8DD-C793-4776-BE33-902EB704BEF6', 10, 1);
GO

INSERT INTO PageModuleSettings
([ModuleId], [SettingName], [SettingValue])
VALUES
('9A41BDD2-AC49-4068-ACB0-DA821E47BE04', 'texthtml:content', 'This is content from the database!');
GO

INSERT INTO Users
([Id], [UserName], [SiteId], [IsSystemAdministrator])
VALUES
('0045048D-408A-4A96-8E21-61AF4819D252', 'sysadmin', NULL, 1);
GO

INSERT INTO UserSecrets
([UserId], [PasswordHash], [PasswordHashAlgorithm])
VALUES
('0045048D-408A-4A96-8E21-61AF4819D252','x61Ey612Kl2gpFL56FT9weDnpSo4AV8j8+qx2AuTHdRyY036xxzTTrw10Wq3+4qQyB+XURPWx1ONxp3Y3pB37A==','SHA512');
GO;

INSERT INTO Users
([Id], [UserName], [SiteId])
VALUES
('68275116-1B5A-42B9-A769-9A2B3E733142', 'admin', 'FD25DBBE-3EE7-4893-9959-6D8974E55D69');
GO

INSERT INTO UserSecrets
([UserId], [PasswordHash], [PasswordHashAlgorithm])
VALUES
('68275116-1B5A-42B9-A769-9A2B3E733142','x61Ey612Kl2gpFL56FT9weDnpSo4AV8j8+qx2AuTHdRyY036xxzTTrw10Wq3+4qQyB+XURPWx1ONxp3Y3pB37A==','SHA512');
GO;

INSERT INTO Roles
([Id], [RoleGroupId], [SiteId], [Name], [Description], [Type])
VALUES
('469E6802-0CB7-4E19-94E6-BC6D4E927988', NULL, 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Administrators', 'System Administrators', 1);
GO

INSERT INTO Roles
([Id], [RoleGroupId], [SiteId], [Name], [Description], [Type])
VALUES
('B97D19F1-CE62-4A9E-B387-5888218C5DDE', NULL, 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Registered Users', 'Registered Users', 1);
GO

INSERT INTO Roles
([Id], [RoleGroupId], [SiteId], [Name], [Description], [Type])
VALUES
('02225041-FA55-433F-918C-36993160D703', NULL, 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'Anonymous Users', 'This role represents users who are not logged on.', 3);
GO

INSERT INTO Roles
([Id], [RoleGroupId], [SiteId], [Name], [Description], [Type])
VALUES
('A0FFFF1E-6FEA-48F2-BD2B-C9DD2E4805ED', NULL, 'FD25DBBE-3EE7-4893-9959-6D8974E55D69', 'All Users', 'This role represents all users, whether logged on or not.', 3);
GO

UPDATE Sites
SET AdministratorsRoleId = '469E6802-0CB7-4E19-94E6-BC6D4E927988', 
		RegisteredUsersRoleId = 'B97D19F1-CE62-4A9E-B387-5888218C5DDE',
		AnonymousUsersRoleId = '02225041-FA55-433F-918C-36993160D703',
		AllUsersRoleId = 'A0FFFF1E-6FEA-48F2-BD2B-C9DD2E4805ED'
WHERE Id = 'FD25DBBE-3EE7-4893-9959-6D8974E55D69';
GO

INSERT INTO UserRoles
([UserId],[RoleId])
VALUES
('68275116-1B5A-42B9-A769-9A2B3E733142', '469E6802-0CB7-4E19-94E6-BC6D4E927988');
GO;

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('7EC40AAA-9EB0-4005-9895-C0256E749B83', NULL, 'urn:nucleus:entities:page/permissiontype/view', 'View', 0);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('F7AD2BA9-948A-4EB4-BC6E-FCE814AEE0BF', NULL, 'urn:nucleus:entities:page/permissiontype/edit', 'Edit', 1);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('0A0CD2FB-8612-4932-8B7B-F4D6EE99CC11', NULL, 'urn:nucleus:entities:module/permissiontype/view', 'View', 0);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('86C40F45-A84E-4BE4-8186-3BEE97896320', NULL, 'urn:nucleus:entities:module/permissiontype/edit', 'Edit', 1);
GO     

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('DC07183F-6D6C-41D0-8974-2AF73D288145', NULL, 'urn:nucleus:entities:folder/permissiontype/view', 'View', 0);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('DEE15E89-D511-42F5-AFC4-E41C77FA3B03', NULL, 'urn:nucleus:entities:folder/permissiontype/edit', 'Edit', 1);
GO

INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('59155015-4172-4C05-97FF-C7FB209DBF81', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', '7EC40AAA-9EB0-4005-9895-C0256E749B83', '469E6802-0CB7-4E19-94E6-BC6D4E927988', 1);
GO

INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('38D63BE9-DAAE-42D0-90B5-5D227775CBD3', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', 'F7AD2BA9-948A-4EB4-BC6E-FCE814AEE0BF', '469E6802-0CB7-4E19-94E6-BC6D4E927988', 1);
GO


INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('48615C02-2519-4526-988E-B30C4414ECEC', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', '7EC40AAA-9EB0-4005-9895-C0256E749B83', 'B97D19F1-CE62-4A9E-B387-5888218C5DDE', 1);
GO

INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('145C5648-0A73-4A73-81E8-263674F91176', '6909B319-25F7-4CA6-99C6-0CDEF6ACC3C1', '7EC40AAA-9EB0-4005-9895-C0256E749B83', '02225041-FA55-433F-918C-36993160D703', 1);
GO

INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('C3FEC5BC-5DA9-4421-85AF-8607EF51F104', '9A41BDD2-AC49-4068-ACB0-DA821E47BE04', '0A0CD2FB-8612-4932-8B7B-F4D6EE99CC11', '02225041-FA55-433F-918C-36993160D703', 1);
GO

INSERT INTO Permissions
([Id], [RelatedId], [PermissionTypeId], [RoleId], [AllowAccess] )
VALUES
('8BF4D8E7-0F1D-45D6-8E70-D0A4199CEA04', '9A41BDD2-AC49-4068-ACB0-DA821E47BE04', '0A0CD2FB-8612-4932-8B7B-F4D6EE99CC11', '469E6802-0CB7-4E19-94E6-BC6D4E927988', 1);
GO

