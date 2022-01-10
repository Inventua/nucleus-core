IF OBJECT_ID(N'Schema', N'U') IS NULL
BEGIN
CREATE TABLE [Schema] 
(
	[SchemaName] NVARCHAR(256) NOT NULL PRIMARY KEY,
	[SchemaVersion] NVARCHAR(32) NULL
);
END;
GO

IF OBJECT_ID(N'ModuleDefinitions', N'U') IS NULL
BEGIN
CREATE TABLE [ModuleDefinitions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[FriendlyName] NVARCHAR(256) NOT NULL,
	[ClassTypeName] NVARCHAR(256) NOT NULL,
	[ViewAction] NVARCHAR(256) NOT NULL,
	[EditAction] NVARCHAR(256) NULL,
	[Categories] NVARCHAR(512) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ModuleDefinitions] ON [ModuleDefinitions]([Id]);
GO

CREATE UNIQUE INDEX [IX_ModuleDefinitions_FriendlyName] ON [ModuleDefinitions]([FriendlyName]);
GO

IF OBJECT_ID(N'LayoutDefinitions', N'U') IS NULL
BEGIN
CREATE TABLE [LayoutDefinitions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[FriendlyName] NVARCHAR(256) NOT NULL,
	[RelativePath] NVARCHAR(256) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_LayoutDefinitions] ON [LayoutDefinitions]([Id]);
GO

CREATE UNIQUE INDEX [IX_LayoutDefinitions_FriendlyName] ON [LayoutDefinitions]([FriendlyName]);
GO

IF OBJECT_ID(N'ContainerDefinitions', N'U') IS NULL
BEGIN
CREATE TABLE [ContainerDefinitions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[FriendlyName] NVARCHAR(256) NOT NULL,
	[RelativePath] NVARCHAR(256) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ContainerDefinitions] ON [ContainerDefinitions]([Id]);
GO

CREATE UNIQUE INDEX [IX_ContainerDefinitions_FriendlyName] ON [ContainerDefinitions]([FriendlyName]);
GO

IF OBJECT_ID(N'ControlPanelExtensionDefinitions', N'U') IS NULL
BEGIN
CREATE TABLE [ControlPanelExtensionDefinitions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[FriendlyName] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(512) NOT NULL,
	[ControllerName] NVARCHAR(256) NOT NULL,
	[ExtensionName] NVARCHAR(256) NOT NULL,
	[Scope] INT NULL,
	[EditAction] NVARCHAR(256) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ControlPanelExtensionDefinitions] ON [ControlPanelExtensionDefinitions]([Id]);
GO

CREATE UNIQUE INDEX [IX_ControlPanelExtensionDefinitions_FriendlyName] ON [ControlPanelExtensionDefinitions]([FriendlyName]);
GO

IF OBJECT_ID(N'ScheduledTasks', N'U') IS NULL
BEGIN
CREATE TABLE [ScheduledTasks] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[TypeName] NVARCHAR(256) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Interval] INT NOT NULL,
	[IntervalType] INT NOT NULL,
	[Enabled] BIT NOT NULL,
	[NextScheduledRun] DATETIME NULL,
	[KeepHistoryCount] int NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ScheduledTasks] ON [ScheduledTasks]([Id]);
GO

CREATE UNIQUE INDEX [IX_ScheduledTasks_Name] ON [ScheduledTasks]([Name]);
GO

IF OBJECT_ID(N'ScheduledTaskHistory', N'U') IS NULL
BEGIN
CREATE TABLE [ScheduledTaskHistory] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ScheduledTaskId] UNIQUEIDENTIFIER NOT NULL,
	[StartDate] DATETIME NOT NULL,
	[FinishDate] DATETIME NULL,
	[NextScheduledRun] DATETIME NULL,
	[Server] NVARCHAR(256) NOT NULL,
	[Status] INT NULL,
	[DateAdded] DATETIME NULL,
	[DateChanged] DATETIME NULL,
	CONSTRAINT [FK_ScheduledTaskHistory_ScheduledTaskId] FOREIGN KEY([ScheduledTaskId]) REFERENCES ScheduledTasks(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ScheduledTaskHistory] ON [ScheduledTaskHistory]([Id]);
GO

IF OBJECT_ID(N'SiteGroups', N'U') IS NULL
BEGIN
CREATE TABLE [SiteGroups] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(512),
	[PrimarySiteId] UNIQUEIDENTIFIER NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_SiteGroups] ON [SiteGroups]([Id]);
GO

CREATE UNIQUE INDEX [IX_SiteGroups_Name] ON [SiteGroups]([Name]);
GO

CREATE UNIQUE INDEX [IX_SiteGroups_PrimarySiteId] ON [SiteGroups]([PrimarySiteId]) WHERE [PrimarySiteId] IS NOT NULL;
GO

IF OBJECT_ID(N'Sites', N'U') IS NULL
BEGIN
CREATE TABLE [Sites] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[AdministratorsRoleId] UNIQUEIDENTIFIER NULL,
	[RegisteredUsersRoleId] UNIQUEIDENTIFIER NULL,
	[AnonymousUsersRoleId] UNIQUEIDENTIFIER NULL,
	[AllUsersRoleId] UNIQUEIDENTIFIER NULL,
	[SiteGroupId] UNIQUEIDENTIFIER NULL,
	[DefaultLayoutDefinitionId] UNIQUEIDENTIFIER NULL,
	[DefaultContainerDefinitionId] UNIQUEIDENTIFIER NULL,
	[DefaultSiteAliasId] UNIQUEIDENTIFIER NULL,
	[HomeDirectory] NVARCHAR(256) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Sites] ON [Sites]([Id]);
GO

CREATE UNIQUE INDEX [IX_Sites_Name] ON [Sites]([Name]);
GO

ALTER TABLE [SiteGroups]
ADD CONSTRAINT [FK_SiteGroups_PrimarySiteId] FOREIGN KEY([PrimarySiteId]) REFERENCES [Sites](Id) ON DELETE SET NULL;
GO

IF OBJECT_ID(N'SiteAlias', N'U') IS NULL
BEGIN
CREATE TABLE [SiteAlias] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[Alias] NVARCHAR(256) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_SiteAlias_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;

CREATE UNIQUE CLUSTERED INDEX [PK_SiteAlias] ON [SiteAlias]([Id]);
GO

CREATE UNIQUE INDEX [IX_SiteAlias_Alias] ON [SiteAlias]([Alias]);
GO

CREATE INDEX SiteAlias_SiteId ON [SiteAlias]([SiteId]);
GO

ALTER TABLE [Sites]
ADD CONSTRAINT [FK_Sites_DefaultSiteAliasId] FOREIGN KEY([DefaultSiteAliasId]) REFERENCES [SiteAlias](Id);
GO
	
IF OBJECT_ID(N'SiteSettings', N'U') IS NULL
BEGIN
CREATE TABLE [SiteSettings] 
(
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[SettingName] NVARCHAR(256) NOT NULL,
	[SettingValue] NVARCHAR(1024) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_SiteSettings_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_SiteSettings] ON [SiteSettings]([SiteId],[SettingName]);
GO

CREATE INDEX SiteSettings_SiteId ON [SiteSettings]([SiteId]);
GO

IF OBJECT_ID(N'Pages', N'U') IS NULL
BEGIN
CREATE TABLE [Pages] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ParentId] UNIQUEIDENTIFIER NULL,
	[SiteId] UNIQUEIDENTIFIER NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Title] NVARCHAR(256) NULL,
	[Description] NVARCHAR(512) NULL,
	[Keywords] NVARCHAR(512) NULL,
	[DefaultPageRouteId] UNIQUEIDENTIFIER NULL,
	[Disabled] BIT NOT NULL,
	[ShowInMenu] BIT NOT NULL,
	[DisableInMenu] BIT NOT NULL,
	[LayoutDefinitionId] UNIQUEIDENTIFIER NULL,
	[DefaultContainerDefinitionId] UNIQUEIDENTIFIER NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Pages_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Pages] ON [Pages]([Id]);
GO

ALTER TABLE [Pages]
ADD CONSTRAINT [FK_Pages_ParentId] FOREIGN KEY([ParentId]) REFERENCES [Pages](Id);
GO

CREATE UNIQUE INDEX [IX_Pages_Name] ON [Pages]([SiteId],[ParentId],[Name]);
GO

CREATE INDEX Pages_SiteId ON [Pages]([SiteId]);
GO

CREATE INDEX Pages_ParentId ON [Pages]([ParentId]);
GO

IF OBJECT_ID(N'PageRoutes', N'U') IS NULL
BEGIN
CREATE TABLE [PageRoutes] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[PageId] UNIQUEIDENTIFIER NOT NULL,
	[Path] NVARCHAR(256) NOT NULL,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_PageRoutes_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id),
	CONSTRAINT [FK_PageRoutes_PageId] FOREIGN KEY([PageId]) REFERENCES [Pages](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_PageRoutes] ON [PageRoutes]([Id]);
GO

CREATE INDEX [IX_PageRoutes_PageId] ON [PageRoutes]([PageId]);
GO

CREATE UNIQUE INDEX [IX_PageRoutes_Path] ON [PageRoutes]([SiteId],[Path]);
GO

ALTER TABLE [Pages]
ADD CONSTRAINT [FK_Pages_DefaultPageRouteId] FOREIGN KEY([DefaultPageRouteId]) REFERENCES PageRoutes(Id);
GO

IF OBJECT_ID(N'PageModules', N'U') IS NULL
BEGIN
CREATE TABLE [PageModules] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[PageId] UNIQUEIDENTIFIER NOT NULL,
	[Pane] NVARCHAR(256) NOT NULL,
	[ContainerDefinitionId] UNIQUEIDENTIFIER NULL,
	[Title] NVARCHAR(256) NULL,
	[ModuleDefinitionId] UNIQUEIDENTIFIER NOT NULL,
	[SortOrder] INT NOT NULL,
	[InheritPagePermissions] BIT NOT NULL,
	[Style] NVARCHAR(256) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_PageModules_PageId] FOREIGN KEY([PageId]) REFERENCES Pages(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_PageModules_ModuleDefinitionId] FOREIGN KEY([ModuleDefinitionId]) REFERENCES ModuleDefinitions(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_PageModules] ON [PageModules]([Id]);
GO

CREATE INDEX [IX_PageModules_PageId] ON [PageModules]([PageId]);
GO

IF OBJECT_ID(N'PageModuleSettings', N'U') IS NULL
BEGIN
CREATE TABLE [PageModuleSettings] 
(
	[PageModuleId] UNIQUEIDENTIFIER NOT NULL,
	[SettingName] NVARCHAR(256) NOT NULL,
	[SettingValue] NVARCHAR(512) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_PageModuleSettings_PageModuleId] FOREIGN KEY([PageModuleId]) REFERENCES PageModules(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_PageModuleSettings] ON [PageModuleSettings]([PageModuleId],[SettingName]);
GO

CREATE INDEX [IX_PageModuleSettings_PageModuleId] ON [PageModuleSettings]([PageModuleId]);
GO

IF OBJECT_ID(N'Users', N'U') IS NULL
BEGIN
CREATE TABLE [Users] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[UserName] NVARCHAR(256) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER,
	[IsSystemAdministrator] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Users_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Users] ON [Users]([Id]);
GO

CREATE UNIQUE INDEX [IX_Users_SiteUser] ON [Users]([UserName],[SiteId]);
GO

CREATE INDEX [IX_Users_SiteId] ON [Users]([SiteId]);
GO

IF OBJECT_ID(N'RoleGroups', N'U') IS NULL
BEGIN
CREATE TABLE [RoleGroups] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(512),
	[SiteId] UNIQUEIDENTIFIER,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_RoleGroups_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_RoleGroups] ON [RoleGroups]([Id]);
GO

CREATE UNIQUE INDEX [IX_RoleGroups_Name] ON [RoleGroups]([SiteId],[Name]);
GO

CREATE INDEX [IX_RoleGroups_SiteId] ON [RoleGroups]([SiteId]);
GO

IF OBJECT_ID(N'Roles', N'U') IS NULL
BEGIN
CREATE TABLE [Roles] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[RoleGroupId] UNIQUEIDENTIFIER,
	[Name] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(512),
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[Type] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Roles_RoleGroupId] FOREIGN KEY([RoleGroupId]) REFERENCES RoleGroups(Id) ON DELETE SET NULL,
	CONSTRAINT [FK_Roles_SiteId] FOREIGN KEY([SiteId]) REFERENCES Sites(Id) 
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Roles] ON [Roles]([Id]);
GO

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles]([SiteId],[Name]);
GO

CREATE INDEX [IX_Roles_SiteId] ON [Roles]([SiteId]);
GO

CREATE INDEX [IX_Roles_RoleGroupId] ON [Roles]([RoleGroupId]);
GO

IF OBJECT_ID(N'UserRoles', N'U') IS NULL
BEGIN
CREATE TABLE [UserRoles] 
(
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[RoleId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_UserRoles_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE,
	CONSTRAINT [FK_UserRoles_RoleId] FOREIGN KEY([RoleId]) REFERENCES Roles(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_UserRoles] ON [UserRoles]([UserId],[RoleId]);
GO

CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles]([UserId]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles]([RoleId]);
GO

ALTER TABLE [Sites]
ADD CONSTRAINT [FK_Sites_AdministratorsRoleId] FOREIGN KEY([AdministratorsRoleId]) REFERENCES Roles(Id);
GO

ALTER TABLE [Sites]
ADD CONSTRAINT [FK_Sites_RegisteredUsersRoleId] FOREIGN KEY([RegisteredUsersRoleId]) REFERENCES Roles(Id);
GO

ALTER TABLE [Sites]
ADD CONSTRAINT [FK_Sites_AnonymousUsersRoleId] FOREIGN KEY([AnonymousUsersRoleId]) REFERENCES Roles(Id);
GO

ALTER TABLE [Sites]
ADD CONSTRAINT [FK_Sites_AllUsersRoleId] FOREIGN KEY([AllUsersRoleId]) REFERENCES Roles(Id);
GO

IF OBJECT_ID(N'UserSecrets', N'U') IS NULL
BEGIN
CREATE TABLE [UserSecrets] 
(
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[PasswordHash] NVARCHAR(1024) NOT NULL,
	[PasswordHashAlgorithm] NVARCHAR(256) NOT NULL,
	[PasswordResetToken] NVARCHAR(256) NULL,
	[PasswordResetTokenExpiryDate] DATETIME NULL,
	[PasswordQuestion] NVARCHAR(256) NULL,
	[PasswordAnswer] NVARCHAR(256) NULL,
	[LastLoginDate] DATETIME NULL,
	[LastPasswordChangedDate] DATETIME NULL,
	[LastLockoutDate] DATETIME NULL,
	[IsLockedOut] BIT NULL,
	[FailedPasswordAttemptCount] INT,
	[FailedPasswordWindowStart] DATETIME NULL,
	[FailedPasswordAnswerAttemptCount] INT,
	[FailedPasswordAnswerWindowStart] DATETIME NULL,
	[Salt] NVARCHAR(1024),
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_UserSecrets_UserId] FOREIGN KEY([UserId]) REFERENCES Users(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_UserSecrets] ON [UserSecrets]([UserId]);
GO

CREATE UNIQUE INDEX IX_UserSecrets_PasswordResetToken ON [UserSecrets] ([PasswordResetToken]) WHERE [PasswordResetToken] IS NOT NULL;
GO

IF OBJECT_ID(N'UserProfileProperties', N'U') IS NULL
BEGIN
CREATE TABLE [UserProfileProperties] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[TypeUri] NVARCHAR(256) NULL,
	[HelpText] NVARCHAR(256) NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_UserProfileProperties_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_UserProfileProperties] ON [UserProfileProperties]([Id]);
GO

CREATE INDEX UserProfileProperties_SiteId ON [UserProfileProperties]([SiteId]);
GO

CREATE UNIQUE INDEX IX_UserProfileProperties_SiteId_TypeUri ON [UserProfileProperties] ([SiteId],[TypeUri]);
GO

CREATE UNIQUE INDEX IX_UserProfileProperties_SiteId_Name ON [UserProfileProperties] ([SiteId],[Name]);
GO

IF OBJECT_ID(N'UserProfileValues', N'U') IS NULL
BEGIN
CREATE TABLE [UserProfileValues] 
(
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[UserProfilePropertyId] UNIQUEIDENTIFIER NOT NULL,
	[Value] NVARCHAR(512) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_UserProfileValues_UserId] FOREIGN KEY([UserId]) REFERENCES [Users](Id) ON DELETE CASCADE,
	CONSTRAINT [FK_UserProfileValues_UserProfilePropertyId] FOREIGN KEY([UserProfilePropertyId]) REFERENCES [UserProfileProperties](Id) 
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_UserProfileValues] ON [UserProfileValues]([UserId],[UserProfilePropertyId]);
GO

CREATE INDEX UserProfile_UserId ON [UserProfileValues]([UserId]);
GO

IF OBJECT_ID(N'UserSessions', N'U') IS NULL
BEGIN
CREATE TABLE [UserSessions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[SlidingExpiry] BIT NOT NULL,
	[IsPersistent] BIT NOT NULL,
	[ExpiryDate] DATETIME NOT NULL,
	[IssuedDate] DATETIME NOT NULL,
	[RemoteIpAddress] NVARCHAR(256) NOT NULL,
	CONSTRAINT [FK_UserSessions_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id),
	CONSTRAINT [FK_UserSessions_UserId] FOREIGN KEY([UserId]) REFERENCES [Users](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_UserSessions] ON [UserSessions]([Id]);
GO

CREATE INDEX IX_UserSession_UserId ON UserSessions([UserId]);
GO

CREATE INDEX IX_UserSession_SiteId ON UserSessions([SiteId]);
GO

IF OBJECT_ID(N'PermissionTypes', N'U') IS NULL
BEGIN
CREATE TABLE [PermissionTypes] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ModuleDefinitionId] UNIQUEIDENTIFIER NULL,
	[Scope] NVARCHAR(256) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	UNIQUE([Scope]),
	CONSTRAINT [FK_PermissionTypes_ModuleDefinitionId] FOREIGN KEY([ModuleDefinitionId]) REFERENCES [ModuleDefinitions](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_PermissionTypes] ON [PermissionTypes]([Id]);
GO

CREATE UNIQUE INDEX [IX_PermissionTypes_Scope] ON [PermissionTypes]([Scope]);
GO

CREATE INDEX [IX_PermissionTypes_ModuleDefinitionId] ON [PermissionTypes]([ModuleDefinitionId]);
GO

IF OBJECT_ID(N'Permissions', N'U') IS NULL
BEGIN
CREATE TABLE [Permissions] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[RelatedId] UNIQUEIDENTIFIER NOT NULL,
	[PermissionTypeId] UNIQUEIDENTIFIER NOT NULL,
	[RoleId] UNIQUEIDENTIFIER NOT NULL,
	[AllowAccess] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Permissions_PermissionTypeId] FOREIGN KEY([PermissionTypeId]) REFERENCES PermissionTypes(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Permissions] ON [Permissions]([Id]);
GO

CREATE UNIQUE INDEX [IX_Permissions_RelatedId_PermissionTypeId_RoleId] ON [Permissions]([RelatedId],[PermissionTypeId],[RoleId]);
GO

CREATE INDEX [IX_Permissions_RelatedId] ON [Permissions]([RelatedId]);
GO

IF OBJECT_ID(N'MailTemplates', N'U') IS NULL
BEGIN
CREATE TABLE [MailTemplates] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Subject] NVARCHAR(256) NOT NULL,
	[Body] NVARCHAR(max) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_MailTemplates_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_MailTemplates] ON [MailTemplates]([Id]);
GO

CREATE UNIQUE INDEX [IX_MailTemplates_SiteId_Name] ON [Roles]([SiteId],[Name]);
GO

IF OBJECT_ID(N'Folders', N'U') IS NULL
BEGIN
CREATE TABLE [Folders] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER,
	[Provider] NVARCHAR(256) NOT NULL,
	[Path] NVARCHAR(1024) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Folders_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Folders] ON [Folders]([Id]);
GO

CREATE INDEX [IX_Folders_Path] ON [Folders]([Provider],[Path]);
GO

IF OBJECT_ID(N'Files', N'U') IS NULL
BEGIN
CREATE TABLE [Files] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[SiteId] UNIQUEIDENTIFIER,
	[Provider] NVARCHAR(256) NOT NULL,
	[Path] NVARCHAR(1024) NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Files_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Files] ON [Files]([Id]);
GO

CREATE INDEX [IX_Files_Path] ON [Files]([Provider],[Path]);
GO

IF OBJECT_ID(N'Lists', N'U') IS NULL
BEGIN
CREATE TABLE [Lists] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(512) NULL,
	[SiteId] UNIQUEIDENTIFIER NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Lists_SiteId] FOREIGN KEY([SiteId]) REFERENCES [Sites](Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Lists] ON [Lists]([Id]);
GO

CREATE UNIQUE INDEX [IX_Lists_Name] ON [Lists]([SiteId],[Name]);
GO

IF OBJECT_ID(N'ListItems', N'U') IS NULL
BEGIN
CREATE TABLE [ListItems] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ListId] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(256) NOT NULL,
	[Value] NVARCHAR(512) NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ListItems_ListId] FOREIGN KEY([ListId]) REFERENCES Lists(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ListItems] ON [ListItems]([Id]);
GO

CREATE UNIQUE INDEX [IX_ListItems_Name] ON [ListItems]([ListId],[Name]);
GO


IF OBJECT_ID(N'Content', N'U') IS NULL
BEGIN
CREATE TABLE [Content] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[PageModuleId] UNIQUEIDENTIFIER NOT NULL,
	[Title] NVARCHAR(256) NULL,
	[Value] NVARCHAR(max) NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_PageModuleId_PageModuleId] FOREIGN KEY([PageModuleId]) REFERENCES PageModules(Id) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Content] ON [Content]([Id]);
GO
