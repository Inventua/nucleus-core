CREATE TABLE IF NOT EXISTS [Schema] 
(
	[SchemaName] TEXT COLLATE NOCASE,
	[SchemaVersion] TEXT NULL,
	CONSTRAINT [PK_Schema] PRIMARY KEY([SchemaName])
);
GO

CREATE TABLE IF NOT EXISTS [ModuleDefinitions] 
(
	[Id] GUID,
	[FriendlyName] TEXT NOT NULL COLLATE NOCASE,
	[ClassTypeName] TEXT NOT NULL,
	[ViewAction] TEXT NOT NULL,
	[EditAction] TEXT NULL,
	[Categories] TEXT NULL COLLATE NOCASE,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ModuleDefinitions] PRIMARY KEY([Id]),
	CONSTRAINT [IX_ModuleDefinitions_FriendlyName] UNIQUE([FriendlyName])
);
GO

CREATE TABLE IF NOT EXISTS [LayoutDefinitions] 
(
	[Id] GUID,
	[FriendlyName] TEXT NOT NULL COLLATE NOCASE,
	[RelativePath] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_LayoutDefinitions] PRIMARY KEY([Id]),	
	CONSTRAINT [IX_LayoutDefinitions_FriendlyName] UNIQUE([FriendlyName])
);
GO

CREATE TABLE IF NOT EXISTS [ContainerDefinitions] 
(
	[Id] GUID,
	[FriendlyName] TEXT NOT NULL COLLATE NOCASE,
	[RelativePath] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ContainerDefinitions] PRIMARY KEY([Id]),
	CONSTRAINT [IX_ContainerDefinitions_FriendlyName] UNIQUE([FriendlyName])
);
GO

CREATE TABLE IF NOT EXISTS [ControlPanelExtensionDefinitions] 
(
	[Id] GUID,
	[FriendlyName] TEXT NOT NULL COLLATE NOCASE,
	[Description] TEXT NOT NULL,
	[ControllerName] TEXT NOT NULL,
	[ExtensionName] TEXT NOT NULL,
	[Scope] INT NULL,
	[EditAction] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ControlPanelExtensionDefinitions] PRIMARY KEY([Id]),
	CONSTRAINT [IX_ControlPanelExtensionDefinitions_FriendlyName] UNIQUE([FriendlyName])
);
GO

CREATE TABLE IF NOT EXISTS [ScheduledTasks] 
(
	[Id] GUID,
	[TypeName] TEXT NOT NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Interval] INT NOT NULL,
	[IntervalType] INT NOT NULL,
	[Enabled] BIT NOT NULL,
	[NextScheduledRun] DATETIME NULL,
	[KeepHistoryCount] int NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ScheduledTasks] PRIMARY KEY([Id]),
	CONSTRAINT [IX_ScheduledTasks_Name] UNIQUE([Name])
);
GO

CREATE TABLE IF NOT EXISTS [ScheduledTaskHistory] 
(
	[Id] GUID,
	[ScheduledTaskId] GUID NOT NULL,
	[StartDate] DATETIME NOT NULL,
	[FinishDate] DATETIME NULL,
	[NextScheduledRun] DATETIME NULL,
	[Server] TEXT NOT NULL,
	[Status] INT NULL,
	[DateAdded] DATETIME NULL,
	[DateChanged] DATETIME NULL,
	CONSTRAINT [PK_ScheduledTasksHistory] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ScheduledTaskHistory_ScheduledTaskId] FOREIGN KEY([ScheduledTaskId]) REFERENCES ScheduledTasks(Id) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [SiteGroups] 
(
	[Id] GUID,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Description] TEXT,
	[PrimarySiteId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_SiteGroups] PRIMARY KEY([Id]),
	CONSTRAINT [IX_SiteGroups_Name] UNIQUE([Name]),
	CONSTRAINT [FK_SiteGroups_PrimarySiteId] FOREIGN KEY([PrimarySiteId]) REFERENCES Sites(Id) ON DELETE SET NULL
);
GO

CREATE TABLE IF NOT EXISTS [Sites] 
(
	[Id] GUID,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[AdministratorsRoleId] GUID NULL,
	[RegisteredUsersRoleId] GUID NULL,
	[AnonymousUsersRoleId] GUID NULL,
	[AllUsersRoleId] GUID NULL,
	[SiteGroupId] GUID NULL,
	[DefaultLayoutDefinitionId] GUID NULL,
	[DefaultContainerDefinitionId] GUID NULL,
	[DefaultSiteAliasId] GUID NULL,
	[HomeDirectory] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Sites] PRIMARY KEY([Id]),
	CONSTRAINT [IX_Sites_Name] UNIQUE([Name]),
	CONSTRAINT [FK_Sites_DefaultSiteAliasId] FOREIGN KEY([DefaultSiteAliasId]) REFERENCES SiteAlias(Id) ON DELETE SET NULL,
	CONSTRAINT [FK_Sites_AdministratorsRoleId] FOREIGN KEY([AdministratorsRoleId]) REFERENCES Roles(Id),
	CONSTRAINT [FK_Sites_RegisteredUsersRoleId] FOREIGN KEY([RegisteredUsersRoleId]) REFERENCES Roles(Id),
	CONSTRAINT [FK_Sites_AnonymousUsersRoleId] FOREIGN KEY([AnonymousUsersRoleId]) REFERENCES Roles(Id),
	CONSTRAINT [FK_Sites_RegisteredUsersRoleId] FOREIGN KEY([RegisteredUsersRoleId]) REFERENCES Roles(Id)
);
GO

CREATE TABLE IF NOT EXISTS [SiteAlias] 
(
	[Id] GUID,
	[SiteId] GUID NOT NULL,
	[Alias] TEXT NOT NULL COLLATE NOCASE,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_SiteAlias] PRIMARY KEY([Id]),
	CONSTRAINT [IX_SiteAlias_Alias] UNIQUE([Alias]),
	CONSTRAINT [FK_SiteAlias_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);

CREATE INDEX [IX_SiteAlias_Alias] ON [SiteAlias]([Alias]);
GO

CREATE INDEX [IX_SiteAlias_SiteId] ON [SiteAlias]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [SiteSettings] 
(
	[SiteId] GUID NOT NULL,
	[SettingName] TEXT NOT NULL COLLATE NOCASE,
	[SettingValue] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_SiteSettings] PRIMARY KEY([SiteId],[SettingName]),
	CONSTRAINT [FK_SiteSettings_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_SiteSettings_SiteId] ON [SiteSettings]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [Pages] 
(
	[Id] GUID,
	[ParentId] GUID NULL,
	[SiteId] GUID NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Title] TEXT NULL COLLATE NOCASE,
	[Description] TEXT NULL,
	[Keywords] TEXT NULL COLLATE NOCASE,
	[DefaultPageRouteId] GUID NULL,
	[Disabled] BIT NOT NULL,
	[ShowInMenu] BIT NOT NULL,
	[DisableInMenu] BIT NOT NULL,
	[LayoutDefinitionId] GUID NULL,
	[DefaultContainerDefinitionId] GUID NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Pages] PRIMARY KEY([Id]),
	CONSTRAINT [IX_Pages_Name] UNIQUE([SiteId],[ParentId],[Name]),
	CONSTRAINT [FK_Pages_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_Pages_DefaultPageRouteId] FOREIGN KEY([DefaultPageRouteId]) REFERENCES PageRoutes(Id) ON DELETE SET NULL	
);
GO

CREATE INDEX [IX_Pages_SiteId] ON [Pages]([SiteId]);
GO

CREATE INDEX [IX_Pages_ParentId] ON [Pages]([ParentId]);
GO

CREATE TABLE IF NOT EXISTS [PageRoutes] 
(
	[Id] GUID,
	[SiteId] GUID NOT NULL,
	[PageId] GUID NOT NULL,
	[Path] TEXT NOT NULL COLLATE NOCASE,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_PageRoutes] PRIMARY KEY([Id]),
	CONSTRAINT [FK_PageRoutes_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id),
	CONSTRAINT [FK_PageRoutes_PageId] FOREIGN KEY([PageId]) REFERENCES Pages(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PageRoutes_PageId] ON [PageRoutes]([PageId]);
GO

CREATE UNIQUE INDEX [IX_PageRoutes_Path] ON [PageRoutes]([SiteId],[Path]);
GO

CREATE TABLE IF NOT EXISTS [PageModules] 
(
	[Id] GUID,
	[PageId] GUID NOT NULL,
	[Pane] TEXT NOT NULL COLLATE NOCASE,
	[ContainerDefinitionId] GUID NULL,
	[Title] TEXT NULL COLLATE NOCASE,
	[ModuleDefinitionId] GUID NOT NULL,
	[SortOrder] INT NOT NULL,
	[InheritPagePermissions] BIT NOT NULL,
	[Style] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_PageModules] PRIMARY KEY([Id]),
	CONSTRAINT [FK_PageModules_PageId] FOREIGN KEY([PageId]) REFERENCES Pages(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_PageModules_ModuleDefinitionId] FOREIGN KEY([ModuleDefinitionId]) REFERENCES ModuleDefinitions(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PageModules_PageId] ON [PageModules]([PageId]);
GO

CREATE TABLE IF NOT EXISTS [PageModuleSettings] 
(
	[PageModuleId] GUID NOT NULL,
	[SettingName] TEXT NOT NULL COLLATE NOCASE,
	[SettingValue] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_PageModuleSettings] PRIMARY KEY([PageModuleId],[SettingName]),
	CONSTRAINT [FK_PageModuleSettings_PageModuleId] FOREIGN KEY([PageModuleId]) REFERENCES PageModules(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PageModuleSettings_PageModuleId] ON [PageModuleSettings]([PageModuleId]);
GO

CREATE TABLE IF NOT EXISTS [Users] 
(
	[Id] GUID,
	[UserName] TEXT NOT NULL COLLATE NOCASE,
	[SiteId] GUID,
	[IsSystemAdministrator] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Users] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Users_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Users_SiteUser] ON [Users]([UserName],[SiteId]);
GO

CREATE INDEX [IX_Users_SiteId] ON [Users]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [UserRoles] 
(
	[UserId] GUID NOT NULL,
	[RoleId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_UserRoles] PRIMARY KEY([UserId],[RoleId]),
	CONSTRAINT [FK_UserRoles_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_UserRoles_RoleId] FOREIGN KEY([RoleId]) REFERENCES Roles(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles]([UserId]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles]([RoleId]);
GO

CREATE TABLE IF NOT EXISTS [UserSecrets] 
(
	[UserId] GUID,
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
	CONSTRAINT [PK_UserSecrets] PRIMARY KEY([UserId]),
	CONSTRAINT [FK_UserSecrets_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_UserSecrets_PasswordResetToken] ON [UserSecrets] ([PasswordResetToken]);
GO

CREATE TABLE IF NOT EXISTS [UserProfileProperties] 
(
	[Id] GUID,
	[SiteId] GUID NOT NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[TypeUri] TEXT NULL COLLATE NOCASE,
	[HelpText] TEXT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_UserProfileProperties] PRIMARY KEY([Id]),
	CONSTRAINT [FK_UserProfileProperties_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserProfileProperties_SiteId] ON [UserProfileProperties]([SiteId]);
GO

CREATE UNIQUE INDEX [IX_UserProfileProperties_SiteId_TypeUri] ON [UserProfileProperties] ([SiteId],[TypeUri]);
GO

CREATE UNIQUE INDEX [IX_UserProfileProperties_SiteId_Name] ON [UserProfileProperties] ([SiteId],[Name]);
GO

CREATE TABLE IF NOT EXISTS [UserProfileValues] 
(
	[UserId] GUID NOT NULL,
	[UserProfilePropertyId] GUID NOT NULL,
	[Value] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_UserProfileValues] PRIMARY KEY([UserId],[UserProfilePropertyId]),
	CONSTRAINT [FK_UserProfileValues_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_UserProfileValues_UserProfilePropertyId] FOREIGN KEY([UserProfilePropertyId]) REFERENCES UserProfileProperties(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserProfile_UserId] ON [UserProfileValues]([UserId]);
GO

CREATE TABLE IF NOT EXISTS [UserSessions] 
(
	[Id] GUID,
	[SiteId] GUID NOT NULL,
	[UserId] GUID NOT NULL,
	[SlidingExpiry] BIT NOT NULL,
	[IsPersistent] BIT NOT NULL,
	[ExpiryDate] DATETIME NOT NULL,
	[IssuedDate] DATETIME NOT NULL,
	[RemoteIpAddress] TEXT NOT NULL,
	CONSTRAINT [PK_UserSessions] PRIMARY KEY([Id]),
	CONSTRAINT [FK_UserSessions_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_UserSessions_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserSession_UserId] ON UserSessions([UserId]);
GO

CREATE INDEX [IX_UserSession_SiteId] ON UserSessions([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [RoleGroups] 
(
	[Id] GUID,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Description] TEXT,
	[SiteId] GUID,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_RoleGroups] PRIMARY KEY([Id]),
	CONSTRAINT [IX_RoleGroups_SiteId_Name] UNIQUE([SiteId],[Name]),
	CONSTRAINT [FK_RoleGroups_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RoleGroups_SiteId] ON [RoleGroups]([SiteId]);
GO

CREATE TABLE IF NOT EXISTS [Roles] 
(
	[Id] GUID,
	[RoleGroupId] GUID,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Description] TEXT,
	[SiteId] GUID NOT NULL,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Roles] PRIMARY KEY([Id]),
	CONSTRAINT [IX_Roles_SiteId_Name] UNIQUE([SiteId],[Name]),
	CONSTRAINT [FK_Roles_RoleGroupId] FOREIGN KEY([RoleGroupId]) REFERENCES RoleGroups(Id) ON DELETE SET NULL,
	CONSTRAINT [FK_UserSessions_SiteId] CONSTRAINT [FK_Roles_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) 
);
GO

CREATE INDEX [IX_Roles_SiteId] ON [Roles]([SiteId]);
GO

CREATE INDEX [IX_Roles_RoleGroupId] ON [Roles]([RoleGroupId]);
GO

CREATE TABLE IF NOT EXISTS [PermissionTypes] 
(
	[Id] GUID,
	[ModuleDefinitionId] GUID NULL,
	[Scope] TEXT NOT NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_PermissionTypes] PRIMARY KEY([Id]),
	CONSTRAINT [IX_PermissionTypes_Scope] UNIQUE([Scope]),
	CONSTRAINT [FK_PermissionTypes_ModuleDefinitionId] FOREIGN KEY([ModuleDefinitionId]) REFERENCES ModuleDefinitions(Id) ON DELETE CASCADE
);
GO

CREATE INDEX PermissionTypes_ModuleDefinitionId ON [PermissionTypes]([ModuleDefinitionId]);
GO

CREATE TABLE IF NOT EXISTS [Permissions] 
(
	[Id] GUID,
	[RelatedId] GUID NOT NULL,
	[PermissionTypeId] GUID NOT NULL,
	[RoleId] GUID NOT NULL,
	[AllowAccess] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Permissions] PRIMARY KEY([Id]),
	CONSTRAINT [IX_Permissions_RelatedId_PermissionTypeId_RoleId] UNIQUE([RelatedId], [PermissionTypeId], [RoleId]),
	CONSTRAINT [FK_Permissions_PermissionTypeId] FOREIGN KEY([PermissionTypeId]) REFERENCES PermissionTypes(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Permissions_RelatedId] ON [Permissions]([RelatedId]);
GO

CREATE TABLE IF NOT EXISTS [MailTemplates] 
(
	[Id] GUID,
	[SiteId] GUID NOT NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Subject] TEXT NOT NULL,
	[Body] TEXT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_MailTemplates] PRIMARY KEY([Id]),
	CONSTRAINT [FK_MailTemplates_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_MailTemplates_SiteId_Name] ON [Roles]([SiteId],[Name]);
GO

CREATE TABLE IF NOT EXISTS [Folders] 
(
	[Id] GUID,
	[SiteId] GUID,
	[Provider] TEXT NOT NULL COLLATE NOCASE,
	[Path] TEXT NOT NULL COLLATE NOCASE,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Folders] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Folders_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Folders_Path] ON [Folders]([Provider],[Path]);
GO

CREATE TABLE IF NOT EXISTS [Files] 
(
	[Id] GUID,
	[SiteId] GUID,
	[Provider] TEXT NOT NULL COLLATE NOCASE,
	[Path] TEXT NOT NULL COLLATE NOCASE,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Files] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Files_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Files_Path] ON [Files]([Provider],[Path]);
GO

CREATE TABLE IF NOT EXISTS [Lists] 
(
	[Id] GUID,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Description] TEXT NULL,
	[SiteId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Lists] PRIMARY KEY([Id]),
	CONSTRAINT [IX_Lists_SiteId_Name] UNIQUE([SiteId],[Name]),
	CONSTRAINT [FK_Lists_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [ListItems] 
(
	[Id] GUID,
	[ListId] GUID NOT NULL,
	[Name] TEXT NOT NULL COLLATE NOCASE,
	[Value] TEXT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ListItems] PRIMARY KEY([Id]),
	CONSTRAINT [IX_ListItems_ListId_Name] UNIQUE([ListId],[Name]),
	CONSTRAINT [FK_ListItems_ListId] FOREIGN KEY([ListId]) REFERENCES Lists(Id) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [Content] 
(
	[Id] GUID,
	[PageModuleId] GUID NOT NULL,
	[Title] TEXT NULL,
	[Value] TEXT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Content] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Content_PageModuleId] FOREIGN KEY([PageModuleId]) REFERENCES PageModules(Id) ON DELETE CASCADE
);
GO
